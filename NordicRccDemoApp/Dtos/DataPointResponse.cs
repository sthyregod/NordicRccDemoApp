using NordicRccDemoApp.Models;

namespace NordicRccDemoApp.Dtos;

public record DataPointResponse(Guid Id, DateTimeOffset StartTime, DateTimeOffset EndTime, Energy Energy)
{
    public static DataPointResponse FromEntity(DataPoint dp) => new(
        dp.Id,
        dp.Period.LowerBound,
        dp.Period.UpperBound,
        dp.Energy);
}