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
using LimsServer.Helpers.DB;
using PluginBase;

namespace LimsServer.Controllers
{

    public class PathCheck
    {
        public Dictionary<string, string> paths { get; set; }
    }


    [Route("api/utility")]
    [ApiController]
    public class UtilitiesController : ControllerBase
    {

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ProcessorsController> _logger;


        public UtilitiesController(IWebHostEnvironment hostingEnvironment, ILogger<ProcessorsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>All workflows</returns>
        [AllowAnonymous]
        [HttpPost("dircheck")]
        public async System.Threading.Tasks.Task<IActionResult> Post([FromBody] PathCheck pInput)
        {
            if (pInput == null)
            {
                return StatusCode(400, "Bad request, missing parameter 'path'");
            }
            Dictionary<string, bool> results = new Dictionary<string, bool>();
            foreach(KeyValuePair<string, string> pV in pInput.paths)
            {
                bool dirTest = new DirectoryInfo(pV.Value).Exists;
                results.Add(pV.Key, dirTest);
            }
            return new ObjectResult(results);
        }

        /// <summary>
        /// Purge DB
        /// </summary>
        /// <returns>All workflows</returns>
        [AllowAnonymous]
        [HttpGet("dbpurge")]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromQuery] LimsServer.Helpers.DB.DBPurge pInput)
        {
            if (pInput == null)
            {
                return StatusCode(400, "Bad request, missing parameter 'path'");
            }
            Dictionary<string, string> results = new Dictionary<string, string>();
            DBManagement dBManagement = new DBManagement(pInput);
            results = dBManagement.dbCleanup();
            return new ObjectResult(results);
        }

    }
}