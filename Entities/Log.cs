using System;

namespace LimsServer.Entities
{
    public class Log
    {
        public string id { get; set; }
        public string workflowId { get; set; }
        public string taskId { get; set; }
        public string taskHangfireID { get; set; }
        public string processorId { get; set; }
        public string message { get; set; }

        /// <summary>
        /// The log type: Information, Debug, Warning, Error, Fatal
        /// </summary>
        public string type { get; set; }
        public DateTime time { get; set; }
    }
}