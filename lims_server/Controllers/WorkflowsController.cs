using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using LimsServer.Entities;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LimsServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WorkflowsController : ControllerBase
    {

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ProcessorsController> _logger;

        public WorkflowsController(IWebHostEnvironment hostingEnvironment, ILogger<ProcessorsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        // GET: /Workflows
        [HttpGet]
        public IEnumerable<string> Get()
        {
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string dbPath = Path.Combine(projectRootPath, "app_files", "database", "lims.db");

            try
            {

                using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbPath))
                {
                    conn.Open();
                    SQLiteCommand command = conn.CreateCommand();
                    command.CommandText = "select * from workflows";
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        List<string> row = new List<string>();
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        row.Add(id.ToString());
                        row.Add(name);
                        //string processor = reader.GetString(2);

                    }
                }
            }
            catch(Exception ex)
            {

            }

            return new string[] { "value1", "value2" };
        }

        // GET: api/Workflows/5
        [HttpGet("{name}")]
        public string Get(string name)
        {
            return "value";
        }

        // POST: api/Workflows
        [HttpPost]
        public void Post([FromBody] Workflow value)
        {
            try
            {
                Workflow wf = value;
            }
            catch(Exception ex)
            {

            }
            
            
        }

        // PUT: api/Workflows/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
