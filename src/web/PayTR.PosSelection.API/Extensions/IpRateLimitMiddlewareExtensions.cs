using PayTR.PosSelection.Infrastructure.Middlewares;

namespace PayTR.PosSelection.API.Extensions
{
    public static class IpRateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseIpRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<IpRateLimitMiddleware>();
        }
    }
}