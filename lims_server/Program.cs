using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Linq;

namespace LimsServer
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
                .Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("/dashboard/")))             // The hangfire dashboard queries the api every 1sec, excluding those requests from log
                .Filter.ByExcluding(c => c.Properties.Any(w => w.Value.ToString().Contains("Worker")))
                .Filter.ByExcluding(c => c.Properties.Any(s => s.Value.ToString().Contains("Server")))
                .Filter.ByExcluding(c => c.Properties.Any(e => e.Value.ToString().Contains("Executed DbCommand")))
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting LIMS server");         
                CreateHostBuilder(args).Build().Run();
            }
            catch(Exception ex)
            {
                Log.Fatal(ex, "LIMS server failed to startup.");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    //services.AddHostedService<LoadProcessors>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost");
                    //webBuilder.UseIIS();
                })
                //.ConfigureLogging(logging =>
                //{
                //    logging.ClearProviders();
                //    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                //})
                .UseSerilog();


        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseStartup<Startup>()
        //        .ConfigureLogging(logging =>
        //        {
        //            logging.ClearProviders();
        //            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        //        })
        //        .UseSerilog();
    }
}
