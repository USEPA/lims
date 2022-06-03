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
    public abstract class DataProcessor
    {
        public abstract string id { get; }
        public abstract string name { get; }
        public abstract string description { get; }
        public abstract string file_type { get; }
        public abstract string input_file { get; set; }
        public abstract string version { get; }
        public abstract string path { get; set; }
        public abstract DataTableResponseMessage Execute();

        //Keep track of current row outside of loop for error handling
        protected int current_row = 0;

        //Make these variables available at this level for use by processors
        //Use these inside loop that iterates over rows (records) in table (spreadsheet, csv, etc...)
        protected string aliquot = "";
        protected string analyteID = "";
        protected double measuredVal = Double.NaN;
        protected string units = "";
        protected double dilutionFactor = Double.NaN;
        protected DateTime analysisDateTime = DateTime.MinValue;
        protected string comment = "";
        protected string dataDescription = "";
        protected string userDefined1 = "";
        protected string userDefined2 = "";
        protected string userDefined3 = "";
        protected string userDefined4 = "";
        protected string userDefined5 = "";
        protected string userDefined6 = "";
        protected string userDefined7 = "";
        protected string userDefined8 = "";
        protected string userDefined9 = "";
        protected string userDefined10 = "";
        protected string userDefined11 = "";
        protected string userDefined12 = "";
        protected string userDefined13 = "";
        protected string userDefined14 = "";
        protected string userDefined15 = "";
        protected string userDefined16 = "";
        protected string userDefined17 = "";
        protected string userDefined18 = "";
        protected string userDefined19 = "";
        protected string userDefined20 = "";

        
        public readonly TemplateField[] Fields = new TemplateField[]
        {
                new TemplateField("Aliquot", typeof(string)),
                new TemplateField("Analyte Identifier", typeof(string)),
                //new TemplateField("Measured Value", typeof(string)),
                new TemplateField("Measured Value", typeof(double)),                
                new TemplateField("Units", typeof(string)),
                //new TemplateField("Dilution Factor", typeof(string)),
                new TemplateField("Dilution Factor", typeof(double)),
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
            if (!File.Exists(input_file))
            {
                rm.ErrorMessage = string.Format("Input data file not found: {0}", input_file);
                rm.LogMessage = string.Format("Input data file not found: {0}", input_file);
                return rm;
            }

            //Verify the file type extension is correct
            FileInfo fi = new FileInfo(input_file);
            string ext = fi.Extension;
            if (string.Compare(ext, file_type, StringComparison.OrdinalIgnoreCase) != 0)
            {
                rm.ErrorMessage = string.Format("Input data file not correct file type. Need {0} , found {1}", file_type, ext);
                rm.LogMessage = string.Format("Input data file not correct file type: {0}", input_file);                
                return rm;
            }



            //Nothing to see here
            return null;
        }

        protected string GetBaseFileName()
        {
            string baseFileName = Path.GetFileNameWithoutExtension(input_file);
            return baseFileName;
        }

        // Sometimes the dilution factor will be appended to aliquot separated by a @
        // e.g. AC1|11|ZF23|6A|MB132|10dpf@144.6
        //Should return a string array with at least 2 elements even if they are empty
        protected string[] GetAliquotDilutionFactor(string aliquot_dil_factor)
        {
            if (aliquot_dil_factor == null)
                return new string[] { "", "" };
            
            List<string> tokens = new List<string>(aliquot_dil_factor.Split("@"));
            if (tokens.Count < 2)
                tokens.Add("");
            
            return tokens.ToArray();
        }

        protected double GetXLDoubleValue(ExcelRange cell)
        {
            double dval = default;
            try
            {
                if (cell == null || cell.Value == null)
                    return dval;
                
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
            if (cell == null || cell.Value == null)
                return retVal;
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
            if (cell == null || cell.Value == null)
                return retVal;
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

        public string LogMessage { get; set; } = null;
        public string ErrorMessage { get; set; } = null;

        
        public string Message { get; set;}
       
        public string OutputFile { get; set; }

        //Input file or directory
        public string InputFile { get; set; }



        //private List<string> _logMessages = null;
        //private List<string> _errorMessages = null;                

        public ResponseMessage()
        {
            //_messages = new Dictionary<string, string>();
            //_logMessages = new List<string>();            
            //_errorMessages = new List<string>();
            
        }

        //public void AddLogMessage(string message)
        //{
        //    if (_logMessages == null)
        //        _logMessages = new List<string>();
            
        //    _logMessages.Add(message);
        //}
        //public void AddErrorMessage(string message)
        //{
        //    if (_errorMessages == null)
        //        _errorMessages = new List<string>();
        //    _errorMessages.Add(message);
        //}
        //public void AddErrorAndLogMessage(string message)
        //{
        //    AddLogMessage(message);
        //    AddErrorMessage(message);
        //}
    }

    public class DataResponseMessage : ResponseMessage
    {
        //public Dictionary<string, string> Data { get; }
        //public Dictionary<string, JArray> Data { get; }
        public DataResponseMessage()
        {
            //Data = new Dictionary<string, string>();
            //Data = new Dictionary<string, JArray>();
        }
        //public void AddData(string key, string value)
        //public void AddData(string key, JArray value)
        //{
        //    if (Data.ContainsKey(key))
        //        Data[key] = value;
        //    else
        //        Data.Add(key, value);
        //}
    }

    public class DataTableResponseMessage : ResponseMessage
    {
        public DataTable TemplateData { get; set; }

        public DataTableResponseMessage()
        {
            TemplateData = null;
        }
    }

    public static class ColumnIndex0
    {
        public readonly static int A = 0;
        public readonly static int B = 1;
        public readonly static int C = 2;
        public readonly static int D = 3;
        public readonly static int E = 4;
        public readonly static int F = 5;
        public readonly static int G = 6;
        public readonly static int H = 7;
        public readonly static int I = 8;
        public readonly static int J = 9;
        public readonly static int K = 10;
        public readonly static int L = 11;
        public readonly static int M = 12;
        public readonly static int N = 13;
        public readonly static int O = 14;
        public readonly static int P = 15;
        public readonly static int Q = 16;
        public readonly static int R = 17;
        public readonly static int S = 18;
        public readonly static int T = 19;
        public readonly static int U = 20;
        public readonly static int V = 21;
        public readonly static int W = 22;
        public readonly static int X = 23;
        public readonly static int Y = 24;
        public readonly static int Z = 25;
    }

    public static class ColumnIndex1
    {
        public readonly static int A = 1;
        public readonly static int B = 2;
        public readonly static int C = 3;
        public readonly static int D = 4;
        public readonly static int E = 5;
        public readonly static int F = 6;
        public readonly static int G = 7;
        public readonly static int H = 8;
        public readonly static int I = 9;
        public readonly static int J = 10;
        public readonly static int K = 11;
        public readonly static int L = 12;
        public readonly static int M = 13;
        public readonly static int N = 14;
        public readonly static int O = 15;
        public readonly static int P = 16;
        public readonly static int Q = 17;
        public readonly static int R = 18;
        public readonly static int S = 19;
        public readonly static int T = 20;
        public readonly static int U = 21;
        public readonly static int V = 22;
        public readonly static int W = 23;
        public readonly static int X = 24;
        public readonly static int Y = 25;
        public readonly static int Z = 26;
    }
}
