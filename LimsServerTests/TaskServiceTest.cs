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


        [Fact]
        public void CreateTest()
        {
            throw new NotImplementedException("Not implemented.");
        }

        [Theory]
        [InlineData("CANCELLED")]
        public void DeleteTest(string expected)
        {
            this._context = this.InitContext().Result;
            TaskService tkService = new TaskService(this._context);
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
            TaskService tkService = new TaskService(this._context);

            var result = tkService.GetAll().Result.ToList();
            Assert.Equal(result.Count, expected);                                // Returned results check
        }

        [Theory]
        [InlineData("3")]
        [InlineData("10")]              // Not in db (returns empty workflow)
        public void GetByIDTest(string id)
        {
            this._context = this.InitContext().Result;
            TaskService tkService = new TaskService(this._context);

            var result = tkService.GetById(id).Result;
            Assert.NotNull(result);                                // Returned results check
        }
    }
} 
