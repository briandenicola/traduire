using Microsoft.AspNetCore.Server.Kestrel.Core;
using Dapr.Client;
using transcription.api.dapr;
using traduire.webapi;

var configBuilder = new ConfigurationBuilder();
configBuilder.AddEnvironmentVariables(prefix: "TRADUIRE_");
var config = configBuilder.Build();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logging => { 
    logging.IncludeScopes = true;
    logging.AddConsoleExporter();
    logging.AddOtlpExporter(otlpOptions => {
        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
        otlpOptions.Endpoint = new Uri(config["otel_collection_endpoint"]);
    });
});

builder.WebHost.ConfigureKestrel(opts => {
    opts.ListenAnyIP(9091, o => o.Protocols = HttpProtocols.Http1);
    opts.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http1AndHttp2);
});

builder.AddCustomOtelConfiguration(
    config["appname"], 
    config["otel_collection_endpoint"]
);

builder.Services.AddCors(options => {
    options.AddDefaultPolicy( builder => {
        builder.WithOrigins("*");
    });
});

var client = new DaprTranscriptionService(new DaprClientBuilder().Build());
builder.Services.AddSingleton<DaprTranscriptionService>(client);
builder.Services.AddControllers().AddDapr();
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapPrometheusScrapingEndpoint().RequireHost("*:9091");
app.MapHealthChecks("/healthz");
app.MapControllers();
app.MapGrpcService<TranscriberService>();
app.MapGrpcReflectionService();

app.Logger.LogInformation( $"{builder.Environment.ApplicationName} - App Run" );
app.Run();