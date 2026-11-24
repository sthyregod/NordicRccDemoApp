using Microsoft.EntityFrameworkCore;
using NordicRccDemoApp.Common;
using NordicRccDemoApp.Data;
using NordicRccDemoApp.Dtos;
using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;
using NpgsqlTypes;

namespace NordicRccDemoApp.Service;

public class TimeSeriesService(AppDbContext db)
{
    public Task<TimeSeries?> GetMetadata(Guid id) => db.TimeSeries.FirstOrDefaultAsync(ts => ts.Id == id);

    public async Task<List<TimeSeries>> GetList(IEnumerable<BiddingZone>? biddingZones = null)
    {
        var query = db.TimeSeries.AsQueryable();

        if (biddingZones?.Any() == true)
        {
            var zones = biddingZones.ToList();
            query = query.Where(ts => zones.Contains(ts.BiddingZone));
        }

        return await query
            .OrderBy(ts => ts.BiddingZone)
            .ThenBy(ts => ts.Resolution)
            .ToListAsync();
    }

    public async Task<List<TimeSeries>> GetData(DateTimeOffset startTime, DateTimeOffset endTime,
        IEnumerable<BiddingZone>? biddingZones = null)
    {
        var query = db.TimeSeries.AsQueryable();

        if (biddingZones?.Any() == true)
        {
            var zones = biddingZones.ToList();
            query = query.Where(ts => zones.Contains(ts.BiddingZone));
        }
        
        return await query
            .Include(ts => ts.DataPoints.Where(dp =>
                dp.Period.LowerBound >= startTime && dp.Period.UpperBound <= endTime)
                .OrderBy(dp => dp.Period.LowerBound))
            .OrderBy(ts => ts.BiddingZone)
            .ThenBy(ts => ts.Resolution)
            .ToListAsync();
    }

    public async Task<Result<TimeSeries>> CreateTimeSeries(CreateTimeSeriesDto dto)
    {
        var exists = await db.TimeSeries.AnyAsync(ts =>
            ts.BiddingZone == dto.BiddingZone &&
            ts.Resolution == dto.Resolution);
        
        if (exists)
            return Result<TimeSeries>.Fail(Outcome.Conflict, "Time series already exists");
        
        var series = new TimeSeries
        {
            BiddingZone = dto.BiddingZone,
            Resolution = dto.Resolution
        };

        db.TimeSeries.Add(series);
        await db.SaveChangesAsync();
        
        return Result<TimeSeries>.Ok(series);
    }

    public async Task<Result<TimeSeries>> GetTimeSeriesById(Guid id, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var query = db.TimeSeries.AsQueryable();

        var timeSeries =  await query
            .Where(ts => ts.Id == id)
            .Include(ts => ts.DataPoints.Where(dp =>
                dp.Period.LowerBound >= startTime && dp.Period.UpperBound <= endTime)
                .OrderBy(dp => dp.Period.LowerBound))
            .OrderBy(ts => ts.BiddingZone)
            .ThenBy(ts => ts.Resolution)
            .FirstOrDefaultAsync(ts => ts.Id == id);
        
        if (timeSeries == null)
            return Result<TimeSeries>.Fail(Outcome.NotFound, "Time series not found");

        return Result<TimeSeries>.Ok(timeSeries);
    }

    public async Task<Result> UpsertDataPoint(Guid timeSeriesId, UpsertDataPointDto dto)
    {
        var timeSeries = await db.TimeSeries.FirstOrDefaultAsync(ts => ts.Id == timeSeriesId);
        
        if (timeSeries is null)
            return Result.Fail(Outcome.NotFound, "Time series couldn't be found");
        
        var period = new NpgsqlRange<DateTimeOffset>(dto.StartTime, true, dto.EndTime, false);
        
        if (!timeSeries.IsValidPeriod(period))
            return Result.Fail(Outcome.BadRequest, "Period does not match the series resolution");
        
        var existing = await db.DataPoints.Where(dp => dp.TimeSeriesId == timeSeriesId && dp.Period == period)
            .ExecuteUpdateAsync(s => s
                .SetProperty(dp => dp.Energy.Value, dto.Energy.Value)
                .SetProperty(dp => dp.Energy.Unit,  dto.Energy.Unit));

        if (existing == 0)
        {
            db.DataPoints.Add(new DataPoint
            {
                TimeSeriesId = timeSeriesId,
                Period = period,
                Energy = dto.Energy
            });
        }
        
        await db.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> DeleteDataPoint(Guid timeSeriesId, Guid dataPointId)
    {
        var dataPoint = await db.DataPoints.FirstOrDefaultAsync(dp => dp.Id == dataPointId && dp.TimeSeriesId == timeSeriesId);

        if (dataPoint is null)
            return Result.Fail(Outcome.NotFound, "Data point couldn't be found");

        db.DataPoints.Remove(dataPoint);

        await db.SaveChangesAsync();

        return Result.Ok();
    }
}