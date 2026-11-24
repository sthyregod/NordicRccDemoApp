using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using NordicRccDemoApp.Enums;
using NpgsqlTypes;

namespace NordicRccDemoApp.Models;

public class TimeSeries
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public BiddingZone BiddingZone { get; init; }
    
    [Required]
    public Resolution Resolution { get; init; }

    public List<DataPoint> DataPoints { get; private set; } = [];

    public bool IsValidPeriod(NpgsqlRange<DateTimeOffset> period)
    {
        var expected = Resolution switch 
        {
            Resolution.QuarterHour => TimeSpan.FromMinutes(15),
            Resolution.Hour => TimeSpan.FromHours(1),
            _ => throw new UnreachableException(nameof(Resolution))
        };
        
        if (period.UpperBound - period.LowerBound != expected)
        {
            return false;
        }
        
        if (!IsOnMinuteBoundary(period.LowerBound))
            return false;
        
        return Resolution switch
        {
            Resolution.QuarterHour => period.LowerBound.Minute % 15 == 0,
            Resolution.Hour => period.LowerBound.Minute == 0,
            _ => false
        };
    }
    
    private static bool IsOnMinuteBoundary(DateTimeOffset t) =>
        t.TimeOfDay.Ticks % TimeSpan.TicksPerMinute == 0;
}