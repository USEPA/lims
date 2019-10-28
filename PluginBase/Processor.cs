using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace PluginBase
{
    public class TemplateField
    {
        public string Name { get; set; }
        public Type DataType { get; set; }
        public TemplateField(string name, Type datatype)
        {
            Name = name;
            DataType = datatype;
        }
    }
    public class Processor
    {
        public static readonly TemplateField[] Fields = new TemplateField[]
        {
                new TemplateField("Aliquot", typeof(string)),
                new TemplateField("Analyte Identifier", typeof(string)),
                new TemplateField("Measured Value", typeof(double)),
                new TemplateField("Units", typeof(string)),
                new TemplateField("Dilution Factor", typeof(double)),
                new TemplateField("Analysis Date/Time", typeof(DateTime)),
                new TemplateField("Comment", typeof(string)),
                new TemplateField("Description", typeof(string)),
                new TemplateField("User Defined 1", typeof(string)),
                new TemplateField("User Defined 2", typeof(string)),
                new TemplateField("User Defined 3", typeof(string)),
                new TemplateField("User Defined 4", typeof(string)),
                new TemplateField("User Defined 5", typeof(string)),
                new TemplateField("User Defined 6", typeof(string)),
                new TemplateField("User Defined 7", typeof(string)),
                new TemplateField("User Defined 8", typeof(string)),
                new TemplateField("User Defined 9", typeof(string)),
                new TemplateField("User Defined 10", typeof(string)),
                new TemplateField("User Defined 11", typeof(string)),
                new TemplateField("User Defined 12", typeof(string)),
                new TemplateField("User Defined 13", typeof(string)),
                new TemplateField("User Defined 14", typeof(string)),
                new TemplateField("User Defined 15", typeof(string)),
                new TemplateField("User Defined 16", typeof(string)),
                new TemplateField("User Defined 17", typeof(string)),
                new TemplateField("User Defined 18", typeof(string)),
                new TemplateField("User Defined 19", typeof(string)),
                new TemplateField("User Defined 20", typeof(string))
        };

        public Processor()
        {
   
        }
    }

    public class ResponseMessage
    {

        public List<string> LogMessages { get { return new List<string>(_logMessages); } }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ErrorMessages { get { return new List<string>(_errorMessages); } }
        public string Message { get; set;}

        private List<string> _logMessages = null;
        private List<string> _errorMessages = null;                

        public ResponseMessage()
        {
            //_messages = new Dictionary<string, string>();
            _logMessages = new List<string>();            
            _errorMessages = new List<string>();
            
        }

        public void AddLogMessage(string message)
        {
            _logMessages.Add(message);
        }
        public void AddErrorMessage(string message)
        {
            _logMessages.Add(message);
        }
        public void AddErrorAndLogMessage(string message)
        {
            _logMessages.Add(message);
            _errorMessages.Add(message);
        }
    }

    public class DataResponseMessage : ResponseMessage
    {
        public Dictionary<string, string> Data { get; }
        public DataResponseMessage()
        {
            Data = new Dictionary<string, string>();
        }
        public void AddData(string key, string value)
        {
            if (Data.ContainsKey(key))
                Data[key] = value;
            else
                Data.Add(key, value);
        }
    }

    public class DataTableResponseMessage : ResponseMessage
    {
        public DataTable TemplateData { get; set; }

        public DataTableResponseMessage()
        {
            TemplateData = null;
        }
    }
}
