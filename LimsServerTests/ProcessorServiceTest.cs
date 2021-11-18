using LimsServer.Entities;
using LimsServer.Helpers;
using LimsServer.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LimsServerTests
{
    public class ProcessorServiceTest
    {

        public DataContext _context;

        private async Task<DataContext> InitContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new DataContext(options);
            return context;
        }

        [Fact]
        public void CreateTest()
        {
            this._context = this.InitContext().Result;
            ProcessorService ps = new ProcessorService(this._context);
            Processor p = new Processor()
            {
                id = "0",
                name = "test_processor",
                version = "0.0.1",
                enabled = true,
                description = "test processor",
                file_type = ".fake",
                process_found = 1
            };
            var p0 = ps.Create(p).Result;
            var result = this._context.Processors.SingleAsync(x => x.id == p.id).Result;
            Assert.NotNull(result);

            Processor p2 = new Processor();
            var r2 = ps.Create(p2).Result;
            Assert.Null(r2.id);
        }

        [Theory]
        [InlineData(2)]
        public void GetAllTest(int expected)
        {
            this._context = this.InitContext().Result;
            ProcessorService ps = new ProcessorService(this._context);
            Processor p1 = new Processor()
            {
                id = "0",
                name = "test_processor1",
                version = "0.0.1",
                enabled = true,
                description = "test processor1",
                file_type = ".fake",
                process_found = 1
            };
            Processor p2 = new Processor()
            {
                id = "1",
                name = "test_processor2",
                version = "0.0.1",
                enabled = true,
                description = "test processor2",
                file_type = ".fake",
                process_found = 1
            };
            this._context.Processors.AddAsync(p1);
            this._context.Processors.AddAsync(p2);
            this._context.SaveChangesAsync();

            var results = ps.GetAll().Result.ToList();
            Assert.Equal(expected, results.Count);
        }

        [Fact]
        public void GetByIdTest()
        {
            this._context = this.InitContext().Result;
            ProcessorService ps = new ProcessorService(this._context);
            Processor p1 = new Processor()
            {
                id = "0",
                name = "test_processor1",
                version = "0.0.1",
                enabled = true,
                description = "test processor1",
                file_type = ".fake",
                process_found = 1
            };
            this._context.Processors.AddAsync(p1);
            this._context.SaveChangesAsync();

            var result = ps.GetByName(p1.name).Result;
            Assert.NotNull(result);

            var badResult = ps.GetByName("fakeID").Result;
            Assert.Null(badResult);
        }

        [Fact]
        public void UpdateTest()
        {
            this._context = this.InitContext().Result;
            ProcessorService ps = new ProcessorService(this._context);
            Processor p1 = new Processor()
            {
                id = "0",
                name = "test_processor1",
                version = "0.0.1",
                enabled = true,
                description = "test processor1",
                file_type = ".fake",
                process_found = 1
            };
            Processor p2 = new Processor()
            {
                id = "0",
                name = "test_processor2",
                version = "0.0.2",
                enabled = true,
                description = "test processor2",
                file_type = ".fake2",
                process_found = 1
            };
            this._context.Processors.AddAsync(p1);
            this._context.SaveChangesAsync();

            var t = ps.Update(p1.id, p2);
            var result = this._context.Processors.SingleAsync(p => p.id == p1.id).Result;
            Assert.Equal(p2.name, result.name);             // P1 processor updated with P2

            var t2 = ps.Update("invalid", p1);
            var result2 = this._context.Processors.SingleAsync(p => p.id == p1.id).Result;
            Assert.Equal(p2.name, result2.name);            // No update expected.
        }

        [Fact]
        public void UpdateListTest()
        {
            this._context = this.InitContext().Result;
            ProcessorService ps = new ProcessorService(this._context);
            Processor p1 = new Processor()
            {
                id = "0",
                name = "test_processor1",
                version = "0.0.1",
                enabled = true,
                description = "test processor1",
                file_type = ".fake",
                process_found = 1
            };
            Processor p2 = new Processor()
            {
                id = "1",
                name = "test_processor2",
                version = "0.0.1",
                enabled = true,
                description = "test processor2",
                file_type = ".fake",
                process_found = 1
            };
            this._context.Processors.AddAsync(p1);
            this._context.Processors.AddAsync(p2);
            this._context.SaveChangesAsync();

            p1.file_type = ".very_fake1";
            p2.file_type = ".very_fake2";
            Processor[] pList = new Processor[] { p1, p2 };

            var t = ps.Update(pList);
            var result1 = this._context.Processors.SingleAsync(p => p.id == p1.id).Result;
            Assert.Equal(p1.file_type, result1.file_type);

            var result2 = this._context.Processors.SingleAsync(p => p.id == p2.id).Result;
            Assert.Equal(p2.file_type, result2.file_type);
        }


    }
}
