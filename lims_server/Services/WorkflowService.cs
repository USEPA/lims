using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using LimsServer.Entities;
using LimsServer.Helpers;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LimsServer.Services
{
    public interface IWorkflowService
    {
        Task<IEnumerable<Workflow>> GetAll();
        Task<Workflow> GetById(string id);
        Task<Workflow> Create(Workflow workflow, bool bypass = false);
        Task<bool> Update(Workflow workflow, bool bypass = false);
        Task<bool> Delete(string id);
    }
    public class WorkflowService : IWorkflowService
    {
        private DataContext _context;
        private readonly ILogService _logService;
        public WorkflowService(DataContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Create a new Workflow and add to db context.
        /// </summary>
        /// <param name="workflow">New workflow to add</param>
        /// <returns>The added workflow, as seen from the db context, or an empty workflow with an error message.</returns>
        public async Task<Workflow> Create(Workflow workflow, bool bypass = false)
        {
            // Get processor 
            var processor = await _context.Processors
                .FirstOrDefaultAsync(p => p.name.ToLower() == workflow.processor.ToLower());
            // Create workflow in db
            string workflowID = System.Guid.NewGuid().ToString();
            workflow.id = workflowID;
            workflow.active = processor.enabled;
            workflow.creationDate = DateTime.Now;
            try
            {
                var result = await _context.Workflows.AddAsync(workflow);
                await _context.SaveChangesAsync();
                string id = System.Guid.NewGuid().ToString();
                Serilog.Log.Information("New LIMS Workflow, ID: {0}, Initial Task ID: {1}", workflowID, id);
                // If processor is enabled, create initial task
                if (!bypass && processor.enabled)
                {
                    LimsServer.Entities.Task tsk = new Entities.Task(id, workflow.id, workflow.interval);

                    TaskService ts = new TaskService(this._context, this._logService);
                    var task = await ts.Create(tsk);
                    await _context.SaveChangesAsync();
                }
                return result.Entity;
            }
            catch (Exception ex)
            {
                var result = new Workflow();
                result.message = ex.Message;
                Serilog.Log.Error(ex, "Error attempting to create new workflow and workflow task");
                return result;
            }
        }

        /// <summary>
        /// Deletes a Workflow and cancels all currently scheduled tasks for that workflow
        /// </summary>
        /// <param name="id"></param>
        public async System.Threading.Tasks.Task<bool> Delete(string id)
        {
            var workflow = await _context.Workflows.Where(w => w.id == id).FirstOrDefaultAsync();
            if (workflow != null)
            {
                try
                {
                    _context.Workflows.Remove(workflow);
                    var tasks = await _context.Tasks.Where(t => t.workflowID == id).ToListAsync();
                    foreach (LimsServer.Entities.Task t in tasks)
                    {
                        try
                        {
                            _context.Tasks.Remove(t);
                            BackgroundJobClient backgroundClient = new BackgroundJobClient();
                            backgroundClient.ChangeState(t.taskID, new Hangfire.States.DeletedState());
                            Serilog.Log.Information("Task deleted. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}", t.workflowID, t.id, t.taskID);
                        }
                        catch (Exception)
                        {
                            Serilog.Log.Warning("Error unable to change Hangfire background job to deleted state. Task ID: {0}", t.taskID);
                        }
                    }
                    Serilog.Log.Information("Deleted Workflow: {0}, and associated Tasks.", id);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex.Message, "Error attempting to delete workflow");
                    return false;
                }
            }
            else
            {
                Serilog.Log.Information("Unable to delete Workflow: {0}, ID not found.", id);
                return false;
            }
        }

        /// <summary>
        /// Query all workflows from current db context
        /// </summary>
        /// <returns>List of workflows</returns>
        public async System.Threading.Tasks.Task<IEnumerable<Workflow>> GetAll()
        {
            var workflows = await _context.Workflows.ToListAsync();
            return workflows as List<Workflow>;
        }

        /// <summary>
        /// Query workflows for specified id.
        /// </summary>
        /// <param name="id">Workflow ID</param>
        /// <returns>the workflow with the specified ID</returns>
        public async Task<Workflow> GetById(string id)
        {
            try
            {
                var workflows = await _context.Workflows.SingleAsync(w => w.id == id);
                return workflows as Workflow;
            }
            catch (Exception ex)
            {
                Serilog.Log.Information("No workflow found with id: {0}", id);
                return new Workflow();
            }
        }

        /// <summary>
        /// Updates the workflow provided by id
        /// </summary>
        /// <param name="workflow"></param>
        public async System.Threading.Tasks.Task<bool> Update(Workflow _workflow, bool bypass = false)
        {
            string id = _workflow.id;
            var workflow = await _context.Workflows.Where(w => w.id == id).FirstOrDefaultAsync();
            if (workflow != null)
            {
                workflow.Update(_workflow);
                await _context.SaveChangesAsync();
                Serilog.Log.Information("Updating Workflow: {0}, and rescheduling existing Tasks.", id);
                var tasks = await _context.Tasks.Where(t => t.workflowID == id).ToListAsync();
                bool taskRunning = false;
                foreach (LimsServer.Entities.Task t in tasks)
                {
                    if (t.status == "SCHEDULED" && workflow.active)
                    {
                        t.start = DateTime.Now.AddMinutes(workflow.interval);
                        await _context.SaveChangesAsync();
                        Serilog.Log.Information("Task Rescheduled. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}, Input Directory: {3}, Message: {4}", t.workflowID, t.id, t.taskID, workflow.inputFolder, "Workflow updated, task rescheduled to new workflow configuration.");
                        taskRunning = true;

                        try
                        {
                            var newSchedule = new Hangfire.States.ScheduledState(TimeSpan.FromMinutes(workflow.interval));
                            BackgroundJobClient backgroundClient = new BackgroundJobClient();
                            backgroundClient.ChangeState(t.taskID, newSchedule);
                        }
                        catch (Exception)
                        {
                            Serilog.Log.Warning("Error rescheduling Hangfire background job. Job ID: {0}", t.taskID);
                        }
                    }
                    else if (t.status != "COMPLETED")
                    {
                        t.status = "CANCELLED";
                        t.message = "Corresponding workflow updated.";
                        var newState = new Hangfire.States.DeletedState();
                        // Log task cancellation to database
                        _logService.Information($"Task Cancelled. WorkflowID: {t.workflowID}, ID: {t.id}, Hangfire ID: {t.taskID}", task: t);

                        try
                        {
                            BackgroundJobClient backgroundClient = new BackgroundJobClient();
                            backgroundClient.ChangeState(t.taskID, newState);
                        }
                        catch (Exception)
                        {
                            Serilog.Log.Warning("Error cancelling Hanfire background job. Job ID: {0}", t.taskID);
                        }
                    }
                }
                if (!taskRunning && workflow.active)
                {
                    string newId = System.Guid.NewGuid().ToString();
                    LimsServer.Entities.Task tsk = new Entities.Task(newId, workflow.id, workflow.interval);
                    TaskService ts = new TaskService(this._context, this._logService);
                    var task = await ts.Create(tsk);
                    await _context.SaveChangesAsync();
                    Serilog.Log.Information("Created new Task for updated Workflow ID: {0}, Updated Task ID: {1}, Hangfire ID: {2}", newId, tsk.id, tsk.taskID);
                }
                return true;
            }
            else
            {
                Serilog.Log.Information("Unable to cancel Workflow: {0}, ID not found.", id);
                return false;
            }
        }
    }
}
