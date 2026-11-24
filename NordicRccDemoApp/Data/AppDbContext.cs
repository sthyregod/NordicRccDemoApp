using Microsoft.EntityFrameworkCore;
using NordicRccDemoApp.Models;

namespace NordicRccDemoApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TimeSeries> TimeSeries { get; set; }
    public DbSet<DataPoint> DataPoints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataPoint>().Property(dp => dp.Period).HasColumnType("tstzrange");

        modelBuilder.Entity<TimeSeries>(entity =>
        {
            entity.Property(ts => ts.Resolution).HasConversion<string>();
            entity.Property(ts => ts.BiddingZone).HasConversion<string>();
            
            entity.HasMany(ts => ts.DataPoints)
                .WithOne(dp => dp.TimeSeries)
                .HasForeignKey(dp => dp.TimeSeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}