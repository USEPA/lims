using System;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using ExcelDataReader;

namespace CE_Instruments_NC2500_Elemental_Analyzer
{
    public class CE_Instruments_NC2500_Elemental_Analyzer : DataProcessor
    {
        public override string id { get => "ce_instruments_nc2500_elemental_analyzer"; }
        public override string name { get => "CE_Instruments_NC2500_Elemental_Analyzer"; }
        public override string description { get => "Processor used for CE Instruments NC2500 Elemental Analyzer translation to universal template"; }
        public override string file_type { get => ".XLS"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            try
            {
                //Using ExcelDataReader Package
                rm = VerifyInputFile();
                if (!rm.IsValid)
                    return rm;

                FileInfo fi = new FileInfo(input_file);

                DataTableCollection tables;
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = File.Open(input_file, FileMode.Open, FileAccess.Read);

                using var reader = ExcelReaderFactory.CreateReader(stream);
                var result = reader.AsDataSet();
                tables = result.Tables;


                DataTable dt_template = GetDataTable();
                dt_template.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
                TemplateField[] fields = Fields;

                var worksheet = tables[0];
                int numRows = worksheet.Rows.Count;
                int numCols = worksheet.Columns.Count;

                for (int row = 1; row < numRows; row++)
                {
                    string aliquot_id = worksheet.Rows[row][0].ToString();
                    DateTime analysis_datetime = fi.CreationTime.Date.Add(DateTime.Parse(worksheet.Rows[row][8].ToString()).TimeOfDay);
                }
            }

            catch (Exception ex)
            {
                rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }
            return rm;
        }

    }
}