using Microsoft.EntityFrameworkCore;
using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;
using NpgsqlTypes;

namespace NordicRccDemoApp.Data;

public static class DbSeeder
{
    public static async Task SeedIfEmpty(AppDbContext db)
    {
        if (await db.TimeSeries.AnyAsync()) return;
        
        var now = new DateTime(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc);

        var dk1SeriesQuarterHourDataPoints = GenerateDataPoints(
            now,
            now + TimeSpan.FromHours(24),
            new Energy(200, EnergyUnit.MWh),
            Resolution.QuarterHour,
            new Energy(10, EnergyUnit.MWh)
        );
        
        var dk1SeriesHourDataPoints = GenerateDataPoints(
            now,
            now + TimeSpan.FromHours(24),
            new Energy(250, EnergyUnit.MWh),
            Resolution.Hour,
            new Energy(10, EnergyUnit.MWh)
        );
        
        var se1SeriesQuarterHourDataPoints = GenerateDataPoints(
            now,
            now + TimeSpan.FromHours(24),
            new Energy(250, EnergyUnit.MWh),
            Resolution.QuarterHour,
            new Energy(10, EnergyUnit.MWh)
        );
        
        var se1SeriesHourDataPoints = GenerateDataPoints(
            now,
            now + TimeSpan.FromHours(24),
            new Energy(250, EnergyUnit.MWh),
            Resolution.Hour,
            new Energy(10, EnergyUnit.MWh)
        );
        
        var dk1SeriesQuarterHour = new TimeSeries
        {
            BiddingZone = BiddingZone.Dk1,
            Resolution = Resolution.QuarterHour
        };
        dk1SeriesQuarterHour.DataPoints.AddRange(dk1SeriesQuarterHourDataPoints);
        
        var dk1SeriesHour = new TimeSeries
        {
            BiddingZone = BiddingZone.Dk1,
            Resolution = Resolution.Hour
        };
        dk1SeriesHour.DataPoints.AddRange(dk1SeriesHourDataPoints);

        var se1SeriesQuarterHour = new TimeSeries
        {
            BiddingZone = BiddingZone.Se1,
            Resolution = Resolution.QuarterHour
        };
        se1SeriesQuarterHour.DataPoints.AddRange(se1SeriesQuarterHourDataPoints);

        var se1SeriesHour = new TimeSeries
        {
            BiddingZone = BiddingZone.Se1,
            Resolution = Resolution.Hour
        };
        se1SeriesHour.DataPoints.AddRange(se1SeriesHourDataPoints);
        
        db.TimeSeries.AddRange(
            dk1SeriesQuarterHour,
            dk1SeriesHour,
            se1SeriesQuarterHour,
            se1SeriesHour
        );

        await db.SaveChangesAsync();
    }

    private static List<DataPoint> GenerateDataPoints(
        DateTime startTime,
        DateTime endTime,
        Energy startEnergy,
        Resolution resolution,
        Energy deviationThreshold,
        int seed = 42)
    {
        var step = resolution switch
        {
            Resolution.QuarterHour => TimeSpan.FromMinutes(15),
            Resolution.Hour => TimeSpan.FromHours(1),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution))
        };

        var rng = new Random(seed);
        var dataPoints = new List<DataPoint>();
        var currentOffset = startTime;
        var currentEnergy = startEnergy;

        while (currentOffset < endTime)
        {
            dataPoints.Add(new DataPoint()
            {
                Period = new NpgsqlRange<DateTimeOffset>(currentOffset, true, currentOffset + step, false),
                Energy = currentEnergy
            });
            
            currentEnergy += RandomDeviation(deviationThreshold, rng);
            currentOffset += step;
        }
        
        return dataPoints;
    }
    
    private static Energy RandomDeviation(Energy threshold, Random rng)
    {
        var magnitude = Math.Round((decimal)rng.NextDouble(), 2);
        var sign = rng.Next(2) == 0 ? 1m : -1m;
        return threshold * magnitude * sign;
    }
}