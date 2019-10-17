using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace LimsServer.Entities
{
    public class ReturnMessage
    {
        public string status { get; set; }
        public string message { get; set; }
        public Dictionary<string, string> data { get; set; }

        public ReturnMessage(string _status, Dictionary<string, string> _data = null, string _message = "")
        {
            status = _status;
            data = _data;
            message = _message;
        }
        public JObject ToJObject()
        {
            JObject jo = JToken.FromObject(this) as JObject;
            return jo;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
