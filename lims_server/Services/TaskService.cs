using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
            // Step 1a: If the task does not exist in the database, has been deleted then return without rescheduling.
            var task = new Task();
            try 
            { 
                var foundTask = await _context.Tasks.SingleAsync(t => t.id == id); 
                task = foundTask;
            }
            catch(ArgumentNullException ex)
            {
                _logService.Information($"No task found for WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}", task: task);
                return;
            }

            _logService.Information($"Executing Task. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}", task: task);

            // Step 1b: If status!="SCHEDULED" cancel task
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
            //Handle directory containing multiple files
            //We will treat directories like files, process it, archive it, remove it
            List<string> dirs = new List<string>(); ;
            string dirFileMessage = "";
            if (new DirectoryInfo(@workflow.inputFolder).Exists)
            {
                if (workflow.multiFile)
                    dirs = Directory.GetDirectories(@workflow.inputFolder).ToList();
                else
                    files = Directory.GetFiles(@workflow.inputFolder).ToList();
            }
            else
            {
                dirFileMessage = String.Format("Input directory {0} not found", workflow.inputFolder);
                _logService.Information(dirFileMessage, task: task);
            }
                               

            // Step 4: If directory or files or subdirectories do not exist reschedule task
            if (files.Count < 1 && dirs.Count < 1)
            {
                dirFileMessage = (dirFileMessage.Length > 0) ? dirFileMessage : String.Format("No files or subdirectories found in directory: {0}", workflow.inputFolder);
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

            Dictionary<string, ResponseMessage> outputs = new Dictionary<string, ResponseMessage>();
            DataTableResponseMessage result;
            if (workflow.multiFile)
            {
                result = await ProcessMultiFile(task, workflow, dirs[0]);
                result.OutputFile = workflow.outputFolder;
                result.TemplateData.TableName = Path.GetFileName(dirs[0]);
            }
            else
            {
                result = await ProcessSingleFile(task, workflow, files[0]);
            }
            
            //outputs.Add(result.InputFile, result);

            if (result != null && string.IsNullOrWhiteSpace(result!.ErrorMessage) && result!.TemplateData != null)
            {
                //var output = pm.WriteTemplateOutputFile(workflow.outputFolder, totalResult.TemplateData)
                var output = ProcessorManager.WriteTemplateOutputFile(workflow.outputFolder, result.TemplateData);
                result.OutputFile = output.OutputFile;
                //outputs.Add(output.OutputFile, output);
            }
            else
            {
                string errorMessage = "";
                string logMessage = "";
                if (result.TemplateData == null)
                {
                    errorMessage = "Processor results template data is null. ";
                }
                if (!String.IsNullOrEmpty(result.ErrorMessage))
                {
                    errorMessage = errorMessage + result.ErrorMessage;
                    logMessage = errorMessage;
                }
                if (result.LogMessage != null)
                {
                    logMessage = result.LogMessage;
                }
                //await this.UpdateStatus(task.id, "CANCELLED", "Error processing data: " + errorMessage);
                //_logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: {logMessage}", task: task);
                //return;
                await this.UpdateStatus(task.id, "SCHEDULED", errorMessage);
                var newSchedule = new Hangfire.States.ScheduledState(TimeSpan.FromMinutes(workflow.interval));
                task.start = DateTime.Now.AddMinutes(workflow.interval);
                await _context.SaveChangesAsync();
                try
                {
                    BackgroundJobClient backgroundClient = new BackgroundJobClient();
                    backgroundClient.ChangeState(task.taskID, newSchedule);
                    _logService.Information($"Task Rescheduled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Input Directory: {workflow.inputFolder}, Message: Invalid input file for selected processor.", task: task);
                }
                catch (Exception)
                {
                    _logService.Warning($"Error attempting to reschedule Hangfire job. Workflow ID: {task.workflowID}, task ID: {task.id}", task: task);
                }
                return;
            }

            // Step 7: Check if output file exists
            //This check shouldnt be necessesary buy whatever
            bool processed = false;
            if (result != null)
            {
                string inputPath = result.InputFile;
                if ((!workflow.multiFile && File.Exists(inputPath)) || (workflow.multiFile && Directory.Exists(inputPath)))
                {
                    processed = true;
                    task.outputFile = result.OutputFile;

                    // Get input file path
                    string fileName = System.IO.Path.GetFileName(result.InputFile);
                    inputPath = System.IO.Path.Combine(workflow.inputFolder, fileName);

                    // If archive folder exists archive input file, otherwise delete
                    await ArchiveOrDeleteInputFile(fileName, inputPath, workflow, task);
                    await _context.SaveChangesAsync();

                }
                else
                {
                    await this.UpdateStatus(task.id, "SCHEDULED", "Error unable to export output. Error Messages: " + result.ErrorMessage);
                }
            }
            
            //for (int i = 0; i < outputs.Count; i++)
            //{
            //    var output = outputs.ElementAt(i);
            //    string inputPath = output.Value.InputFile;
            //    // Step 8: If output file does exist, update task outputFile
            //    if (File.Exists(inputPath))
            //    {
            //        processed = true;
            //        task.outputFile = outputPath;

            //        // Get input file path
            //        string fileName = System.IO.Path.GetFileName(output.Key);
            //        string inputPath = System.IO.Path.Combine(workflow.inputFolder, fileName);

            //        // Backup input file
            //        DataBackup dbBackup = new DataBackup();
            //        dbBackup.DumpData(id, inputPath, outputPath);

            //        // If archive folder exists archive input file, otherwise delete
            //        await ArchiveOrDeleteInputFile(fileName, inputPath, workflow, task);
            //        await _context.SaveChangesAsync();
            //    }
            //    else
            //    {
            //        await this.UpdateStatus(task.id, "SCHEDULED", "Error unable to export output. Error Messages: " + output.Value.ErrorMessage);
            //    }
            //}

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

        protected async System.Threading.Tasks.Task<DataTableResponseMessage> ProcessMultiFile(Task task, Workflow workflow, string dataPath)
        {
            DataTableResponseMessage dataResponseMessage = null;
            // Step 5: If file does exist, update task inputFile then compare against previous Tasks.
            task.inputFile = dataPath;
            task.status = "PROCESSING";
            await _context.SaveChangesAsync();
            //bool alreadyCompleted = await this.InputDirectoryCheck(task.inputFile, task.workflowID);
            //if (alreadyCompleted)
            //{
            //    _logService.Information($"Hash input file match for WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Input File: {task.inputFile}, Message: Rerunning task after removing file.", task: task);
            //    try
            //    {
            //        File.Delete(task.inputFile);
            //        _logService.Information($"Hash input file match successfully deleted. WorkflowID: {task.workflowID}, ID: {task.id}, Input File: {task.inputFile}", task: task);
            //    }
            //    catch (FileNotFoundException ex)
            //    {
            //        _logService.Warning($"Error unable to delete input file after hash file match with previous input file. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
            //    }

            //    string statusMessage = String.Format("Input file: {0} matches previously processed input file", task.inputFile);
            //    await this.UpdateStatus(task.id, "SCHEDULED", statusMessage);
            //    await this.RunTask(task.id);
            //    return dataResponseMessage;
            //}

            ProcessorManager pm = new ProcessorManager();
            string config = "./app_files/processors";
            ProcessorDTO processor = pm.GetProcessors(config).Find(p => p.Name.ToLower() == workflow.processor.ToLower());
            if (processor == null)
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Unable to find Processor for the Task., Processor: {workflow.processor}", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Error, invalid processor name. Name: " + workflow.processor);
                return dataResponseMessage;
            }
            // If processor is disabled don't run task
            var proc = await _context.Processors.Where(p => p.name.ToLower() == workflow.processor.ToLower()).FirstOrDefaultAsync();
            if (!proc.enabled)
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Processor is not enabled.", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Error, processor is not enabled. Name: " + workflow.processor);
                return dataResponseMessage;
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

            string regexFilter = workflow.filter;
            List<string> files = Directory.GetFiles(dataPath, "*", SearchOption.AllDirectories).OrderBy(p => p).ToList();
            List<string> lstFiles = new List<string>();
            Match match;
            foreach (string fullFilename in files)
            {
                string filename = Path.GetFileName(fullFilename);
                match = Regex.Match(filename, regexFilter, RegexOptions.IgnoreCase);
                if (match.Success)
                    lstFiles.Add(fullFilename);
            }
                //Dictionary<string, ResponseMessage> outputs = new Dictionary<string, ResponseMessage>();
            string file = task.inputFile;
            DataTableResponseMessage totalResult = new DataTableResponseMessage(dataPath);
            
            foreach (string filename in lstFiles)
            {
                DataTableResponseMessage singleResult = pm.ExecuteProcessor(processor.Path, processor.Name, filename);
                GC.Collect();
                GC.WaitForPendingFinalizers();                
                if (singleResult.ErrorMessage == null && singleResult.TemplateData != null)
                {
                    if (totalResult.TemplateData == null)
                        totalResult.TemplateData = singleResult.TemplateData;
                    else
                        totalResult.TemplateData.Merge(singleResult.TemplateData);
                }
                else
                {
                    totalResult.TemplateData = null;
                    totalResult.ErrorMessage = $"Unable to process: {filename} for multifile data in: {dataPath}";
                    return totalResult;
                }
                
            }

            totalResult.IsValid = true;
            return totalResult;
        }

        protected async System.Threading.Tasks.Task<DataTableResponseMessage> ProcessSingleFile(Task task, Workflow workflow, string dataFile)
        {
            DataTableResponseMessage dataResponseMessage = null;
            // Step 5: If file does exist, update task inputFile then compare against previous Tasks.
            task.inputFile = dataFile;
            task.status = "PROCESSING";
            await _context.SaveChangesAsync();
            //bool alreadyCompleted = await this.InputFileCheck(task.inputFile, task.workflowID);
            //if (alreadyCompleted)
            //{
            //    _logService.Information($"Hash input file match for WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Input File: {task.inputFile}, Message: Rerunning task after removing file.", task: task);
            //    try
            //    {
            //        File.Delete(task.inputFile);
            //        _logService.Information($"Hash input file match successfully deleted. WorkflowID: {task.workflowID}, ID: {task.id}, Input File: {task.inputFile}", task: task);
            //    }
            //    catch (FileNotFoundException ex)
            //    {
            //        _logService.Warning($"Error unable to delete input file after hash file match with previous input file. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
            //    }

            //    string statusMessage = String.Format("Input file: {0} matches previously processed input file", task.inputFile);
            //    await this.UpdateStatus(task.id, "SCHEDULED", statusMessage);
            //    await this.RunTask(task.id);
            //    return dataResponseMessage;
            //}

            ProcessorManager pm = new ProcessorManager();
            string config = "./app_files/processors";
            ProcessorDTO processor = pm.GetProcessors(config).Find(p => p.Name.ToLower() == workflow.processor.ToLower());
            if (processor == null)
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Unable to find Processor for the Task., Processor: {workflow.processor}", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Error, invalid processor name. Name: " + workflow.processor);
                return dataResponseMessage;
            }

            // If processor is disabled don't run task
            var proc = await _context.Processors.Where(p => p.name.ToLower() == workflow.processor.ToLower()).FirstOrDefaultAsync();
            if (!proc.enabled)
            {
                _logService.Information($"Task Cancelled. WorkflowID: {task.workflowID}, ID: {task.id}, Hangfire ID: {task.taskID}, Message: Processor is not enabled.", task: task);
                await this.UpdateStatus(task.id, "CANCELLED", "Error, processor is not enabled. Name: " + workflow.processor);
                return dataResponseMessage;
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

            return result;
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
        /// Get all task IDs for the specified workflow, get the input bytes and run a MD5 hash, compare to the inputDirectory
        /// Courtesy of stackoverflow: https://stackoverflow.com/questions/3625658/how-do-you-create-the-hash-of-a-folder-in-c
        /// </summary>
        /// <param name="inputDirectoryPath"></param>
        /// <param name="workflowID"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<bool> InputDirectoryCheck(string inputDirectoryPath, string workflowID)
        {
            var tasks = await _context.Tasks.Where(t => t.workflowID == workflowID && t.status == "COMPLETED").ToListAsync();
            bool match = false;

            if (tasks.Count >= 1)
            {
                var filePaths = Directory.GetFiles(inputDirectoryPath, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();
                using var md5 = MD5.Create();
                
                foreach (var filePath in filePaths)
                {
                    // hash path
                    byte[] pathBytes = Encoding.UTF8.GetBytes(filePath);
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    // hash contents
                    byte[] contentBytes = File.ReadAllBytes(filePath);

                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }

                //Handles empty filePaths case
                md5.TransformFinalBlock(new byte[0], 0, 0);

                string inputHash = BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
                _logService.Debug($"Input file compare. WorkflowID: {workflowID}, InputFile: {inputDirectoryPath}, InputFile Hash: {inputHash}", workflowID: workflowID);
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
        /// Archive or delete input file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="inputPath"></param>
        /// <param name="workflow"></param>
        private async System.Threading.Tasks.Task ArchiveOrDeleteInputFile(string fileName, string inputPath, Workflow workflow, Task task)
        {
            // If archive folder exists archive input file, otherwise delete
            if (string.IsNullOrEmpty(workflow.archiveFolder))
            {
                try
                {
                    File.Delete(inputPath);
                }
                catch (Exception ex)
                {
                    _logService.Warning($"Error unable to delete input file after successful processing. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
                }
            }
            else
            {
                try
                {
                    // Check that archive folder exists, and create if not
                    if (!Directory.Exists(workflow.archiveFolder))
                        Directory.CreateDirectory(workflow.archiveFolder);
                    // Move input file to archive folder
                    string archivePath = System.IO.Path.Combine(workflow.archiveFolder, fileName);

                    //if (workflow.multiFile)
                    //{
                    //    Directory.Move(inputPath, archivePath);
                    //}
                    //else
                    //{
                    //    File.Move(inputPath, archivePath);
                    //}
                    archivePath = MoveFileOrDirectory(inputPath, archivePath, workflow.multiFile);

                    // Set task archiveFile location
                    task.archiveFile = archivePath;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    if (ex is UnauthorizedAccessException)
                    {
                        _logService.Warning($"Error unable to archive input file after successful processing. Unauthorized access to archive folder. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
                    }
                    else
                    {
                        _logService.Warning($"Error unable to archive input file after successful processing. Workflow ID: {task.workflowID}, ID: {task.id}", task: task);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method for moving a file or directory
        /// and appending a timestamp and iteration number
        /// if the file or directory already exist
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="archivePath"></param>
        /// <param name="multi"></param>
        /// <returns>the resolved path as a string</returns>
        protected string MoveFileOrDirectory(string inputPath, string archivePath, bool multi)
        {
            int existingPathCount = 0;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            if (multi)
            {
                if (Directory.Exists(archivePath))
                {
                    string archivePathTimestamped = archivePath + "_" + timestamp;
                    while (Directory.Exists(archivePathTimestamped))
                    {
                        existingPathCount++;
                        archivePathTimestamped = archivePathTimestamped + "(" + existingPathCount + ")";
                    }
                    archivePath = archivePathTimestamped;
                }
                Directory.Move(inputPath, archivePath);
            }
            else
            {
                if (File.Exists(archivePath))
                {
                    string archivePathDir = Path.GetDirectoryName(archivePath);
                    string archiveFileName = Path.GetFileNameWithoutExtension(archivePath);
                    string archiveFileExt = Path.GetExtension(archivePath);

                    string archivePathTimestamped = Path.Join(archivePathDir, archiveFileName + "_" + timestamp);
                    string archiveFullPath = archivePathTimestamped + archiveFileExt;
                    while (File.Exists(archiveFullPath))
                    {
                        existingPathCount++;
                        archiveFullPath = archivePathTimestamped + "(" + existingPathCount + ")" + archiveFileExt;
                    }
                    archivePath = archiveFullPath;
                }
                File.Move(inputPath, archivePath);
            }
            return archivePath;
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
                    _context.Tasks.Remove(task);
                    if (task.taskID != null)
                    {
                        BackgroundJob.Delete(task.taskID);
                    }
                }
                catch (InvalidOperationException)
                {
                    _logService.Information($"No Hangfire task found for ID: {task.taskID}", task: task);
                }
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
