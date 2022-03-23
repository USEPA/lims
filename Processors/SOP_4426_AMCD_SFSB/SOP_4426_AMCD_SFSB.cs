using System;
using System.IO;
using System.Data;
using PluginBase;

namespace SOP_4426_AMCD_SFSB
{
    public class SOP_4426_AMCD_SFSB : DataProcessor
    {
        public override string id { get => "sop_4426_amcd_sfsb1.0"; }
        public override string name { get => "SOP_4426_AMCD_SFSB"; }
        public override string description { get => "Processor used for SOP_4426_AMCD_SFSB translation to universal template"; }
        public override string file_type { get => ".txt"; }
        public override string version { get => "1.0"; }
        public override string? input_file { get; set; }
        public override string? path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            DataTable? dt = null;
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                using StreamReader sr = new StreamReader(input_file);
                string? line;

                string regexExp = "^.*?";
                int rowIdx = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    current_row = rowIdx;
                    string currentLine = line.Trim();

                    
                    
                    
                    rowIdx++;

                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + ex.Message;
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row);
                rm.ErrorMessage = errorMsg;
            }

            rm.TemplateData = dt;

            return rm;

        }
    }
}