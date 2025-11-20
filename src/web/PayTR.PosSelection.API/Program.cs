using Asp.Versioning;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using PayTR.PosSelection.API.Extensions;
using PayTR.PosSelection.Infrastructure;
using PayTR.PosSelection.Infrastructure.Middlewares;
using PayTR.PosSelection.Infrastructure.Models.PosSelection.Requests;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("config/appsettings.json", false, true);
var services = builder.Services;


var redisConn = builder.Configuration.GetSection("Redis")["ConnectionString"];
var postgresConn = builder.Configuration.GetSection("ConnectionStrings")["DefaultConnection"];

services.AddInfra(builder.Configuration);

services
    .AddHealthChecks()
    .AddNpgSql(
        connectionString: postgresConn,
        name: "postgresql",
        timeout: TimeSpan.FromSeconds(3))
    .AddRedis(
        redisConnectionString: redisConn,
        name: "redis",
        timeout: TimeSpan.FromSeconds(3))
    .AddCheck("self", () => HealthCheckResult.Healthy());

services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader());
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

services.AddHttpContextAccessor();
services.AddHttpClient();
services.AddValidatorsFromAssemblyContaining<PosSelectionRequestValidator>();


var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "pos-selection-api",
        serviceVersion: "1.0.0")
    .AddAttributes(new[]
    {
        new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
        new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)
    });

builder.Services.AddOpenTelemetry() 
    .WithTracing(tracer =>
    {
        tracer
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = ctx => true; // istersen health endpointleri ignore edersin
            }) // request
            .AddHttpClientInstrumentation() // http trace 
            .AddNpgsql()  // postgreSQL trace
            .AddSource("PayTR.PosSelection") // custom ActivitySource kullanırsan
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri("http://otel-collector:4317");
            });
    })
    .WithMetrics(meter =>
    {
        meter
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation() // request metrics (durations, active req, vs.)
            .AddHttpClientInstrumentation() // HTTP client metrics
            .AddRuntimeInstrumentation() //Runtime metrics (CPU, GC, heap, threads vs.)
            .AddMeter("Npgsql") // PostgreSQL metrics (komut sayısı vs)
            .AddPrometheusExporter(); // metrikleri prometheus'a aktar -> ordan grafana dashboard yap
    });


// console log
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logOptions =>
{
    logOptions.SetResourceBuilder(resourceBuilder);
    logOptions.IncludeScopes = true;
    logOptions.ParseStateValues = true;
    logOptions.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://otel-collector:4317"); 
    });
});

var app = builder.Build();

app.UseMiddleware(typeof(ExceptionMiddleware));
app.UseIpRateLimiting();
app.UseRouting();
app.MapEndpoint();

app.Run(); 
