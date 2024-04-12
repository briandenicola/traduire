using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using Azure.Messaging.WebPubSub;

using transcription.models;
using transcription.common.cognitiveservices;

namespace transcription.TranslationOnStarted
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
            _ = services.AddSingleton<AzureCognitiveServicesClient>(cogs);

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
                endpoints.MapControllers();
            });
        }
    }
}
