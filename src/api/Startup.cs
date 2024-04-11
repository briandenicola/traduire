using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Dapr;
using Dapr.Client;
using Grpc.AspNetCore.Server;

using transcription.api.dapr;

namespace traduire.webapi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var builder = new ConfigurationBuilder();  
            builder.AddEnvironmentVariables(prefix: "TRADUIRE_");
            var config = builder.Build();

            services.AddHealthChecks();
            services.AddCustomOtelConfiguration( config["appname"], new Uri(config["otel_collection_endpoint"]) );
            
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("*");
                    });
            });

            services.AddControllers().AddDapr();

            var client = new DaprTranscriptionService(new DaprClientBuilder().Build());
            services.AddSingleton<DaprTranscriptionService>(client);
            services.AddGrpc();
            services.AddGrpcReflection();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseCors();
            app.UseRouting();
            app.UseAuthorization();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Traduie Api v1");
            });

            app.UseOpenTelemetryPrometheusScrapingEndpoint( context =>  context.Connection.LocalPort == 9090 );
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapControllers();
                endpoints.MapGrpcService<TranscriberService>();
                endpoints.MapGrpcReflectionService();
            });
        }
    }
}
