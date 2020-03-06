using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        Task<Workflow> Create(Workflow workflow);
        void Update(string id, Workflow workflow);
        void Delete(string id);
    }
    public class WorkflowService : IWorkflowService
    {
        private DataContext _context;
        public WorkflowService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create a new Workflow and add to db context.
        /// </summary>
        /// <param name="workflow">New workflow to add</param>
        /// <returns>The added workflow, as seen from the db context, or an empty workflow with an error message.</returns>
        public async Task<Workflow> Create(Workflow workflow)
        {
            string workflowID = System.Guid.NewGuid().ToString();
            workflow.id = workflowID;
            try
            {
                var result = await _context.Workflows.AddAsync(workflow);
                await _context.SaveChangesAsync();
                string id = System.Guid.NewGuid().ToString();
                Log.Information("New LIMS Workflow, ID: {0}, Initial Task ID: {1}", workflowID, id);

                LimsServer.Entities.Task tsk = new Entities.Task(id, workflow.id, workflow.interval);

                TaskService ts = new TaskService(this._context);
                var task = await ts.Create(tsk);

                return result.Entity;
            }
            catch(Exception ex)
            {
                var result = new Workflow();
                result.message = ex.Message;
                Log.Error(ex, "Error attempting to create new workflow and workflow task");
                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public async void Delete(string id)
        {
            //TODO: All Tasks of the specified workflow need to be cancelled.

            var workflow = await _context.Workflows.SingleAsync(w => w.id == id);
            Log.Information("Deleting LIMS Workflow, ID: {0}", id);
            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync();
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
            var workflows = await _context.Workflows.SingleAsync(w => w.id == id);
            return workflows as Workflow;
        }

        /// <summary>
        /// Updates the workflow provided by id
        /// </summary>
        /// <param name="workflow"></param>
        public async void Update(string id, Workflow workflow)
        {
            //TODO: Workflow Update will require that all Tasks currently running for the specified workflow be cancelled and a new Task be created.

            var oldW = await _context.Workflows.SingleAsync(w => w.id == id);
            oldW = workflow;
            await _context.SaveChangesAsync();
        }
    }
}
