using Npgsql;

using src.Data;
using src.Domain;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "AspireMetrics";

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(
        ResourceBuilder
            .CreateDefault()
            .AddService(serviceName))
            .AddOtlpExporter()
    ;
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter()
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsqlInstrumentation()
        .AddOtlpExporter()
    )
;

builder.Services.AddDbContextFactory<DataContext>(factory =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    factory.UseNpgsql(connectionString, options =>
    {
        options.EnableRetryOnFailure();
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (ILogger<Program> logger, DataContext context) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            Guid.CreateVersion7(),
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    await context.WeatherForecasts.AddRangeAsync(forecast);
    await context.SaveChangesAsync();
    logger.LogInformation("{Endpoint} called with {Count} items", "GetWeatherForecast", forecast.Length);
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/", async (ILogger<Program> logger, DataContext context) =>
{
    await context.Database.EnsureCreatedAsync();
    var items = await context.WeatherForecasts.Where(w => w.Date >= DateOnly.FromDateTime(DateTime.Now))
        .OrderBy(w => w.Date)
        .ToListAsync();
    logger.LogInformation("{Endpoint} called with {Count} items", "GetRoot", items.Count);
    return items;
})
.WithName("GetRoot");

app.Run();
