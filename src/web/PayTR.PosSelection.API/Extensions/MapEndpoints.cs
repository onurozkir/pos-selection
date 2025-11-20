using System.Net.Mime;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace PayTR.PosSelection.API.Extensions;

public static class MapEndpoints
{
    public static WebApplication MapEndpoint(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();
        
        app.MapGet("/", () => "POS Selection API is running.");
        
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = MediaTypeNames.Application.Json;

                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        error = x.Value.Exception?.Message
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                });

                await context.Response.WriteAsync(result);
            }
        }); 
        app.MapHealthChecks("/health/ready", 
            new HealthCheckOptions { Predicate = _ => true });

        app.MapHealthChecks("/health/live",
            new HealthCheckOptions { Predicate = r => r.Name == "self" });
        

        // POS seçim endpoint’i
        app.MapPost("v{version:apiVersion}/pos-selection", Endpoints.v1.Pos.PosSelection)
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0));

        return app;
    }
}