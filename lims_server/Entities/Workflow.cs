using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimsServer.Entities
{
    public class Workflow
    {
        private string BaseNetworkPath = @"\\AA\ORD\ORD\PRIV";
        public string Processor { get; set; }
        public string InputFolder { get; set; }
        public string OutputFolder { get; set; }
        //Interval in minutes
        public int PollingInterval { get; set; }


    }
}
