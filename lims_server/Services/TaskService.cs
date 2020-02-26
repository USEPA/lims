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
        IEnumerable<Task> GetAll();
        Task GetById(int id);
        System.Threading.Tasks.Task<Task> Create(Task task);
        void Update(Task task);
        void Delete(int id);
    }
    public class TaskService : ITaskService
    {
        private DataContext _context;
        public TaskService(DataContext context)
        {
            _context = context;
        }

        public async void RunTask(string id, PerformContext hfContext)
        {
            string taskID = hfContext.BackgroundJob.Id;
            var task = await _context.Tasks.SingleAsync(t => t.id == id);
            task.taskID = taskID;

            // Step 1: If status!="SCHEDULED" cancel task
            if (!task.status.Equals("SCHEDULED"))
            {
                task.status = "CANCELLED";
                await _context.SaveChangesAsync();
                return;
            }
            // Step 2: Change status to "STARTING"
            task.status = "STARTING";
            await _context.SaveChangesAsync();

            var workflow = await _context.Workflows.SingleAsync(w => w.id == task.workflowID);
            if(workflow == null)
            {
                task.message = "Error attempting to get workflow of this task. Workflow ID: " + workflow.id;
                task.status = "CANCELLED";
                await _context.SaveChangesAsync();
                return;
            }

            // Step 3: Check source directory for files
            List<string> files = Directory.GetFiles(workflow.inputFolder).ToList();
            // Step 4: If none exist reschedule task
            if(files.Count == 0)
            {
                DateTime newStart = DateTime.Now.AddMinutes(workflow.interval);
                task.start = newStart;
                task.status = "SCHEDULED";
                await _context.SaveChangesAsync();
                BackgroundJob.Requeue(taskID);
                return;
            }

            // Step 5: If file does exist, update task inputFile
            task.inputFiles = files;
            task.status = "PROCESSING";
            await _context.SaveChangesAsync();

            ProcessorManager pm = new ProcessorManager();
            string config = "PATHTOPROCESSORS";
            ProcessorDTO processor = pm.GetProcessors(config).Find(p => p.Name.ToLower() == workflow.processor.ToLower());

            // Step 6: Run processor on source file
            Dictionary<string, ResponseMessage> outputs = new Dictionary<string, ResponseMessage>();
            foreach (string file in files)
            {
                string fullFilePath = System.IO.Path.Combine(workflow.inputFolder, file);
                DataTableResponseMessage result = pm.ExecuteProcessor(processor.Path, processor.UniqueId, fullFilePath);
                outputs.Add(fullFilePath, pm.WriteTemplateOutputFile(workflow.outputFolder, result.TemplateData));
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
                    task.message = "Error unable to export output. Error Messages: " + rm.Value.ErrorMessages.ToString();

                    DateTime newStart = DateTime.Now.AddMinutes(workflow.interval);
                    task.start = newStart;
                    task.status = "SCHEDULED";
                    await _context.SaveChangesAsync();
                }
            }

            // Step 9: Change task status to COMPLETED
            // Step 10: Create new Task and schedule
            if (processed) 
            {
                task.status = "COMPLETED";
                await _context.SaveChangesAsync();
                string newID = System.Guid.NewGuid().ToString();
                Task newTask = new Task(newID, workflow.id, workflow.interval);
                await this.Create(newTask);
                return;
            }
            else
            {
                task.inputFiles = new List<string>();
                task.outputFiles = new List<string>();
                BackgroundJob.Requeue(taskID);
                return;
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
            var workflow = await _context.Workflows.SingleAsync(w => w.id == task.workflowID);
            var tsk = await _context.Tasks.SingleAsync(t => t.id == task.id);

            BackgroundJob.Schedule(() => this.RunTask(tsk.id, null), TimeSpan.FromMinutes(workflow.interval));

            tsk.status = "SCHEDULED";
            await _context.SaveChangesAsync();
            return tsk;
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Task> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task GetById(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(Task task)
        {
            throw new NotImplementedException();
        }
    }
}
