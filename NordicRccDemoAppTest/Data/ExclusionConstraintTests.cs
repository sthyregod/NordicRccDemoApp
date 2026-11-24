using Microsoft.EntityFrameworkCore;
using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;
using NordicRccDemoAppTest.Infrastructure;
using NpgsqlTypes;

namespace NordicRccDemoAppTest.Data;

public class ExclusionConstraintTests : DatabaseTestBase
{
    [Test]
    public async Task OverlappingPeriodsInSameSeries_ThrowsDbUpdateException()
    {
        await using var db = CreateDb();

        var series = new TimeSeries
        {
            BiddingZone = BiddingZone.Dk1,
            Resolution = Resolution.QuarterHour
        };
        db.TimeSeries.Add(series);
        await db.SaveChangesAsync();
        
        var start = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

        db.DataPoints.Add(new DataPoint
        {
            TimeSeriesId = series.Id,
            Period = new NpgsqlRange<DateTimeOffset>(start, start.AddMinutes(15)),
            Energy = new Energy(42.5m, EnergyUnit.MWh)
        });
        
        db.DataPoints.Add(new DataPoint
        {
            TimeSeriesId = series.Id,
            Period = new NpgsqlRange<DateTimeOffset>(start.AddMinutes(10), start.AddMinutes(25)),
            Energy = new Energy(43.0m, EnergyUnit.MWh)
        });
        
        var ex = Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.That(ex.InnerException?.Message ?? "", Does.Contain("NoDpOverlap"));
    }

    [Test]
    public async Task NonOverlappingPeriodsInSameSeries_Succeed()
    {
        await using var db = CreateDb();

        var series = new TimeSeries
        {
            BiddingZone = BiddingZone.Dk1,
            Resolution = Resolution.QuarterHour
        };

        db.TimeSeries.Add(series);
        await db.SaveChangesAsync();

        var start = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        
        db.DataPoints.Add(new DataPoint
        {
            TimeSeriesId = series.Id,
            Period = new NpgsqlRange<DateTimeOffset>(start, true, start.AddMinutes(15), false),
            Energy = new Energy(42.5m, EnergyUnit.MWh)
        });
        
        db.DataPoints.Add(new DataPoint
        {
            TimeSeriesId = series.Id,
            Period = new NpgsqlRange<DateTimeOffset>(start.AddMinutes(15), true, start.AddMinutes(30), false),
            Energy = new Energy(43.0m, EnergyUnit.MWh)
        });

        await db.SaveChangesAsync();
        
        var count = await db.DataPoints.CountAsync(dp => dp.TimeSeriesId == series.Id);
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task OverlappingPeriodsInDifferentSeries_Succeed()
    {
        await using var db = CreateDb();

        var start = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var period = new NpgsqlRange<DateTimeOffset>(start, true, start.AddMinutes(15), false);
        
        var s1 = new TimeSeries { BiddingZone = BiddingZone.Dk1, Resolution = Resolution.QuarterHour };
        var s2 = new TimeSeries { BiddingZone = BiddingZone.Dk2, Resolution = Resolution.QuarterHour };
        db.TimeSeries.AddRange(s1, s2);
        await db.SaveChangesAsync();
        
        db.DataPoints.Add(new DataPoint { TimeSeriesId = s1.Id, Period = period, Energy = new Energy(10m, EnergyUnit.MWh) });
        db.DataPoints.Add(new DataPoint { TimeSeriesId = s2.Id, Period = period, Energy = new Energy(20m, EnergyUnit.MWh) });
        await db.SaveChangesAsync();

        var count1 = await db.DataPoints.CountAsync(dp => dp.TimeSeriesId == s1.Id);
        var count2 = await db.DataPoints.CountAsync(dp => dp.TimeSeriesId == s2.Id);
        Assert.Multiple(() =>
        {
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(1));
        });
    }
}