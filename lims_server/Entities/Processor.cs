using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimsServer.Entities
{
    public class Processor
    {
        public string id { get; set; }
        public string name { get; set; }        
        public string description { get; set; }
        public string file_type { get; set; }
        public int process_found { get; set; }
    }
}
