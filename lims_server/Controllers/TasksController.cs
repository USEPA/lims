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
    [Route("api/[controller]")]
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
        /// GET: api/Tasks
        /// </summary>
        /// <returns>all Tasks</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromServices]ITaskService _service)
        {
            var tasks = await _service.GetAll();
            return new ObjectResult(tasks);
        }

        /// <summary>
        /// GET: api/Tasks/ID
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Task of the specified ID</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, [FromServices]ITaskService _service)
        {
            var task = await _service.GetById(id);
            return new ObjectResult(task);
        }

        /// <summary>
        /// DELETE: api/Tasks/ID
        /// </summary>
        /// <param name="id">ID of the task to be deleted.</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromServices]ITaskService _service)
        {
            try
            {
                await _service.Delete(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
