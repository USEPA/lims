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
        [NotMapped]
        public List<string> inputFiles { get; set; }
        [NotMapped]
        public List<string> outputFiles { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public DateTime start { get; set; }

        public Task() { }

        public Task(string id, string workflow, int interval)
        {
            this.id = id;
            this.taskID = null;
            this.workflowID = workflow;
            this.inputFiles = new List<string>();
            this.outputFiles = new List<string>();
            this.status = "PENDING";
            this.message = null;
            this.start = DateTime.Now.AddMinutes(interval);
        }
    }
}
