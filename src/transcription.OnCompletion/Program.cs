using Dapr.Client;
using Dapr.Extensions.Configuration;
using transcription.oncompletion;

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

builder.AddCustomOtelConfiguration(
    config["appname"], 
    config["otel_collection_endpoint"]
);

var daprClient = new DaprClientBuilder().Build();
builder.Configuration.AddDaprSecretStore(Components.SecureStore, daprClient);

builder.Services.AddCors(options => {
    options.AddDefaultPolicy( builder => {
        builder.WithOrigins("*");
    });
});

builder.Services.AddControllers();

var region = Environment.GetEnvironmentVariable("AZURE_COGS_REGION");
var cogs = new AzureCognitiveServicesClient(config[Components.SecretName], region);

builder.Services.AddSingleton<DaprClient>(daprClient);
builder.Services.AddSingleton<AzureCognitiveServicesClient>(cogs);
builder.Services.AddAzureClients(builder => {
    builder.AddWebPubSubServiceClient(config[Components.PubSubSecretName], Components.PubSubHubName);
});
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCloudEvents();
app.UseRouting();
app.UseAuthorization();
app.MapHealthChecks("/healthz");
app.MapControllers();
app.MapSubscribeHandler();

app.Logger.LogInformation( $"{builder.Environment.ApplicationName} - App Run" );
app.Run();