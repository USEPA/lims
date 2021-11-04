using LimsServer.Entities;
using LimsServer.Helpers;
using LimsServer.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LimsServerTests
{
    public class HelpersDataBackupTest
    {
        public DataContext _context;
        public ILogService _logService;
        private async Task<DataContext> InitContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new DataContext(options);
            return context;
        }

        [Fact]
        public void DataCheckTest()
        {
            this._context = this.InitContext().Result;
            TaskServiceTest tst = new TaskServiceTest();
            tst.FileSetup();

            Workflow newWorkflow = new Workflow()
            {
                id = "test12345678",
                inputFolder = "app_files\\TestFiles\\Input",
                outputFolder = "app_files\\TestFiles\\Output",
                active = true,
                name = "workflow_test123456",
                processor = "masslynx",
                interval = 10,
                message = ""
            };
            this._context.Workflows.Add(newWorkflow);

            LimsServer.Entities.Task tsk = new LimsServer.Entities.Task()
            {
                id = "001",
                workflowID = "test12345678",
                start = DateTime.Now.AddMinutes(5),
                taskID = "1234567890",
                status = "SCHEDULED"
            };
            LimsServer.Entities.Task tsk2 = new LimsServer.Entities.Task()
            {
                id = "002",
                workflowID = "test12345678",
                start = DateTime.Now.AddMinutes(5),
                taskID = "1234567890",
                status = "SCHEDULED"
            };
            this._context.Tasks.Add(tsk);
            this._context.Tasks.Add(tsk2);
            this._context.SaveChanges();
            TaskService ts = new TaskService(this._context, this._logService);

            var tsResult = ts.RunTask(tsk.id);

            DataBackup db = new DataBackup();
            var results = db.DataCheck("000", this._context);
            Assert.Contains("No task ID found", results);

            var results2 = db.DataCheck(tsk.id, this._context);
            Assert.Equal("", "");

            var results3 = db.DataCheck(tsk2.id, this._context);
            Assert.Contains("Backup expired.", results3);
        }

        [Fact]
        public void GetTaskDataTest()
        {
            this._context = this.InitContext().Result;
            TaskServiceTest tst = new TaskServiceTest();
            tst.FileSetup();

            Workflow newWorkflow = new Workflow()
            {
                id = "test12345678",
                inputFolder = "app_files\\TestFiles\\Input",
                outputFolder = "app_files\\TestFiles\\Output",
                active = true,
                name = "workflow_test123456",
                processor = "masslynx",
                interval = 10,
                message = ""
            };
            this._context.Workflows.Add(newWorkflow);

            LimsServer.Entities.Task tsk = new LimsServer.Entities.Task()
            {
                id = "001",
                workflowID = "test12345678",
                start = DateTime.Now.AddMinutes(5),
                taskID = "1234567890",
                status = "SCHEDULED"
            };
            this._context.Tasks.Add(tsk);
            this._context.SaveChanges();

            TaskService ts = new TaskService(this._context, this._logService);

            var tsResult = ts.RunTask(tsk.id);

            DataBackup db = new DataBackup();
            var results = db.GetTaskData(tsk.id, this._context);
            Assert.NotNull(results);
        }

        [Fact]
        public void CleanupTest()
        {
            this._context = this.InitContext().Result;

            DataBackup db = new DataBackup();
            var result = db.Cleanup();
            Assert.True(result);
        }

    }
}
