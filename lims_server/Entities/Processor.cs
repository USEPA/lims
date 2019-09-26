using System;
using System.Collections.Generic;

namespace LimsServer.Entities
{
    public class Processor
    {
        public string name { get; set; }
        public string instrument { get; set; }
        public string date { get; set; }
        public Dictionary<string,string> field_mappings { get; set; }

    }
}
