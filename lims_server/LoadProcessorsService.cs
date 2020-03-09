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
        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string processorPath = Path.Combine(projectRootPath, "app_files", "processors");
            PluginBase.ProcessorManager pm = new ProcessorManager();
            var procsFromDisk = pm.GetProcessors(processorPath);
            List<string> procsDiskNames = new List<string>();
            foreach (ProcessorDTO proc in procsFromDisk)
                procsDiskNames.Add(proc.Name.ToLower());

            // Create a new scope to retrieve scoped services

            List<string> procsInDb = new List<string>();
            using (var scope = _serviceProvider.CreateScope())
            {
                // Get the DbContext instance
                var procService = scope.ServiceProvider.GetRequiredService<IProcessorService>();
                var dbProcs = await procService.GetAll();
                //var dbProcs = procs.Result;
                List<Processor> lstProcs = new List<Processor>();
                foreach (Processor proc in dbProcs)
                {
                    proc.enabled = false;
                    lstProcs.Add(proc);
                    procsInDb.Add(proc.name.ToLower());
                }
                
                //Set all the processors in the db to enabled = false
                procService.Update(lstProcs.ToArray());

                var procsIntersect = procsDiskNames.Intersect(procsInDb);
                lstProcs = new List<Processor>();
                foreach (string proc in procsIntersect)
                {
                    var processor = await procService.GetById(proc);
                    //Processor processor = result.Result;
                    processor.enabled = true;
                    lstProcs.Add(processor);
                }

                //Set all the processors in the db that are also on disk to enabled = true
                procService.Update(lstProcs.ToArray());

                var newProcs = procsDiskNames.Except(procsInDb);
                List<ProcessorDTO> lstProcDTO = new List<ProcessorDTO>();
                foreach(string proc in newProcs)                
                    lstProcDTO.Add(procsFromDisk.Find(x => x.Name.ToLower() == proc.ToLower()));

                foreach (ProcessorDTO proc in lstProcDTO)
                {
                    Processor processor = new Processor();
                    processor.enabled = true;
                    processor.description = proc.Description;
                    processor.file_type = proc.InstrumentFileType;
                    processor.id = proc.UniqueId.ToLower();
                    processor.name = proc.Name.ToLower();

                    await procService.Create(processor);
                }
            }

            //return System.Threading.Tasks.Task.CompletedTask;
            return;
            
        }
    }
}
