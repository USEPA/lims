using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LimsServer.Entities
{
    public class Task
    {
        public string id { get; set; }
        public string taskID { get; set; }
        public string workflowID { get; set; }
        public string inputFile { get; set; }
        public string outputFile { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public DateTime start { get; set; }

        public Task() { }

        public Task(string id, string workflow, int interval)
        {
            this.id = id;
            this.taskID = null;
            this.workflowID = workflow;
            this.inputFile = "";
            this.outputFile = "";
            this.status = "PENDING";
            this.message = null;
            this.start = DateTime.Now.AddMinutes(interval);
        }
    }
}
