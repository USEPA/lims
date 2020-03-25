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
    [Route("api/workflows")]
    [ApiController]
    public class WorkflowsController : ControllerBase
    {

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<WorkflowsController> _logger;

        public WorkflowsController(IWebHostEnvironment hostingEnvironment, ILogger<WorkflowsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// Get all workflows
        /// </summary>
        /// <returns>List of workflows</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Get([FromServices]IWorkflowService _service)
        {
            var workflows = await _service.GetAll();
            return new ObjectResult(workflows);
        }

        /// <summary>
        /// Get the details of a single workflow
        /// </summary>
        /// <param name="id">workflow ID</param>
        /// <returns>Details of workflow</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, [FromServices]IWorkflowService _service)
        {
            var workflows = await _service.GetById(id);
            return new ObjectResult(workflows);
        }

        /// <summary>
        /// Creates a new workflow
        /// </summary>
        /// <param name="value">json object containing the variables for a new workflow</param>
        /// <returns>201 on success with assigned id, 400 on fail</returns>
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
        /// Update workflow with new details
        /// </summary>
        /// <param name="value">json object containing the variables for a workflow</param>
        /// <returns>No content on success, 400 on fail.</returns>
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Workflow value, [FromServices]IWorkflowService _service)
        {
            try
            {
                await _service.Update(value);
                return NoContent();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Delete the specified user (marks user as inactive)
        /// </summary>
        /// <param name="id">User to delete</param>
        /// <returns>No content on success, 400 on fail.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromServices]IWorkflowService _service)
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
