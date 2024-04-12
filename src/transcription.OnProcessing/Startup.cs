using Microsoft.Extensions.Azure;

using transcription.models;
using transcription.actors;
using transcription.common.cognitiveservices;

namespace transcription.processing
{
    public class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("*");
                    });
            });

            services.AddControllers();

            var region = Environment.GetEnvironmentVariable("AZURE_COGS_REGION");
            var cogs = new AzureCognitiveServicesClient(Configuration[Components.SecretName], region);
            services.AddSingleton<AzureCognitiveServicesClient>(cogs);

            services.AddActors(options =>
            {
                options.Actors.RegisterActor<TranscriptionActor>();
            });

            services.AddAzureClients(builder =>
            {
                builder.AddWebPubSubServiceClient(Configuration[Components.PubSubSecretName], Components.PubSubHubName);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();
            app.UseCloudEvents();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapActorsHandlers();
                endpoints.MapControllers();
            });
        }
    }
}
