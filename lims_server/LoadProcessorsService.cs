using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using LimsServer.Services;
using LimsServer.Entities;
using PluginBase;
using Microsoft.Extensions.Logging;


namespace LimsServer
{
    public class LoadProcessors : BackgroundService
    {
        //readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public LoadProcessors(IServiceProvider serviceProvider, IWebHostEnvironment hostingEnvironment)
        {
            //_logger = logger;
            _serviceProvider = serviceProvider;
            _hostingEnvironment = hostingEnvironment;
        }
        protected override async void ExecuteAsync(CancellationToken stoppingToken)
        {
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string processorPath = Path.Combine(projectRootPath, "app_files", "processors");
            PluginBase.ProcessorManager pm = new ProcessorManager();
            var procsFromDisk = pm.GetProcessors(processorPath);


            // Create a new scope to retrieve scoped services

            List<Processor> lstProcs = new List<Processor>();
            using (var scope = _serviceProvider.CreateScope())
            {
                // Get the DbContext instance
                var procService = scope.ServiceProvider.GetRequiredService<IProcessorService>();
                var procs = await procService.GetAll();
                
            }
            return;
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(10000, stoppingToken);
            //}
        }
    }
}
