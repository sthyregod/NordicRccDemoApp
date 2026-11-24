using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace NordicRccDemoApp.Models;

public class DataPoint
{
    [Key]
    public Guid Id { get; init; }
    
    [ForeignKey(nameof(TimeSeries))]
    public Guid TimeSeriesId { get; init; }

    public TimeSeries TimeSeries { get; private set; } = null!;

    public NpgsqlRange<DateTimeOffset> Period { get; set; }
    
    [Required]
    public required Energy Energy { get; set; }
}