using src.Domain;

namespace src.Data;

public class DataContext(DbContextOptions<DataContext> options)
    : DbContext(options)
{
    public DbSet<WeatherForecast> WeatherForecasts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherForecast>().HasKey(wf => wf.Id);
        base.OnModelCreating(modelBuilder);
    }
}
