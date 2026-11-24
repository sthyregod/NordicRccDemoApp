# Electricity Market Time Series API

A small ASP.NET Core API for storing and querying electricity market time series. A series belongs to a bidding zone and has a fixed resolution. Its data points contain an energy value and a time interval. PostgreSQL persists the data and prevents overlapping intervals within a series.

Deliberate scope choices and their revisit triggers are in [Design decisions](#design-decisions). Known edges are in [Scope boundaries](#scope-boundaries).
## Run locally

Prerequisites:

- .NET 11 SDK. The projects target `net11.0`
- Docker with Compose and a running Docker daemon, including access to the PostgreSQL 16 image. Port `5432` must be available. The project has been tested using Podman

From the repository root:

```bash
docker compose -f NordicRccDemoApp/compose.yml up -d --wait
ASPNETCORE_ENVIRONMENT=Development dotnet run --project NordicRccDemoApp --no-launch-profile --urls http://localhost:5000
```

The API is available at `http://localhost:5000`. In Development, the API reference is at `http://localhost:5000/scalar`. The app applies EF Core migrations at startup and seeds sample data if there are no series. Seeded `Dk1` and `Se1` series contain hourly and quarter-hourly data points for 26th of November.

### Tests

```bash
dotnet test NordicRccDemoAppTest/NordicRccDemoAppTest.csproj
```

Integration tests use containerized PostgreSQL, so they also require a working Docker-compatible container runtime and image access. Tests cover domain rules, database overlap constraints, and API workflows and validation.

## API

All routes are relative to `/api/timeseries`. JSON enum names such as `Dk1`, `QuarterHour` and `MWh` can be used in requests.

| Method | Route | Behavior |
| --- | --- | --- |
| `POST` | `/` | Create a series for a zone and resolution. Sequential duplicates return `409`. |
| `GET` | `/` | List series metadata. Optionally filter with the `biddingZones` parameter. |
| `GET` | `/Data` | Retrieve series and data points, with required `startTime` and `endTime` and optional `biddingZones`. |
| `GET` | `/{id}` | Retrieve one series and its data points for the required time range. |
| `GET` | `/{id}/metadata` | Retrieve one series' metadata without a time range. |
| `PUT` | `/{id}` | Insert a point or update the energy of the point with the same period. |
| `DELETE` | `/{id}/DataPoints/{dataPointId}` | Hard-delete a point belonging to the series. |

Time filters return data points within the requested interval. Use UTC timestamps (`Z` or a zero offset); non-zero offsets are rejected. Data point intervals are half-open `[start, end)`, must match the series resolution, and must start on a resolution boundary. Error responses include `400` for invalid input and `404` for missing resources. The `Location` of a newly created series points to its metadata.

### Example workflow

Create a series using a zone not already present in the seed data:

```bash
curl -i -X POST http://localhost:5000/api/timeseries \
  -H 'Content-Type: application/json' \
  -d '{"biddingZone":"No1","resolution":"QuarterHour"}'
```

Copy the returned `id` into `ID`, then add a point and retrieve it:

```bash
ID='<returned-id>'
curl -i -X PUT "http://localhost:5000/api/timeseries/$ID" \
  -H 'Content-Type: application/json' \
  -d '{"startTime":"2026-11-26T12:00:00Z","endTime":"2026-11-26T12:15:00Z","energy":{"value":42,"unit":"MWh"}}'

curl -G "http://localhost:5000/api/timeseries/$ID" \
  --data-urlencode 'startTime=2026-11-26T12:00:00Z' \
  --data-urlencode 'endTime=2026-11-26T13:00:00Z'
```

To query by zone and interval instead:

```bash
curl -G http://localhost:5000/api/timeseries/Data \
  --data-urlencode 'biddingZones=No1' \
  --data-urlencode 'startTime=2026-11-26T12:00:00Z' \
  --data-urlencode 'endTime=2026-11-26T13:00:00Z'
```

To delete the data point, copy its returned `id` into `DATA_POINT_ID`:

```bash
DATA_POINT_ID='<returned-point-id>'
curl -i -X DELETE "http://localhost:5000/api/timeseries/$ID/DataPoints/$DATA_POINT_ID"
```

## Design decisions

Each entry states the choice, the reasoning, and the condition under which I would change it

**PostgreSQL over in-memory, despite the brief permitting in-memory.**  
The brief allowed in-memory, and for the API surface alone a dictionary would have been fewer moving parts. I chose Postgres for one reason: the core correctness rule of a time series is that points within a series do not overlap, and that rule is only truly enforceable at the database. A tsrange column with an exclusion constraint rejects an overlapping write transactionally, so the invariant holds under concurrent writers in a way an application-level check cannot. If the team standardizes on another datastore, the domain model and endpoints stay put, only the repository implementation changes.

**The invariant lives in the schema, not the service.**  
I deliberately pushed non-overlap down to the lowest layer that can guarantee it. A rule enforced only in the service layer is one careless future endpoint away from being violated; a schema constraint means the next developer, including me in six months, cannot reintroduce overlaps without the write failing loudly. This is the one decision I'd defend hardest: I can't see a realistic case for moving the no-overlap guarantee out of the database, so it's the part I'd least want to move.

**Half-open `[start, end)` intervals.**  
Adjacent points share a boundary without double-counting it, so a day sliced into quarter-hours sums correctly. If a downstream consumer required closed intervals, I'd convert at the response boundary rather than change storage, since closed intervals don't tile and would corrupt the canonical representation for everyone else.

**UTC storage and filtering, offsets rejected at the edge.**  
I store and filter in UTC so interval math is unambiguous across the DST boundary. Non-UTC input is rejected with
`400` and bidding-zone local time is treated as a presentation concern. If the product genuinely needed zone-local semantics, I'd model that explicitly, storing the UTC timestamp plus the zone ID and deriving local time on read rather than make local time the storage default.

**Energy as a value object (value + unit), not a bare decimal.**  
Unit confusion is a real class of bug in market data. Encoding the unit in the type moves it from a runtime surprise to a compile-time one. If more measures such as price, power, or capacity appeared, I'd generalize this into a small set of strongly-typed measures rather than special-case energy.

**Application-level uniqueness for series, database-level for overlap.**  
I enforced the *data* invariant (no overlapping points) in the database, where it is transactionally safe, but left the *request* invariant (no duplicate series, idempotent upsert) to the application. That asymmetry is deliberate, not an oversight. The data invariant must never be violated by any code path, whereas the request invariant is a concurrency nicety that only matters when callers race each other. I'd add a unique
constraint on (zone, resolution) and an idempotency key on the upsert once this is exposed to more than a single trusted caller.

# Scope boundaries

Deliberate exclusions for this exercise:

* Single-tenant, no auth. Appropriate for the brief. The first thing I'd add the moment this is exposed beyond localhost.
* Hard delete of data points. Demonstrates the requested operation. Market data normally needs auditability, so a production system would use soft delete or bitemporal history. I chose the literal operation over inventing an audit model the brief didn't ask for.
* Series identified by (zone, resolution) only. This is intentionally too coarse to distinguish multiple kinds of market data (flow direction, production vs consumption, counterparty zone). I left those out rather than build a half-modeled version. The model has a clear seam to extend if they're needed.
* Fixed resolution per series, no archiving. A resolution change is assumed to mean a new series. Archiving/retention is a lifecycle concern outside this scope.