using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SQLite;
using LimsServer.Services;
using LimsServer.Entities;
using PluginBase;

namespace LimsServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessorsController : ControllerBase
    {

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ProcessorsController> _logger;


        public ProcessorsController(IWebHostEnvironment hostingEnvironment, ILogger<ProcessorsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }


        /// <summary>
        /// Loop over the folders in the processors folder.
        /// Each folder will contain a dll that has a class that implmenets the IProcessor interface.
        /// The dll will have the same name as the folder.
        /// </summary>
        /// <returns>All workflows</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromServices] IProcessorService _service)
        {
            var processors = await _service.GetAll();
            return new ObjectResult(processors);
        }

        /// <summary>
        /// Enables/disables a processor in the database 
        /// </summary>
        /// <param name="name">The name of the processor</param>
        /// <param name="enabled">True to enable, false to disable</param>
        /// <returns>Updated processor, or error</returns>
        [HttpPut]
        public async System.Threading.Tasks.Task<IActionResult> ToggleProcessor([FromServices] IProcessorService _procService, [FromServices] IWorkflowService _workflowService, [FromQuery] string name, [FromQuery] bool enabled)
        {
            // Get processor and update enabled
            var processor = await _procService.GetByName(name);
            if (processor == null)
            {
                return new ObjectResult(new { error = "Processor not found" });
            }
            processor.enabled = enabled;
            await _procService.Update(processor.id, processor);

            // If processor disabled stop all workflows that are using this processor
            if (!enabled)
            {
                var workflows = await _workflowService.GetAll();
                workflows = workflows.Where(w => w.processor.ToLower() == name.ToLower()).ToList();
                foreach (var workflow in workflows)
                {
                    workflow.active = false;
                    await _workflowService.Update(workflow, false);
                }
            }
            return new ObjectResult(processor);
        }
    }
}
