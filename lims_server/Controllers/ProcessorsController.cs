using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
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
        /// Each folder will contain a dll that has a class that 
        /// implmenets the IProcessor interface
        /// The dll will have the same name as the folder.
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// GET: /workflows
        /// </summary>
        /// <returns>All workflows</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromServices]IProcessorService _service)
        {
            //var processors = await _service.GetAll();
            var processors = _service.GetAll();
            return new ObjectResult(processors);
        }

    }
}
