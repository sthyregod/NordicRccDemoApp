using Microsoft.EntityFrameworkCore;
using NordicRccDemoApp.Common;
using NordicRccDemoApp.Data;
using NordicRccDemoApp.Dtos;
using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;
using NordicRccDemoApp.Service;
using NordicRccDemoAppTest.Infrastructure;

namespace NordicRccDemoAppTest.Services;

public class TimeSeriesServiceTests : DatabaseTestBase
{
    private TimeSeriesService CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Container.GetConnectionString())
            .Options;
        return new TimeSeriesService(new AppDbContext(options));
    }

    private async Task<Guid> SeedSeries(BiddingZone zone, Resolution resolution)
    {
        await using var db = CreateDb();
        var series = new TimeSeries
        {
            BiddingZone = zone,
            Resolution = resolution
        };
        db.TimeSeries.Add(series);
        await db.SaveChangesAsync();
        return series.Id;
    }
    
    private static DateTimeOffset BaseTime => new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task UpsertDataPoint_WithValidPeriod_ReturnsSuccess()
    {
        var service = CreateService();
        var id = await SeedSeries(BiddingZone.Dk1, Resolution.QuarterHour);

        var result = await service.UpsertDataPoint(id, new UpsertDataPointDto(BaseTime, BaseTime.AddMinutes(15), new Energy(100, EnergyUnit.MWh)));
        
        Assert.IsTrue(result.IsSuccess);
    }
    
    [Test]
    public async Task UpsertDataPoint_WithNonexistentSeries_ReturnsNotFound()
    {
        var service = CreateService();

        var result = await service.UpsertDataPoint(Guid.NewGuid(), new UpsertDataPointDto(BaseTime, BaseTime.AddMinutes(15), new Energy(100, EnergyUnit.MWh)));

        Assert.That(result.Outcome, Is.EqualTo(Outcome.NotFound));
    }

    [Test]
    public async Task UpsertDataPoint_WithInvalidPeriod_ReturnsBadRequest()
    {
        var id = await SeedSeries(BiddingZone.Dk1, Resolution.QuarterHour);
        var service = CreateService();
        
        var result = await service.UpsertDataPoint(id, new UpsertDataPointDto(BaseTime, BaseTime.AddMinutes(16), new Energy(100, EnergyUnit.MWh)));

        Assert.That(result.Outcome, Is.EqualTo(Outcome.BadRequest));
    }

    [Test]
    public async Task UpsertDataPoint_ExistingPeriod_UpdatesEnergyInsteadOfInserting()
    {
        var id = await SeedSeries(BiddingZone.Dk1, Resolution.QuarterHour);
        var service = CreateService();

        await service.UpsertDataPoint(id, new UpsertDataPointDto(BaseTime, BaseTime.AddMinutes(15), new Energy(100, EnergyUnit.MWh)));
        
        await service.UpsertDataPoint(id, new UpsertDataPointDto(BaseTime, BaseTime.AddMinutes(15), new Energy(200, EnergyUnit.MWh)));

        await using var db = CreateDb();
        var dataPoints = await db.DataPoints.Where(dp => dp.TimeSeriesId == id).ToListAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(dataPoints.Count, Is.EqualTo(1));
            Assert.That(dataPoints[0].Energy, Is.EqualTo(new Energy(200, EnergyUnit.MWh)));
        });
    }

    [Test]
    public async Task CreateTimeSeries_WithDuplicateZoneAndResolution_ReturnsConflict()
    {
        await SeedSeries(BiddingZone.Dk1, Resolution.QuarterHour);
        var service = CreateService();

        var result = await service.CreateTimeSeries(new CreateTimeSeriesDto(BiddingZone.Dk1, Resolution.QuarterHour));
            
        Assert.That(result.Outcome, Is.EqualTo(Outcome.Conflict));
    }
    
    [Test]
    public async Task CreateTimeSeries_WithNewZone_ReturnsSuccessWithGeneratedId()
    {
        var service = CreateService();

        var result = await service.CreateTimeSeries(new CreateTimeSeriesDto(BiddingZone.Se1, Resolution.QuarterHour));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(Outcome.Success));
            Assert.That(result.Value!.Id, Is.Not.EqualTo(Guid.Empty));
        });
        
    }
}