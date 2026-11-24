using Microsoft.AspNetCore.Mvc;
using NordicRccDemoApp.Common;
using NordicRccDemoApp.Dtos;
using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Service;

namespace NordicRccDemoApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeSeriesController(TimeSeriesService timeSeriesService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TimeSeriesResponse[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TimeSeriesResponse>>> GetListOfTimeSeries([FromQuery] List<BiddingZone>? biddingZones)
    {
        var series = await timeSeriesService.GetList(biddingZones);
        
        return Ok(series.Select(TimeSeriesResponse.FromEntity).ToList());
    }
    
    [HttpGet("Data")]
    [ProducesResponseType<TimeSeriesDataResponse[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TimeSeriesDataResponse>>> GetListOfTimeSeries([FromQuery] List<BiddingZone>? biddingZones, [FromQuery] DateTimeOffset? startTime, [FromQuery] DateTimeOffset? endTime)
    {
        if (startTime is null || endTime is null)
            return Problem(detail: "Start and end time must be provided", statusCode: StatusCodes.Status400BadRequest);
        if (startTime.Value.Offset != TimeSpan.Zero || endTime.Value.Offset != TimeSpan.Zero)
            return Problem(detail: "Start and end time must be UTC", statusCode: StatusCodes.Status400BadRequest);
        
        if (startTime >= endTime)
            return Problem(detail: "Start time must be before end time", statusCode: StatusCodes.Status400BadRequest);
        
        var series = await timeSeriesService.GetData(startTime.Value, endTime.Value, biddingZones);
        
        return Ok(series.Select(TimeSeriesDataResponse.FromEntity).ToList());
    }
    
    [HttpPost]
    [ProducesResponseType<TimeSeriesResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TimeSeriesResponse>> CreateNewTimeSeries([FromBody] CreateTimeSeriesDto dto)
    {
        var result = await timeSeriesService.CreateTimeSeries(dto);

        if (result.Outcome == Outcome.Conflict)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status409Conflict);

        var response = TimeSeriesResponse.FromEntity(result.Value!);

        return CreatedAtAction(nameof(GetTimeSeriesMetadata), new { id = response.Id }, response);
    }

    [HttpGet("{id}/metadata")]
    [ProducesResponseType<TimeSeriesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TimeSeriesResponse>> GetTimeSeriesMetadata([FromRoute] Guid id)
    {
        var series = await timeSeriesService.GetMetadata(id);
        if (series is null)
            return Problem(detail: "Time series not found", statusCode: StatusCodes.Status404NotFound);

        return Ok(TimeSeriesResponse.FromEntity(series));
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType<TimeSeriesDataResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TimeSeriesDataResponse>> GetTimeSeriesById([FromRoute] Guid id, [FromQuery] DateTimeOffset? startTime, [FromQuery] DateTimeOffset? endTime)
    {
        if (startTime is null || endTime is null)
            return Problem(detail: "Start and end time must be provided", statusCode: StatusCodes.Status400BadRequest);
        if (startTime.Value.Offset != TimeSpan.Zero || endTime.Value.Offset != TimeSpan.Zero)
            return Problem(detail: "Start and end time must be UTC", statusCode: StatusCodes.Status400BadRequest);
        
        if (startTime >= endTime)
            return Problem(detail: "Start time must be before end time", statusCode: StatusCodes.Status400BadRequest);
        
        var result = await timeSeriesService.GetTimeSeriesById(id, startTime.Value, endTime.Value);

        if (result.Outcome == Outcome.NotFound)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status404NotFound);

        return Ok(TimeSeriesDataResponse.FromEntity(result.Value!));
    }

    [HttpPut("{timeSeriesId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpsertDataPoint([FromRoute] Guid timeSeriesId, [FromBody] UpsertDataPointDto dto)
    {
        if (dto.StartTime.Offset != TimeSpan.Zero || dto.EndTime.Offset != TimeSpan.Zero)
            return Problem(detail: "Start and end time must be UTC", statusCode: StatusCodes.Status400BadRequest);

        var result = await timeSeriesService.UpsertDataPoint(timeSeriesId, dto);

        return result.Outcome switch
        {
            Outcome.Success => NoContent(),
            Outcome.NotFound => Problem(detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            Outcome.BadRequest => Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            _ => throw new InvalidOperationException($"Unexpected outcome: {result.Outcome}")
        };
    }

    [HttpDelete("{timeSeriesId}/DataPoints/{dataPointId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDataPoint([FromRoute] Guid timeSeriesId, [FromRoute] Guid dataPointId)
    {
        var result = await timeSeriesService.DeleteDataPoint(timeSeriesId, dataPointId);

        return result.Outcome switch
        {
            Outcome.Success => NoContent(),
            Outcome.NotFound => Problem(detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => throw new InvalidOperationException($"Unexpected outcome: {result.Outcome}")
        };
    }
}