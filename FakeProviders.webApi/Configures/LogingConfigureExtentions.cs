using Serilog;

namespace FakeProviders.webApi.Configures;


public static class LogingConfigureExtentions
{
    public static WebApplicationBuilder AddLoggingBuildConfigure(this WebApplicationBuilder builder)
    {
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs\\Serilog");
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration) // یا تنظیمات دستی
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }


    public static void AddLoggingRuntimeConfigure(this WebApplication app)
    {
        // فعال کردن Request Logging خودکار Serilog
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("ClientIp", GetClientIpAddress(httpContext));
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            };
        });
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // 1- First X-Forwarded-For For when set use LoadBalancer or proxy service 
        string? forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        // 2-  X-Real-IP header 
        string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // 3. get default RemoteIpAddress
        var remoteIp = context.Connection.RemoteIpAddress;
        return remoteIp?.ToString() ?? "Unknown";
    }


}