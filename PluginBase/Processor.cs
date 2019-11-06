using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;
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

        public DataTable GetDataTable()
        {
            DataTable dt_template = new DataTable();
            TemplateField[] fields = Processor.Fields;

            for (int idx = 0; idx < fields.Length; idx++)
            {
                DataColumn dc = new DataColumn(fields[idx].Name, fields[idx].DataType);
                if (fields[idx].DataType == typeof(string))
                    dc.DefaultValue = "";
                dt_template.Columns.Add(dc);
            }

            return dt_template;
        }
}

    public class ResponseMessage
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> LogMessages { 
            get 
            { if (_logMessages == null)
                    return null;
              else 
                    return new List<string>(_logMessages); 
            } 
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ErrorMessages
        {
            get
            {
                if (_errorMessages == null)
                    return null;
                else 
                    return new List<string>(_errorMessages);
            }
        }
        public string Message { get; set;}

        private List<string> _logMessages = null;
        private List<string> _errorMessages = null;                

        public ResponseMessage()
        {
            //_messages = new Dictionary<string, string>();
            //_logMessages = new List<string>();            
            //_errorMessages = new List<string>();
            
        }

        public void AddLogMessage(string message)
        {
            if (_logMessages == null)
                _logMessages = new List<string>();
            
            _logMessages.Add(message);
        }
        public void AddErrorMessage(string message)
        {
            if (_errorMessages == null)
                _errorMessages = new List<string>();
            _errorMessages.Add(message);
        }
        public void AddErrorAndLogMessage(string message)
        {
            AddLogMessage(message);
            AddErrorMessage(message);
        }
    }

    public class DataResponseMessage : ResponseMessage
    {
        //public Dictionary<string, string> Data { get; }
        public Dictionary<string, JArray> Data { get; }
        public DataResponseMessage()
        {
            //Data = new Dictionary<string, string>();
            Data = new Dictionary<string, JArray>();
        }
        //public void AddData(string key, string value)
        public void AddData(string key, JArray value)
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
