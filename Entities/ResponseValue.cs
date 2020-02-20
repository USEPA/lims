using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json;

namespace LimsServer.Entities
{
    

    public class ResponseValue
    {
        
        public List<Dictionary<string, string>> errors { get; set; }
        public Dictionary<string, string> data { get; set; }
        public string message { get; set; }

        public ResponseValue(Dictionary<string, string> _data = null, Dictionary<string, string> _errors = null)
        {            
            data = _data;
        }

        public ResponseValue(string msg = null)
        {
            message = msg;
        }

        public ResponseValue()
        {
        }
        

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, typeof(ResponseValue));
        }

        private JsonSerializerOptions GetSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };

            return options;
        }
    }
}
