namespace transcription.oncompletion;

public static class ProgramExtensions
{
    public static void AddCustomOtelConfiguration (this WebApplicationBuilder builder, string ApplicationName, string otelConnnectionString)
    {
        var credential = new DefaultAzureCredential();
        var otel = builder.Services.AddOpenTelemetry();

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