using System.Net;
using Microsoft.AspNetCore.Hosting;
using NordicRccDemoApp.Dtos;
using NordicRccDemoApp.Enums;
using NordicRccDemoAppTest.Infrastructure;

namespace NordicRccDemoAppTest.Controllers;

public class TimeSeriesWorkflowTests : ApiTestBase
{
    private const string Range = "startTime=2026-12-01T12%3A00%3A00Z&endTime=2026-12-01T13%3A00%3A00Z";
    private static readonly DateTimeOffset Start = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        base.ConfigureWebHost(builder);
    }

    private async Task<TimeSeriesResponse> CreateSeries(BiddingZone zone)
    {
        var response = await Client.PostAsJsonAsync("/api/timeseries",
            new { BiddingZone = zone, Resolution = Resolution.QuarterHour });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        
        var series = await response.Content.ReadFromJsonAsync<TimeSeriesResponse>(JsonOptions);
        
        Assert.Multiple(() =>
        {
            Assert.That(series, Is.Not.Null);
            Assert.That(response.Headers.Location, Is.Not.Null);
        });
        
        var locationResponse = await Client.GetAsync(response.Headers.Location);
        Assert.That(locationResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var located = await locationResponse.Content.ReadFromJsonAsync<TimeSeriesResponse>(JsonOptions);
        Assert.That(located!.Id, Is.EqualTo(series!.Id));
        
        return series;
    }

    [TestCase("2026-12-01T12:00:00+01:00", "2026-12-01T12:15:00Z")]
    [TestCase("2026-12-01T12:00:00Z", "2026-12-01T12:15:00-01:00")]
    public async Task NonUtcInputs_AreRejected(string start, string end)
    {
        var id = Guid.NewGuid();
        var range = $"startTime={Uri.EscapeDataString(start)}&endTime={Uri.EscapeDataString(end)}";
        Assert.That((await Client.GetAsync($"/api/timeseries/Data?{range}")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Client.GetAsync($"/api/timeseries/{id}?{range}")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var response = await Client.PutAsJsonAsync($"/api/timeseries/{id}",
            new { StartTime = start, EndTime = end, Energy = new { Value = 42m, Unit = EnergyUnit.MWh } });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<HttpResponseMessage> PutPoint(Guid id, DateTimeOffset start, decimal value, EnergyUnit unit = EnergyUnit.MWh)
    {
        var response = await Client.PutAsJsonAsync($"/api/timeseries/{id}",
            new { StartTime = start, EndTime = start.AddMinutes(15), Energy = new { Value = value, Unit = unit } });
        if (response.StatusCode == HttpStatusCode.InternalServerError)
            TestContext.Progress.WriteLine(await response.Content.ReadAsStringAsync());
        return response;
    }

    private async Task<TimeSeriesDataResponse> GetSeries(Guid id, string range = Range)
    {
        var response = await Client.GetAsync($"/api/timeseries/{id}?{range}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var series = await response.Content.ReadFromJsonAsync<TimeSeriesDataResponse>(JsonOptions);
        Assert.That(series, Is.Not.Null);
        return series!;
    }

    [Test]
    public async Task Create_ReturnsSeriesAndRejectsDuplicate()
    {
        var series = await CreateSeries(BiddingZone.No1);
        Assert.Multiple(() =>
        {
            Assert.That(series.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(series.BiddingZone, Is.EqualTo(BiddingZone.No1));
            Assert.That(series.Resolution, Is.EqualTo(Resolution.QuarterHour));
        });

        var duplicate = await Client.PostAsJsonAsync("/api/timeseries",
            new { BiddingZone = BiddingZone.No1, Resolution = Resolution.QuarterHour });
        Assert.That(duplicate.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That((await GetSeries(series.Id)).Id, Is.EqualTo(series.Id));
    }

    [Test]
    public async Task Retrieve_FiltersByZoneAndContainedTimeRange()
    {
        var series = await CreateSeries(BiddingZone.No2);
        var other = await CreateSeries(BiddingZone.No3);
        Assert.That((await PutPoint(series.Id, Start, 10)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await PutPoint(series.Id, Start.AddMinutes(30), 20)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await PutPoint(other.Id, Start, 30)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var listResponse = await Client.GetAsync("/api/timeseries?biddingZones=No2");
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var list = await listResponse.Content.ReadFromJsonAsync<TimeSeriesResponse[]>(JsonOptions);
        Assert.That(list!.Select(s => s.Id), Is.EquivalentTo(new[] { series.Id }));

        const string narrowRange = "startTime=2026-12-01T12%3A30%3A00Z&endTime=2026-12-01T12%3A45%3A00Z";
        var dataResponse = await Client.GetAsync($"/api/timeseries/Data?biddingZones=No2&{narrowRange}");
        Assert.That(dataResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var data = await dataResponse.Content.ReadFromJsonAsync<TimeSeriesDataResponse[]>(JsonOptions);
        Assert.That(data, Has.Length.EqualTo(1));
        Assert.That(data![0].Id, Is.EqualTo(series.Id));
        Assert.That(data[0].DataPoints, Has.Length.EqualTo(1));
        Assert.That(data[0].DataPoints[0].Energy.Value, Is.EqualTo(20));
        var byId = await GetSeries(series.Id, narrowRange);
        Assert.That(byId.DataPoints.Select(p => p.Id), Is.EqualTo(data[0].DataPoints.Select(p => p.Id)));

        Assert.That((await Client.GetAsync($"/api/timeseries/{Guid.NewGuid()}?{Range}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Client.GetAsync($"/api/timeseries/{series.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Client.GetAsync("/api/timeseries/Data?startTime=2026-12-01T13%3A00%3A00Z&endTime=2026-12-01T12%3A00%3A00Z")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Upsert_AdjacentPeriods_AreAccepted()
    {
        var series = await CreateSeries(BiddingZone.Se2);
        Assert.That((await PutPoint(series.Id, Start, 10)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await PutPoint(series.Id, Start.AddMinutes(15), 20)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetSeries(series.Id)).DataPoints, Has.Length.EqualTo(2));
    }

    [Test]
    public async Task Upsert_InsertsThenUpdatesSamePeriod()
    {
        var series = await CreateSeries(BiddingZone.No4);
        Assert.That((await PutPoint(series.Id, Start, 10)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var original = await GetSeries(series.Id);
        Assert.That(original.DataPoints, Has.Length.EqualTo(1));

        Assert.That((await PutPoint(series.Id, Start, 25, EnergyUnit.KWh)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var updated = await GetSeries(series.Id);
        Assert.That(updated.DataPoints, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(updated.DataPoints[0].Id, Is.EqualTo(original.DataPoints[0].Id));
            Assert.That(updated.DataPoints[0].StartTime, Is.EqualTo(Start));
            Assert.That(updated.DataPoints[0].EndTime, Is.EqualTo(Start.AddMinutes(15)));
            Assert.That(updated.DataPoints[0].Energy.Value, Is.EqualTo(25));
            Assert.That(updated.DataPoints[0].Energy.Unit, Is.EqualTo(EnergyUnit.KWh));
        });
        Assert.That((await PutPoint(Guid.NewGuid(), Start, 10)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_RemovesOnlyPointBelongingToSeries()
    {
        var series = await CreateSeries(BiddingZone.No5);
        var other = await CreateSeries(BiddingZone.Fi);
        Assert.That((await PutPoint(series.Id, Start, 10)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var point = (await GetSeries(series.Id)).DataPoints.Single();

        Assert.That((await Client.DeleteAsync($"/api/timeseries/{other.Id}/DataPoints/{point.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await GetSeries(series.Id)).DataPoints, Has.Length.EqualTo(1));
        Assert.That((await Client.DeleteAsync($"/api/timeseries/{series.Id}/DataPoints/{point.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetSeries(series.Id)).DataPoints, Is.Empty);
        Assert.That((await Client.DeleteAsync($"/api/timeseries/{series.Id}/DataPoints/{point.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}