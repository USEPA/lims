using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimsServer.Entities
{
    public class Task
    {
        public string id { get; set; }
        public string processor { get; set; }
        public string file { get; set; }
        public string status { get; set; }
    }
}
