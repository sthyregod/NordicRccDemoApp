using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;

namespace NordicRccDemoApp.Dtos;

public record TimeSeriesDataResponse(
    Guid Id,
    BiddingZone BiddingZone,
    Resolution Resolution,
    DataPointResponse[] DataPoints)
{
    public static TimeSeriesDataResponse FromEntity(TimeSeries ts) => new(
        ts.Id,
        ts.BiddingZone,
        ts.Resolution,
        [.. ts.DataPoints.Select(DataPointResponse.FromEntity).ToArray()]);
}