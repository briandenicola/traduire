using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace traduire.webapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => 
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(9091, o => o.Protocols = HttpProtocols.Http1);
                        options.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http1AndHttp2);
                    });

                    webBuilder.UseStartup<Startup>();
                });
        
    }
}
