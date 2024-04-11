namespace traduire.webapi;

public static class ProgramExtensions
{
    public static void AddCustomOtelConfiguration (this IServiceCollection services, string ApplicationName,  string azureMonitorConnectionString)
    {
        var otel = services.AddOpenTelemetry();

        var traduireApiMeter = new Meter("traduire", "2.0.0");
        var traduireActivitySource = new ActivitySource("traduire.api");
        var credential = new DefaultAzureCredential();

        otel.ConfigureResource(resource => resource
            .AddService(serviceName: ApplicationName));
            
        otel.WithMetrics( metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(traduireApiMeter.Name)
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddPrometheusExporter()
            .AddConsoleExporter()
            .AddAzureMonitorMetricExporter(o => {
                o.ConnectionString = azureMonitorConnectionString;
                o.Credential = credential;
            })
        );

        otel.WithTracing( tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(traduireActivitySource.Name)
            .AddConsoleExporter()
            .AddAzureMonitorTraceExporter(o => {
                o.ConnectionString = azureMonitorConnectionString;
                o.Credential = credential;
            })
        );
    }
}