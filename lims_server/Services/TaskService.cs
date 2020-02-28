using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hangfire;
using Hangfire.Server;
using LimsServer.Entities;
using LimsServer.Helpers;
using Microsoft.EntityFrameworkCore;
using PluginBase;

namespace LimsServer.Services
{
    public interface ITaskService
    {
        System.Threading.Tasks.Task<IEnumerable<Task>> GetAll();
        System.Threading.Tasks.Task<IEnumerable<Task>> GetById(string id);
        System.Threading.Tasks.Task<Task> Create(Task task);
        System.Threading.Tasks.Task<bool> Delete(string id);
    }
    public class TaskService : ITaskService
    {
        private DataContext _context;
        public TaskService(DataContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task RunTask(string id, PerformContext hfContext)
        {
            var task = await _context.Tasks.SingleAsync(t => t.id == id);

            // Step 1: If status!="SCHEDULED" cancel task
            if (!task.status.Equals("SCHEDULED"))
            {
                await this.UpdateStatus(task.id, "CANCELLED", "Task status was set to: " + task.status);
                return;
            }
            // Step 2: Change status to "STARTING"
            await this.UpdateStatus(task.id, "STARTING");

            var workflow = await _context.Workflows.SingleAsync(w => w.id == task.workflowID);
            if(workflow == null)
            {
                await this.UpdateStatus(task.id, "CANCELLED", "Error attempting to get workflow of this task. Workflow ID: " + workflow.id);
                return;
            }

            // Step 3: Check source directory for files
            List<string> files = new List<string>();
            if (Directory.Exists(workflow.inputFolder))
            {
                files = Directory.GetFiles(workflow.inputFolder).ToList();
            }
            // Step 4: If directory or files do not exist reschedule task
            if(files.Count == 0)
            {
                await this.UpdateStatus(task.id, "CANCELLED", "No files found");
                await this.CreateNewTask(workflow.id, workflow.interval);
                return;
            }

            // Step 5: If file does exist, update task inputFile
            task.inputFiles = files;
            task.status = "PROCESSING";
            await _context.SaveChangesAsync();

            ProcessorManager pm = new ProcessorManager();
            string config = "./app_files/processors";
            ProcessorDTO processor = pm.GetProcessors(config).Find(p => p.Name.ToLower() == workflow.processor.ToLower());
            if(processor == null)
            {
                await this.UpdateStatus(task.id, "CANCELLED", "Error, invalid processor name. Name: " + workflow.processor);
                return;
            }

            // Step 6: Run processor on source file
            if (!Directory.Exists(workflow.outputFolder))
            {
                Directory.CreateDirectory(workflow.outputFolder);
            }

            Dictionary<string, ResponseMessage> outputs = new Dictionary<string, ResponseMessage>();
            foreach (string file in files)
            {
                DataTableResponseMessage result = pm.ExecuteProcessor(processor.Path, processor.UniqueId, file);
                if (result.ErrorMessages.Count == 0)
                {
                    outputs.Add(file, pm.WriteTemplateOutputFile(workflow.outputFolder, result.TemplateData));
                }
                else
                {
                    await this.UpdateStatus(task.id, "CANCELLED", "Error processing data: " + result.ErrorMessages.ToString());
                    await this.CreateNewTask(workflow.id, workflow.interval);
                    return;
                }
            }
            // Step 7: Check if output file exists
            bool processed = false;
            foreach(KeyValuePair<string, ResponseMessage> rm in outputs)
            {
                string outputPath = System.IO.Path.Combine(workflow.outputFolder, rm.Value.OutputFile);
                // Step 8: If output file does exist, update task outputFile and delete input file
                if (File.Exists(outputPath))
                {
                    processed = true;
                    string inputPath = System.IO.Path.Combine(workflow.inputFolder, rm.Key);
                    File.Delete(inputPath);
                    task.outputFiles.Add(outputPath);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await this.UpdateStatus(task.id, "SCHEDULED", "Error unable to export output. Error Messages: " + rm.Value.ErrorMessages.ToString());
                }
            }

            // Step 9: Change task status to COMPLETED
            // Step 10: Create new Task and schedule
            string newStatus = "";
            if (processed) 
            {
                newStatus = "COMPLETED";
            }
            else
            {
                newStatus = "CANCELLED";               
            }
            await this.UpdateStatus(task.id, newStatus);

            await this.CreateNewTask(workflow.id, workflow.interval);
            return;
        }

        /// <summary>
        /// Helper method for updating a task status
        /// </summary>
        /// <param name="workflowID"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task UpdateStatus(string id, string status)
        {
            var task = await _context.Tasks.SingleAsync(t => t.id == id);
            if(task != null)
            {
                task.status = status;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Helper method for updating a task status and message
        /// </summary>
        /// <param name="workflowID"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task UpdateStatus(string id, string status, string message)
        {
            var task = await _context.Tasks.SingleAsync(t => t.id == id);
            if (task != null)
            {
                task.status = status;
                task.message = message;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Helper method for creating a new Task
        /// </summary>
        /// <param name="workflowID"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task CreateNewTask(string workflowID, int interval)
        {
            string newID0 = System.Guid.NewGuid().ToString();
            Task newTask0 = new Task(newID0, workflowID, interval);
            await this.Create(newTask0);
        }

        /// <summary>
        /// Creates a new Task in the database and schedules the Task with Hangfire
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<LimsServer.Entities.Task> Create(Task task)
        {
            var create = await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
            var workflow = await _context.Workflows.SingleAsync(w => w.id == task.workflowID);
            var tsk = await _context.Tasks.SingleAsync(t => t.id == task.id);
            tsk.status = "SCHEDULED";

            tsk.taskID = BackgroundJob.Schedule(() => this.RunTask(tsk.id, null), TimeSpan.FromMinutes(workflow.interval));
            //await this.RunTask(tsk.id, null);
            await _context.SaveChangesAsync();
            return tsk;
        }

        /// <summary>
        /// Deletes the specified task, by the task GUID, not the Hangfire taskID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>True if task exists and is deleted, False if the task does not exist</returns>
        public async System.Threading.Tasks.Task<bool> Delete(string id)
        {
            var task = await _context.Tasks.SingleAsync(t => t.id == id);
            if(task != null)
            {
                if(task.taskID != null)
                {
                    BackgroundJob.Delete(task.taskID);
                }
                task.status = "CANCELLED";
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets all Tasks
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<IEnumerable<Task>> GetAll()
        {
            List<Task> tasks = await _context.Tasks.ToListAsync();
            return tasks;
        }

        /// <summary>
        /// Get a specified task by workflow ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<IEnumerable<Task>> GetById(string id)
        {
            List<Task> task = await _context.Tasks.Where(t => t.workflowID == id).ToListAsync();
            return task;
        }
    }
}
