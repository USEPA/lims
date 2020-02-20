using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimsServer.Entities
{
    public class Workflow
    {
        private string BaseNetworkPath = @"\\AA\ORD\ORD\PRIV";
        public int id { get; set; }
        public string processor { get; set; }
        public string input_folder { get; set; }
        public string output_folder { get; set; }
        //Interval in minutes
        public int interval { get; set; }

    }
}
