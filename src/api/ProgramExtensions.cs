namespace traduire.webapi;

public static class ProgramExtensions
{
    public static void AddCustomOtelConfiguration (this IServiceCollection services, string ApplicationName, string otelConnnectionString, string azureMonitorConnectionString)
    {
        var credential = new DefaultAzureCredential();
        var otel = services.AddOpenTelemetry()
            .UseAzureMonitor( o => {
                o.ConnectionString = azureMonitorConnectionString;
                o.Credential = credential;
                o.SamplingRatio = 0.1F;
            });

        var traduireApiMeter = new Meter("traduire", "2.0.0");
        var traduireActivitySource = new ActivitySource("traduire.api");

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
            .AddOtlpExporter(opt =>
            {
                opt.Protocol = OtlpExportProtocol.Grpc;
                opt.Endpoint = new Uri(otelConnnectionString);
            })
        );

        otel.WithTracing( tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(traduireActivitySource.Name)
            .AddConsoleExporter()
            .AddOtlpExporter(opt =>
            {
                opt.Protocol = OtlpExportProtocol.Grpc;
                opt.Endpoint = new Uri(otelConnnectionString);
            })
        );
    }
}