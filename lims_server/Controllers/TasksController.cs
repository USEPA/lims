using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using Hangfire.Storage.SQLite;
using Hangfire.Storage.Monitoring;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using LimsServer.Services;

namespace LimsServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {

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
        /// POST: api/Tasks
        /// </summary>
        /// <param name="value">Serialized string of a new task</param>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        /// <summary>
        /// PUT: api/Tasks/ID
        /// </summary>
        /// <param name="id">task ID</param>
        /// <param name="value">Serialized string with the updated task configuration</param>
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE: api/ApiWithActions/5
        /// <summary>
        /// DELETE: api/Tasks/ID
        /// </summary>
        /// <param name="id">ID of the task to be deleted.</param>
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
