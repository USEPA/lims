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
    public class WorkflowServiceTest
    {
        public DataContext _context;

        private async Task<DataContext> InitContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new DataContext(options);

            for (int i = 0; i < 10; i++)
            {
                var workflow = new Workflow
                {
                    id = i.ToString(),
                    inputFolder = "path\\to\\test\\" + i.ToString(),
                    outputFolder = "path\\to\\output\\" + i.ToString(),
                    active = true,
                    name = "workflow_" + i.ToString(),
                    processor = "processor_" + i.ToString(),
                    interval = 1,
                    message = ""
                };
                await context.Workflows.AddAsync(workflow);
            }
            await context.SaveChangesAsync();
            return context;
        }


        [Theory]
        [InlineData(11)]
        public void CreateTest(int expected)
        {
            this._context = this.InitContext().Result;
            WorkflowService wkService = new WorkflowService(this._context);
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
            var result = wkService.Create(newWorkflow, true).Result;
            Assert.NotNull(result.id);                          // Returned results check

            var dbWorkflow = this._context.Workflows.Select(wk => wk.id == newWorkflow.id).ToList();
            Assert.Equal(dbWorkflow.Count, expected);           // Database update check
        }

        [Theory]
        [InlineData(false)]
        public void DeleteTest(bool expected)
        {
            this._context = this.InitContext().Result;

            LimsServer.Entities.Task tsk = new LimsServer.Entities.Task()
            {
                id = "1",
                workflowID = "0",
                start = DateTime.Now.AddMinutes(15),
                taskID = null,
                status = "SCHEDULED"
            };
            this._context.Tasks.Add(tsk);
            this._context.SaveChanges();

            WorkflowService wkService = new WorkflowService(this._context);
            var firstWk = this._context.Workflows.FirstAsync().Result;

            var result = wkService.Delete(firstWk.id).Result;
            Assert.True(result);                                // Returned results check

            var dbWorkflow = this._context.Workflows.SingleAsync(wk => wk.id == firstWk.id).Result;
            Assert.Equal(dbWorkflow.active, expected);          // Database update check
        }

        [Theory]
        [InlineData(10)]
        public void GetAllTest(int expected)
        {
            this._context = this.InitContext().Result;
            WorkflowService wkService = new WorkflowService(this._context);

            var result = wkService.GetAll().Result.ToList();
            Assert.Equal(result.Count, expected);                                // Returned results check
        }

        [Theory]
        [InlineData("3")]
        [InlineData("10")]              // Not in db (returns empty workflow)
        public void GetByIDTest(string id)
        {
            this._context = this.InitContext().Result;
            WorkflowService wkService = new WorkflowService(this._context);

            var result = wkService.GetById(id).Result;
            Assert.NotNull(result);                                // Returned results check
        }

        [Theory]
        [InlineData("workflow_test123456")]
        public void UpdateTest(string expected)
        {
            this._context = this.InitContext().Result;

            LimsServer.Entities.Task tsk = new LimsServer.Entities.Task()
            {
                id = "1",
                workflowID = "5",
                start = DateTime.Now.AddMinutes(15),
                taskID = null,
                status = "SCHEDULED"
            };
            this._context.Tasks.Add(tsk);
            LimsServer.Entities.Task tsk2 = new LimsServer.Entities.Task()
            {
                id = "2",
                workflowID = "5",
                start = DateTime.Now.AddMinutes(15),
                taskID = null,
                status = "PENDING"
            };
            this._context.Tasks.Add(tsk2);
            this._context.SaveChanges();

            WorkflowService wkService = new WorkflowService(this._context);
            Workflow newWorkflow = new Workflow()
            {
                id = "5",
                inputFolder = "path\\to\\test123456",
                outputFolder = "path\\to\\test123456\\output\\",
                active = true,
                name = "workflow_test123456",
                processor = "processor_test123456",
                interval = 1,
                message = ""
            };
            var result = wkService.Update(newWorkflow, true).Result;
            Assert.True(result);                                // Returned results check

            var dbWorkflow = this._context.Workflows.SingleAsync(wk => wk.id == newWorkflow.id).Result;
            Assert.Equal(dbWorkflow.name, expected);           // Database update check

        }
    }
} 
