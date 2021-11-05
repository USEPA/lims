using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using LimsServer.Entities;
using System.Threading.Tasks;
using LimsServer.Services;

namespace LimsServer.Controllers
{
    [Authorize]
    [Route("api/logs")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<LogController> _logger;

        public LogController(IWebHostEnvironment hostingEnvironment, ILogger<LogController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// Get all logs
        /// </summary>
        /// <returns>List of logs</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromServices] ILogService _service)
        {
            return Ok(await _service.GetAll());
        }
    }
}