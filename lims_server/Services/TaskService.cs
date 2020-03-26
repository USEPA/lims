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
using Serilog;

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
            Log.Information("Executing Task. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}", task.workflowID, task.id, task.taskID);

            // Step 1: If status!="SCHEDULED" cancel task

            if (!task.status.Equals("SCHEDULED"))
            {
                await this.UpdateStatus(task.id, "CANCELLED", "Task status was set to: " + task.status);
                Log.Information("Task Cancelled. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}, Message: {3}", task.workflowID, task.id, task.taskID, "Task status is not marked as schedulled.");
                return;
            }
            // Step 2: Change status to "STARTING"
            await this.UpdateStatus(task.id, "STARTING", "");

            var workflow = await _context.Workflows.SingleAsync(w => w.id == task.workflowID);
            if(workflow == null)
            {
                await this.UpdateStatus(task.id, "CANCELLED", "Error attempting to get workflow of this task. Workflow ID: " + workflow.id);
                Log.Information("Task Cancelled. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}, Message: {3}", task.workflowID, task.id, task.taskID, "Unable to find Workflow for the Task.");
                return;
            }

            // Step 3: Check source directory for files
            List<string> files = new List<string>();
            string dirFileMessage = "";
            if (new DirectoryInfo(@workflow.inputFolder).Exists)
            {
                files = Directory.GetFiles(@workflow.inputFolder).ToList();
            }
            else
            {
                dirFileMessage = String.Format("Input directory {0} not found", workflow.inputFolder);
                Log.Information(dirFileMessage);
       
            }
            // Step 4: If directory or files do not exist reschedule task
            if (files.Count == 0)
            {
                dirFileMessage = (dirFileMessage.Length > 0) ? dirFileMessage : String.Format("No files found in directory: {0}", workflow.inputFolder);
                await this.UpdateStatus(task.id, "SCHEDULED", dirFileMessage);
                var newSchedule = new Hangfire.States.ScheduledState(TimeSpan.FromMinutes(workflow.interval));
                task.start = DateTime.Now.AddMinutes(workflow.interval);
                await _context.SaveChangesAsync();

                BackgroundJobClient backgroundClient = new BackgroundJobClient();
                backgroundClient.ChangeState(task.taskID, newSchedule);
                Log.Information("Task Rescheduled. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}, Input Directory: {3}, Message: {4}", task.workflowID, task.id, task.taskID, workflow.inputFolder, "No files found in input directory.");
                return;
            }

            // Step 5: If file does exist, update task inputFile
            task.inputFile = files.First();
            task.status = "PROCESSING";
            await _context.SaveChangesAsync();

            ProcessorManager pm = new ProcessorManager();
            string config = "./app_files/processors";
            ProcessorDTO processor = pm.GetProcessors(config).Find(p => p.Name.ToLower() == workflow.processor.ToLower());
            if(processor == null)
            {
                await this.UpdateStatus(task.id, "CANCELLED", "Error, invalid processor name. Name: " + workflow.processor);
                Log.Information("Task Cancelled. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}, Message: {3}", task.workflowID, task.id, task.taskID, "Unable to find Processor for the Task.");
                return;
            }

            // Step 6: Run processor on source file
            if (!Directory.Exists(@workflow.outputFolder))
            {
                Directory.CreateDirectory(@workflow.outputFolder);
            }

            Dictionary<string, ResponseMessage> outputs = new Dictionary<string, ResponseMessage>();
            string file = task.inputFile;
            DataTableResponseMessage result = pm.ExecuteProcessor(processor.Path, processor.UniqueId, file);
            if (result.ErrorMessage == null && result.TemplateData != null)
            {
                var output = pm.WriteTemplateOutputFile(workflow.outputFolder, result.TemplateData);
                outputs.Add(file, output);
            }
            else
            {
                string errorMessage = "";
                string logMessage = "";
                if (result.TemplateData == null)
                {
                    errorMessage = "Processor results template data is null. ";
                }
                if (result.ErrorMessage != null)
                {
                    errorMessage = errorMessage + result.ErrorMessage.ToString();
                    logMessage = errorMessage;
                }
                if (result.LogMessage != null)
                {
                    logMessage = result.LogMessage.ToString();
                }
                await this.UpdateStatus(task.id, "CANCELLED", "Error processing data: " + errorMessage);
                Log.Information("Task Cancelled. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}, Message: {3}", task.workflowID, task.id, task.taskID, logMessage);
                return;
            }

            // Step 7: Check if output file exists
            bool processed = false;
            for(int i = 0; i < outputs.Count; i++)
            {
                var output = outputs.ElementAt(i);
                string outputPath = output.Value.OutputFile;
                // Step 8: If output file does exist, update task outputFile and delete input file
                if (File.Exists(outputPath))
                {
                    processed = true;
                    string inputPath = System.IO.Path.Combine(workflow.inputFolder, output.Key);

                    DataBackup dbBackup = new DataBackup();
                    dbBackup.DumpData(id, inputPath, outputPath);

                    File.Delete(inputPath);
                    task.outputFile = outputPath;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await this.UpdateStatus(task.id, "SCHEDULED", "Error unable to export output. Error Messages: " + output.Value.ErrorMessage);
                }
            }

            // Step 9: Change task status to COMPLETED
            // Step 10: Create new Task and schedule
            string newStatus = "";
            if (processed) 
            {
                newStatus = "COMPLETED";
                Log.Information("Task Completed. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}", task.workflowID, task.id, task.taskID);
                if (files.Count > 1)
                {
                    await this.CreateNewTask(workflow.id, 0);
                }
                else
                {
                    await this.CreateNewTask(workflow.id, workflow.interval);
                }
            }
            else
            {
                newStatus = "CANCELLED";
                Log.Information("Task Cancelled. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}, Message: {3}", task.workflowID, task.id, task.taskID, "Failed to process input file");
            }
            await this.UpdateStatus(task.id, newStatus);          
        }

        /// <summary>
        /// Helper method for updating a task status
        /// </summary>
        /// <param name="workflowID"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task UpdateStatus(string id, string status)
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
        protected async System.Threading.Tasks.Task UpdateStatus(string id, string status, string message)
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
        protected async System.Threading.Tasks.Task CreateNewTask(string workflowID, int interval)
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

            TimeSpan scheduledStart = task.start - DateTime.Now;

            tsk.taskID = BackgroundJob.Schedule(() => this.RunTask(tsk.id, null), scheduledStart);
            //await this.RunTask(tsk.id, null);
            await _context.SaveChangesAsync();
            Log.Information("New Task Created. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}", task.workflowID, task.id, task.taskID);
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
                Log.Information("Deleted Task. WorkflowID: {0}, ID: {1}, Hangfire ID: {2}", task.workflowID, task.id, task.taskID);
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
