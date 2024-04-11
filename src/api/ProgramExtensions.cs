namespace traduire.webapi;

public static class ProgramExtensions
{
    public static void AddCustomOtelConfiguration (this IServiceCollection services, string ApplicationName,  Uri otelCollectorEndpoint)
    {
        var otel = services.AddOpenTelemetry();
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
            .AddOtlpExporter(otlpOptions => {
                otlpOptions.Endpoint = otelCollectorEndpoint;
            })
        );

        otel.WithTracing( tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(traduireActivitySource.Name)
            .AddConsoleExporter()
            .AddOtlpExporter(otlpOptions => {
                otlpOptions.Endpoint = otelCollectorEndpoint;
            })
        );
    }
}