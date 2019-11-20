using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OfficeOpenXml;


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
    public abstract class Processor
    {
        public abstract string UniqueId { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string InstrumentFileType { get; }
        public abstract string InputFile { get; set; }
        public abstract string Path { get; set; }
        public abstract DataTableResponseMessage Execute();

        public readonly TemplateField[] Fields = new TemplateField[]
        {
                new TemplateField("Aliquot", typeof(string)),
                new TemplateField("Analyte Identifier", typeof(string)),
                new TemplateField("Measured Value", typeof(string)),
                //new TemplateField("Measured Value", typeof(double)),
                new TemplateField("Units", typeof(string)),
                new TemplateField("Dilution Factor", typeof(string)),
                //new TemplateField("Dilution Factor", typeof(double)),
                new TemplateField("Analysis Date/Time", typeof(string)),
                //new TemplateField("Analysis Date/Time", typeof(DateTime)),
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
            TemplateField[] fields = Fields;

            for (int idx = 0; idx < fields.Length; idx++)
            {
                DataColumn dc = new DataColumn(fields[idx].Name, fields[idx].DataType);
                if (fields[idx].DataType == typeof(string))
                    dc.DefaultValue = "";
                dt_template.Columns.Add(dc);
            }

            DataColumn[] primKeys = new DataColumn[2];
            primKeys[0] = dt_template.Columns["Aliquot"];
            primKeys[1] = dt_template.Columns["Analyte Identifier"];
            //dt_template.PrimaryKey = primKeys;

            return dt_template;
        }

        public DataTableResponseMessage VerifyInputFile()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();

            //Verify that the file exists
            if (!File.Exists(InputFile))
            {
                rm.ErrorMessages.Add(string.Format("Input data file not found: {0}", InputFile));
                rm.LogMessages.Add(string.Format("Input data file not found: {0}", InputFile));
                return rm;
            }

            //Verify the file type extension is correct
            FileInfo fi = new FileInfo(InputFile);
            string ext = fi.Extension;
            if (string.Compare(ext, InstrumentFileType, StringComparison.OrdinalIgnoreCase) != 0)
            {
                rm.AddErrorAndLogMessage(string.Format("Input data file not correct file type. Need {0} , found {1}", InstrumentFileType, ext));
                return rm;
            }

            //Nothing to see here
            return null;
        }

        protected double GetXLDoubleValue(ExcelRange cell)
        {
            double dval = default;
            try
            {
                bool bval = Double.TryParse(cell.Value.ToString().Trim(), out dval);
                if (bval)
                    return dval;
            }
            catch(Exception ex)
            { }
            return dval;
            
        }
        protected string GetXLStringValue(ExcelRange cell)
        {
            string retVal = "";
            try
            {   
                retVal = Convert.ToString(cell.Value.ToString().Trim());
            }
            catch(Exception ex)
            { }
            return retVal;
        }

        protected DateTime GetXLDateTimeValue(ExcelRange cell)
        {
            DateTime retVal = default;
            try
            {             
                retVal = Convert.ToDateTime(cell.Value.ToString().Trim());
            }
            catch(Exception ex)
            { }
            return retVal;
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
        public string OutputFile { get; set; }

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
