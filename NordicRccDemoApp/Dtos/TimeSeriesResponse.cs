using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;

namespace NordicRccDemoApp.Dtos;

public record TimeSeriesResponse(
    Guid Id,
    BiddingZone BiddingZone,
    Resolution Resolution)
{
    public static TimeSeriesResponse FromEntity(TimeSeries ts) => new(
        ts.Id,
        ts.BiddingZone,
        ts.Resolution);
}