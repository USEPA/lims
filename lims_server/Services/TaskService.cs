using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        private readonly ILogService _logService;
        public TaskService(DataContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async System.Threading.Tasks.Task RunTask(string id)
        {
            var task = await _context.Tasks.SingleAsync(t => t.id == id);
            _logService.Information($"Executing Task. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}", task: task);

            // Step 1: If status!="SCHEDULED" cancel task

            if (!task.status.Equals("SCHEDULED"))
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Current Status: {task.status}, Message: Task status is not marked as scheduled.", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Task status was set to: " + task.status);
                return;
            }
            // Step 2: Change status to "STARTING"
            await this.UpdateStatus(task.id, "STARTING");

            var workflow = await _context.Workflows.Where(w => w.id == task.workflowID).FirstOrDefaultAsync();
            if (workflow == null)
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Unable to find Workflow for the Task.", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Error attempting to get workflow of this task. Workflow ID: " + task.workflowID);
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
                _logService.Information(dirFileMessage, task: task);
            }
            // Step 4: If directory or files do not exist reschedule task
            if (files.Count == 0)
            {
                dirFileMessage = (dirFileMessage.Length > 0) ? dirFileMessage : String.Format("No files found in directory: {0}", workflow.inputFolder);
                await this.UpdateStatus(task.id, "SCHEDULED", dirFileMessage);
                var newSchedule = new Hangfire.States.ScheduledState(TimeSpan.FromMinutes(workflow.interval));
                task.start = DateTime.Now.AddMinutes(workflow.interval);
                await _context.SaveChangesAsync();
                try
                {
                    BackgroundJobClient backgroundClient = new BackgroundJobClient();
                    backgroundClient.ChangeState(task.taskID, newSchedule);
                    _logService.Information($"Task Rescheduled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Input Directory: {workflow.inputFolder}, Message: No files found in input directory.", task: task);
                }
                catch (Exception)
                {
                    _logService.Warning($"Error attempting to reschedule Hangfire job. Workflow ID: {task.workflowID}, task ID: {task.id}", task: task);
                }
                return;
            }

            // Step 5: If file does exist, update task inputFile then compare against previous Tasks.
            task.inputFile = files.First();
            task.status = "PROCESSING";
            await _context.SaveChangesAsync();
            bool alreadyCompleted = await this.InputFileCheck(task.inputFile, task.workflowID);
            if (alreadyCompleted)
            {
                _logService.Information($"Hash input file match for WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Input File: {task.inputFile}, Message: Rerunning task after removing file.", task: task);
                try
                {
                    File.Delete(task.inputFile);
                    _logService.Information($"Hash input file match successfully deleted. WorkflowID: {task.workflowID}, ID: {task.id}, Input File: {task.inputFile}", task: task);
                }
                catch (FileNotFoundException ex)
                {
                    _logService.Warning($"Error unable to delete input file after hash file match with previous input file. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
                }

                string statusMessage = String.Format("Input file: {0} matches previously processed input file", task.inputFile);
                await this.UpdateStatus(task.id, "SCHEDULED", statusMessage);
                await this.RunTask(task.id);
                return;
            }


            ProcessorManager pm = new ProcessorManager();
            string config = "./app_files/processors";
            ProcessorDTO processor = pm.GetProcessors(config).Find(p => p.Name.ToLower() == workflow.processor.ToLower());
            if (processor == null)
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Unable to find Processor for the Task., Processor: {workflow.processor}", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Error, invalid processor name. Name: " + workflow.processor);
                return;
            }

            // If processor is disabled don't run task
            var proc = await _context.Processors.Where(p => p.name.ToLower() == workflow.processor.ToLower()).FirstOrDefaultAsync();
            if (!proc.enabled)
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Processor is not enabled.", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Error, processor is not enabled. Name: " + workflow.processor);
                return;
            }

            try
            {
                // Step 6: Run processor on source file
                if (!new DirectoryInfo(@workflow.outputFolder).Exists)
                {
                    Directory.CreateDirectory(@workflow.outputFolder);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logService.Warning($"Task unable to create output directory, unauthorized access exception. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Output Directory: {workflow.outputFolder}", task: task);
            }

            Dictionary<string, ResponseMessage> outputs = new Dictionary<string, ResponseMessage>();
            string file = task.inputFile;
            DataTableResponseMessage result = pm.ExecuteProcessor(processor.Path, processor.Name, file);
            GC.Collect();
            GC.WaitForPendingFinalizers();

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
                    errorMessage = errorMessage + result.ErrorMessage;
                    logMessage = errorMessage;
                }
                if (result.LogMessage != null)
                {
                    logMessage = result.LogMessage;
                }
                await this.UpdateStatus(task.id, "CANCELLED", "Error processing data: " + errorMessage);
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: {logMessage}", task: task);
                return;
            }

            // Step 7: Check if output file exists
            bool processed = false;
            for (int i = 0; i < outputs.Count; i++)
            {
                var output = outputs.ElementAt(i);
                string outputPath = output.Value.OutputFile;
                // Step 8: If output file does exist, update task outputFile and delete input file
                if (File.Exists(outputPath))
                {
                    processed = true;
                    string fileName = System.IO.Path.GetFileName(output.Key);
                    string inputPath = System.IO.Path.Combine(workflow.inputFolder, fileName);

                    DataBackup dbBackup = new DataBackup();
                    dbBackup.DumpData(id, inputPath, outputPath);
                    try
                    {
                        File.Delete(inputPath);
                    }
                    catch (Exception ex)
                    {
                        _logService.Warning($"Error unable to delete input file after successful processing. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
                    }
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
                _logService.Information($"Task Completed. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}", task: task);
                try
                {
                    if (files.Count > 1)
                    {
                        await this.CreateNewTask(workflow.id, 0);
                    }
                    else
                    {
                        await this.CreateNewTask(workflow.id, workflow.interval);
                    }
                }
                catch (Exception)
                {
                    _logService.Warning($"Error creating new Hangfire job after successful job. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
                }
            }
            else
            {
                newStatus = "CANCELLED";
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Failed to process input file.", task: task);
            }
            await this.UpdateStatus(task.id, newStatus);
        }

        /// <summary>
        /// Get all task IDs for the specified workflow, get the input bytes and run a MD5 hash, compare to the inputFile
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="workflowID"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<bool> InputFileCheck(string inputFilePath, string workflowID)
        {
            var tasks = await _context.Tasks.Where(t => t.workflowID == workflowID && t.status == "COMPLETED").ToListAsync();
            bool match = false;

            if (tasks.Count >= 1)
            {
                byte[] inputFile = File.ReadAllBytes(inputFilePath);
                byte[] inputData = MD5.Create().ComputeHash(inputFile);
                StringBuilder ib = new StringBuilder();
                for (int i = 0; i < inputData.Length; i++)
                {
                    ib.Append(inputData[i].ToString("x2"));
                }
                string inputHash = ib.ToString();

                _logService.Debug($"Input file compare. WorkflowID: {workflowID}, InputFile: {inputFilePath}, InputFile Hash: {inputHash}", workflowID: workflowID);
                DataBackup db = new DataBackup();
                foreach (Task t in tasks)
                {
                    Dictionary<string, byte[]> previousTask = db.GetData(t.id);
                    byte[] previousData = MD5.Create().ComputeHash(previousTask["input"]);
                    StringBuilder jb = new StringBuilder();
                    for (int j = 0; j < previousData.Length; j++)
                    {
                        jb.Append(previousData[j].ToString("x2"));
                    }
                    string previousHash = jb.ToString();
                    _logService.Debug($"Input file compare. WorkflowID: {workflowID}, Previous Input Hash: {previousHash}", task: t);
                    if (StringComparer.OrdinalIgnoreCase.Compare(inputHash, previousHash) == 0)
                    {
                        match = true;
                    }
                }
            }
            return match;
        }

        /// <summary>
        /// Helper method for updating a task status
        /// </summary>
        /// <param name="workflowID"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task UpdateStatus(string id, string status)
        {
            var task = await _context.Tasks.Where(t => t.id == id).FirstOrDefaultAsync();
            if (task != null)
            {
                task.status = status;
                await _context.SaveChangesAsync();
                _logService.Information($"Task Status Updated. WorkflowID: {task.workflowID}, ID: {task.id}, Status: {status}", task: task);
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
            var task = await _context.Tasks.Where(t => t.id == id).FirstOrDefaultAsync();
            if (task != null)
            {
                task.status = status;
                task.message = message;
                await _context.SaveChangesAsync();
                _logService.Information($"Task Status Updated. WorkflowID: {task.workflowID}, ID: {task.id}, Status: {status}, Message: {message}", task: task);
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
            // Check that processor is enabled before creating new task
            var workflow = await _context.Workflows.Where(w => w.id == workflowID).FirstOrDefaultAsync();
            var processor = await _context.Processors
                .Where(p => p.name.ToLower() == workflow.processor.ToLower()).FirstOrDefaultAsync();
            if (processor.enabled)
            {
                string newID0 = System.Guid.NewGuid().ToString();
                Task newTask0 = new Task(newID0, workflowID, interval);
                try
                {
                    await this.Create(newTask0);
                }
                catch (Exception)
                {
                    _logService.Warning($"Error attempting to create new Hangfire task. Workflow ID: {workflowID}", workflowID: workflowID);
                }
            }
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
            var workflow = await _context.Workflows.Where(w => w.id == task.workflowID).FirstOrDefaultAsync();
            var tsk = await _context.Tasks.Where(t => t.id == task.id).FirstOrDefaultAsync();
            tsk.status = "SCHEDULED";

            TimeSpan scheduledStart = task.start - DateTime.Now;
            try
            {
                tsk.taskID = BackgroundJob.Schedule(() => this.RunTask(tsk.id), scheduledStart);
                _logService.Information($"New Task Created. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}", task: task);
            }
            catch (Exception)
            {
                tsk.message = "Task not scheduled, unable to connect to Hangfire server.";
                _logService.Warning($"Error unable to schedule new Hangfire job, workflow ID: {task.workflowID}, task ID: {task.id}", task: task);
            }
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
            var task = await _context.Tasks.Where(t => t.id == id).FirstOrDefaultAsync();
            if (task != null)
            {
                try
                {
                    if (task.taskID != null)
                    {
                        BackgroundJob.Delete(task.taskID);
                    }
                }
                catch (InvalidOperationException)
                {
                    _logService.Information($"No Hangfire task found for ID: {task.taskID}", task: task);
                }
                task.status = "CANCELLED";
                await _context.SaveChangesAsync();
                _logService.Information($"Deleted Task. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}", task: task);
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
