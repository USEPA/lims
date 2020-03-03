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
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowsController : ControllerBase
    {

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ProcessorsController> _logger;

        public WorkflowsController(IWebHostEnvironment hostingEnvironment, ILogger<ProcessorsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// GET: /workflows
        /// </summary>
        /// <returns>All workflows</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromServices]IWorkflowService _service)
        {
            var workflows = await _service.GetAll();
            return new ObjectResult(workflows);
        }

        /// <summary>
        /// GET: api/workflows/ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, [FromServices]IWorkflowService _service)
        {
            var workflows = await _service.GetById(id);
            return new ObjectResult(workflows);
        }

        /// <summary>
        /// POST: api/workflows
        /// Creates a new workflow
        /// </summary>
        /// <param name="value">Serialized workflow</param>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Workflow value, [FromServices]IWorkflowService _service)
        {
            if (value == null)
            {
                // Returns 400
                return BadRequest();
            }
            else
            {
                Workflow wf = value;
                var workflow = await _service.Create(wf);
                if(workflow.message == null)
                {
                    // Returns 201 (successfully created new object)
                    return CreatedAtAction(nameof(Get), new { id = workflow.id }, workflow);
                }
                else
                {
                    // Returns 400 (Failed to create new object)
                    return BadRequest(workflow.message);
                }
            }          
        }

        /// <summary>
        /// PUT: api/workflows/ID
        /// </summary>
        /// <param name="id">ID of the workflow to update</param>
        /// <param name="value">Updated workflow configuration</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Workflow value, [FromServices]IWorkflowService _service)
        {
            try
            {
                _service.Update(id, value);
                return NoContent();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// DELETE: api/workflows/ID
        /// </summary>
        /// <param name="id"></param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromServices]IWorkflowService _service)
        {
            try
            {
                _service.Delete(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
