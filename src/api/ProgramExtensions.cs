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

        otel.WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(traduireApiMeter.Name)
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddPrometheusExporter());

        otel.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddSource(traduireActivitySource.Name);
            tracing.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = otelCollectorEndpoint;
            });
        });
    }
}