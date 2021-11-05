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
    public class TaskServiceTest
    {
        public DataContext _context;
        public ILogService _logService;
        private async Task<DataContext> InitContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new DataContext(options);

            for (int i = 0; i < 10; i++)
            {
                var task = new LimsServer.Entities.Task
                {
                    id = i.ToString(),
                    workflowID = i.ToString(),
                    taskID = i.ToString(),
                    inputFile = "",
                    outputFile = "",
                    start = DateTime.Now.AddMinutes(1),
                    status = "SCHEDULED",
                    message = ""
                };
                await context.Tasks.AddAsync(task);
            }
            await context.SaveChangesAsync();
            return context;
        }

        public void FileSetup()
        {
            string inputPath = "app_files\\TestFiles\\Input";
            string outputPath = "app_files\\TestFiles\\Output";
            string storePath = "app_files\\TestFiles\\Store";


            if (!System.IO.Directory.Exists(inputPath))
            {
                System.IO.Directory.CreateDirectory(inputPath);
            }
            else
            {
                string[] currentFiles = System.IO.Directory.GetFiles(inputPath);
                foreach (string s in currentFiles)
                {
                    System.IO.File.Delete(s);
                }
            }

            string[] inputFiles = System.IO.Directory.GetFiles(storePath);
            foreach (string s in inputFiles)
            {
                string fileName = System.IO.Path.GetFileName(s);
                string destFile = System.IO.Path.Combine(inputPath, fileName);
                System.IO.File.Copy(s, destFile, true);
            }

            if (!System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.CreateDirectory(outputPath);
            }
            else
            {
                string[] outputFiles = System.IO.Directory.GetFiles(outputPath);
                foreach (string s in outputFiles)
                {
                    System.IO.File.Delete(s);
                }
            }

        }


        [Fact]
        public void CreateTest()
        {
            this._context = this.InitContext().Result;
            Workflow newWorkflow = new Workflow()
            {
                id = "test123456",
                inputFolder = "path\\to\\test123456",
                outputFolder = "path\\to\\test123456\\output\\",
                active = true,
                name = "workflow_test123456",
                processor = "processor_test123456",
                interval = 1,
                message = ""
            };
            this._context.Workflows.Add(newWorkflow);
            this._context.SaveChanges();

            LimsServer.Entities.Task tsk = new LimsServer.Entities.Task()
            {
                id = "001",
                workflowID = "test123456",
                start = DateTime.Now.AddMinutes(5)
            };
            TaskService ts = new TaskService(this._context, this._logService);
            var result = ts.Create(tsk).Result;
            Assert.NotNull(result.message);
        }

        [Fact]
        public void RunTaskTest()
        {
            this.FileSetup();
            this._context = this.InitContext().Result;
            Workflow newWorkflow = new Workflow()
            {
                id = "test123456",
                inputFolder = "path\\to\\test123456",
                outputFolder = "path\\to\\test123456\\output\\",
                active = true,
                name = "workflow_test123456",
                processor = "processor_test123456",
                interval = 10,
                message = ""
            };
            this._context.Workflows.Add(newWorkflow);
            Workflow newWorkflow2 = new Workflow()
            {
                id = "test1234567",
                inputFolder = "app_files\\TestFiles\\Input",
                outputFolder = "app_files\\TestFiles\\Output",
                active = true,
                name = "workflow_test123456",
                processor = "masslynx",
                interval = 10,
                message = ""
            };
            this._context.Workflows.Add(newWorkflow2);
            Workflow newWorkflow3 = new Workflow()
            {
                id = "test12345678",
                inputFolder = "app_files\\TestFiles\\Input",
                outputFolder = "app_files\\TestFiles\\Output",
                active = true,
                name = "workflow_test123456",
                processor = "picogreen",
                interval = 10,
                message = ""
            };
            this._context.Workflows.Add(newWorkflow3);

            LimsServer.Entities.Task tsk = new LimsServer.Entities.Task()
            {
                id = "001",
                workflowID = "test123456",
                start = DateTime.Now.AddMinutes(5),
                taskID = "1234567890",
                status = "SCHEDULED"
            };
            LimsServer.Entities.Task tsk2 = new LimsServer.Entities.Task()
            {
                id = "002",
                workflowID = "test123456",
                start = DateTime.Now.AddMinutes(5),
                taskID = "12345678901",
                status = "PENDING"
            };
            LimsServer.Entities.Task tsk3 = new LimsServer.Entities.Task()
            {
                id = "003",
                workflowID = "test",
                start = DateTime.Now.AddMinutes(5),
                taskID = "12345678902",
                status = "SCHEDULED"
            };
            LimsServer.Entities.Task tsk4 = new LimsServer.Entities.Task()
            {
                id = "004",
                workflowID = "test1234567",
                start = DateTime.Now.AddMinutes(5),
                taskID = "12345678903",
                status = "SCHEDULED"
            };
            LimsServer.Entities.Task tsk5 = new LimsServer.Entities.Task()
            {
                id = "005",
                workflowID = "test12345678",
                start = DateTime.Now.AddMinutes(5),
                taskID = "12345678903",
                status = "SCHEDULED"
            };
            this._context.Tasks.Add(tsk);
            this._context.Tasks.Add(tsk2);
            this._context.Tasks.Add(tsk3);
            this._context.Tasks.Add(tsk4);
            this._context.Tasks.Add(tsk5);
            this._context.SaveChanges();

            TaskService ts = new TaskService(this._context, this._logService);

            var result1 = ts.RunTask(tsk.id);
            var dbCheck1 = this._context.Tasks.SingleAsync(tk => tk.id == tsk.id).Result;
            Assert.NotNull(dbCheck1.message);

            var result2 = ts.RunTask(tsk2.id);
            var dbCheck2 = this._context.Tasks.SingleAsync(tk => tk.id == tsk2.id).Result;
            Assert.Equal("CANCELLED", dbCheck2.status);         // Invalid status to continue task

            var result3 = ts.RunTask(tsk3.id);
            var dbCheck3 = this._context.Tasks.SingleAsync(tk => tk.id == tsk3.id).Result;
            Assert.Equal("CANCELLED", dbCheck3.status);         // No workflow found with the workflow ID provided

            var result54 = ts.RunTask(tsk5.id);
            var dbCheck5 = this._context.Tasks.SingleAsync(tk => tk.id == tsk5.id).Result;
            Assert.Equal("CANCELLED", dbCheck5.status);         // Cancelled, wrong processor

            var result4 = ts.RunTask(tsk4.id);
            var dbCheck4 = this._context.Tasks.SingleAsync(tk => tk.id == tsk4.id).Result;
            Assert.Equal("COMPLETED", dbCheck4.status);         // Completed

        }

        [Theory]
        [InlineData("CANCELLED")]
        public void DeleteTest(string expected)
        {
            this._context = this.InitContext().Result;
            TaskService tkService = new TaskService(this._context, this._logService);
            var firstTk = this._context.Tasks.FirstAsync().Result;

            var result = tkService.Delete(firstTk.id).Result;
            Assert.True(result);                                // Returned results check

            var dbTask = this._context.Tasks.SingleAsync(tk => tk.id == firstTk.id).Result;
            Assert.Equal(dbTask.status, expected);              // Database update check
        }

        [Theory]
        [InlineData(10)]
        public void GetAllTest(int expected)
        {
            this._context = this.InitContext().Result;
            TaskService tkService = new TaskService(this._context, this._logService);

            var result = tkService.GetAll().Result.ToList();
            Assert.Equal(result.Count, expected);                                // Returned results check
        }

        [Theory]
        [InlineData("3")]
        [InlineData("10")]              // Not in db (returns empty workflow)
        public void GetByIDTest(string id)
        {
            this._context = this.InitContext().Result;
            TaskService tkService = new TaskService(this._context, this._logService);

            var result = tkService.GetById(id).Result;
            Assert.NotNull(result);                                // Returned results check
        }
    }
}
