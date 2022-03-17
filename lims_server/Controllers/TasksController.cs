using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using LimsServer.Services;
using Microsoft.Extensions.Logging;
using System;

namespace LimsServer.Controllers
{
    [Authorize]
    [Route("api/tasks")]
    [ApiController]
    public class TasksController : ControllerBase
    {

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<TasksController> _logger;

        public TasksController(IWebHostEnvironment hostingEnvironment, ILogger<TasksController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// Gets details for all tasks.
        /// </summary>
        /// <returns>Collection of tasks</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromServices]ITaskService _service)
        {
            var tasks = await _service.GetAll();
            return new ObjectResult(tasks);
        }

        /// <summary>
        /// Gets details for a single task, specified by ID.
        /// </summary>
        /// <param name="id">task ID</param>
        /// <returns>Single task</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, [FromServices]ITaskService _service)
        {
            var task = await _service.GetById(id);
            return new ObjectResult(task);
        }

        /// <summary>
        /// Deletes a single task, specified by ID. (sets status to CANCELLED)
        /// </summary>
        /// <param name="id">task ID</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromServices]ITaskService _service)
        {
            try
            {
                var deleteResult = await _service.Delete(id);
                Dictionary<string, string> result = new Dictionary<string, string>();
                result.Add("result", $"task deleted: {deleteResult}");
                return new ObjectResult(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
