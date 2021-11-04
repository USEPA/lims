using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LimsServer.Entities;
using LimsServer.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LimsServer.Services
{
    public interface ILogService
    {
        Task<Log> GetById(string id);
        Task<Log> Create(Log log);
        void Debug(string message, LimsServer.Entities.Task task);
        void Debug(string message, string workflowID);
        void Debug(string message);
        void Information(string message, LimsServer.Entities.Workflow workflow);
        void Information(string message, LimsServer.Entities.Task task);
        void Warning(string message, LimsServer.Entities.Task task);
        void Warning(string message, string workflowID);

    }
    public class LogService : ILogService
    {
        private enum LogType { Debug, Error, Warning, Information }
        private DataContext _context;
        public LogService(DataContext context)
        {
            _context = context;
        }

        public async Task<Log> GetById(string id)
        {
            return await _context.Logs.FindAsync(id);
        }

        public async Task<Log> Create(Log log)
        {
            await _context.Logs.AddAsync(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async void Information(string message, LimsServer.Entities.Task task)
        {
            // Log to console
            Serilog.Log.Information(message);
            // Get the workflow
            var workflow = await _context.Workflows.FindAsync(task.workflowID);
            // Create the log entry
            await Create(new Log()
            {
                id = Guid.NewGuid().ToString(),
                workflowId = task.workflowID,
                taskId = task.id,
                taskHangfireID = task.taskID,
                processorId = workflow.processor,
                message = message,
                type = LogType.Information.ToString(),
                time = DateTime.Now
            });
        }

        public async void Information(string message, LimsServer.Entities.Workflow workflow)
        {
            // Log to console
            Serilog.Log.Information(message);
            // Create the log entry
            await Create(new Log()
            {
                id = Guid.NewGuid().ToString(),
                workflowId = workflow.id,
                taskId = null,
                taskHangfireID = null,
                processorId = workflow.processor,
                message = message,
                type = LogType.Information.ToString(),
                time = DateTime.Now
            });
        }

        public async void Warning(string message, string workflowID)
        {
            // Log to console
            Serilog.Log.Warning(message);
            // Create the log entry
            await Create(new Log()
            {
                id = Guid.NewGuid().ToString(),
                workflowId = workflowID,
                message = message,
                type = LogType.Warning.ToString(),
                time = DateTime.Now
            });
        }

        public async void Warning(string message, LimsServer.Entities.Task task)
        {
            // Log to console
            Serilog.Log.Warning(message);
            // Get the workflow
            var workflow = await _context.Workflows.FindAsync(task.workflowID);
            // Create the log entry
            await Create(new Log()
            {
                id = Guid.NewGuid().ToString(),
                workflowId = task.workflowID,
                taskId = task.id,
                taskHangfireID = task.taskID,
                processorId = workflow.processor,
                message = message,
                type = LogType.Warning.ToString(),
                time = DateTime.Now
            });
        }

        public async void Debug(string message)
        {
            Serilog.Log.Debug(message);
            await Create(new Log()
            {
                id = Guid.NewGuid().ToString(),
                message = message,
                type = LogType.Debug.ToString(),
                time = DateTime.Now
            });
        }

        public async void Debug(string message, string workflowID)
        {
            // Log to console
            Serilog.Log.Debug(message);
            // Create the log entry
            await Create(new Log()
            {
                id = Guid.NewGuid().ToString(),
                workflowId = workflowID,
                message = message,
                type = LogType.Debug.ToString(),
                time = DateTime.Now
            });
        }

        public async void Debug(string message, LimsServer.Entities.Task task)
        {
            // Log to console
            Serilog.Log.Debug(message);
            // Get the workflow
            var workflow = await _context.Workflows.FindAsync(task.workflowID);
            // Create the log entry
            await Create(new Log()
            {
                id = Guid.NewGuid().ToString(),
                workflowId = task.workflowID,
                taskId = task.id,
                taskHangfireID = task.taskID,
                processorId = workflow.processor,
                message = message,
                type = LogType.Debug.ToString(),
                time = DateTime.Now
            });
        }
    }
}