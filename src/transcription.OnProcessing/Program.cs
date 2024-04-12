using Dapr.Client;
using Dapr.Extensions.Configuration;

using transcription.models;

namespace transcription.processing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var client = new DaprClientBuilder().Build();

            return Host.CreateDefaultBuilder(args)
               .ConfigureServices((services) =>
                {
                    services.AddSingleton<DaprClient>(client);
                })
                .ConfigureAppConfiguration((configBuilder) =>
                {
                    configBuilder.AddDaprSecretStore(Components.SecureStore, client);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}
