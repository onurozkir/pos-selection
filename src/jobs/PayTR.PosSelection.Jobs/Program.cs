using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PayTR.PosSelection.API.Extensions;
using PayTR.PosSelection.Infrastructure;
using PayTR.PosSelection.Infrastructure.Models.RatiosJob;
using PayTR.PosSelection.Jobs.Jobs;
using Quartz;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.Sources.Clear();
        config.SetBasePath(AppContext.BaseDirectory); 
        config.AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true); 
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService("pos-selection-job",
                serviceVersion: "1.0.0")
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)
            });

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddOpenTelemetry(logOptions =>
            {
                logOptions.SetResourceBuilder(resourceBuilder);
                logOptions.IncludeScopes = true;
                logOptions.ParseStateValues = true;
                logOptions.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri("http://otel-collector:4317");
                });
            });
        });

        services.AddOpenTelemetry()
            .WithTracing(tracer =>
            {
                tracer
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation() // http trace 
                    .AddNpgsql() // postgreSQL trace
                    .AddSource("PayTR.PosSelection") // custom ActivitySource kullanırsan
                    .AddOtlpExporter(otlpOptions => { otlpOptions.Endpoint = new Uri("http://otel-collector:4317"); });
            })
            .WithMetrics(meter =>
            {
                meter
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation() // HTTP client metrics
                    .AddRuntimeInstrumentation() //Runtime metrics (CPU, GC, heap, threads vs.)
                    .AddMeter("Npgsql"); // PostgreSQL metrics (komut sayısı vs)
            });

        services.Configure<RatiosJobOptions>(
            configuration.GetSection(RatiosJobOptions.SectionName));
            
        services.AddCircuitBreakerPolicies();

        services.AddInfra(configuration);

        services
            .AddQuartz(q =>
            {
                // deprecedated uyarısı
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.MisfireThreshold = TimeSpan.FromMilliseconds(3000);

                var jobKey = new JobKey(nameof(RatiosJobs));

                q.AddJob<RatiosJobs>(opts =>
                    opts.WithIdentity(jobKey));

                var cronExpr = configuration[$"{RatiosJobOptions.SectionName}:Cron"]
                               ?? "0/10 * * * * ?";

                // 23:59:01
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"{nameof(RatiosJobs)}-trigger")
                    .WithCronSchedule(cronExpr, cron =>
                    {
                        cron.InTimeZone(
                            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
                    }));

                // 00:00 - 00:15 arası her dakika
                // q.AddTrigger(opts => opts
                //     .ForJob(jobKey)
                //     .WithIdentity("Trigger_00_00_to_00_15")
                //     .WithCronSchedule(cronMidnightExpr, cron =>
                //     {
                //         cron.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
                //     })
                // );
            })
            .AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    });


var host = builder.Build();
await host.RunAsync();