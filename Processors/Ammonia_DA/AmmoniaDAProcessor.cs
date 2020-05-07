using System;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using ExcelDataReader;

namespace Ammonia_DA
{
    /*class AliquotAnalyte
    {
        public string Aliquot { get; set; }
        public string AnalyteID { get; set; }
        public string MeasuredValue { get; set; }
        public string Comment { get; set; }

        public AliquotAnalyte(string aliquot, string analyteID, string measuredVal = "", string comment = "")
        {
            Aliquot = aliquot;
            AnalyteID = analyteID;
            MeasuredValue = measuredVal;
            Comment = Comment;
        }
    }*/
    public class AmmoniaDAProcessor : DataProcessor
    {
        public override string id { get => "ammonia_da_version1.0"; }
        public override string name { get => "Ammonia_DA"; }
        public override string description { get => "Processor used for Ammonia DA translation to universal template"; }
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
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                FileInfo fi = new FileInfo(input_file);

                DataTableCollection tables;
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using (var stream = File.Open(input_file, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();
                        tables = result.Tables;
                    }
                }

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
                    double measured_val = Convert.ToDouble(worksheet.Rows[row][1].ToString());
                    string analyte_id = "NH3";
                    double dilution_factor = Convert.ToDouble(worksheet.Rows[row][3].ToString());
                    string comment = worksheet.Rows[row][4].ToString();

                    DataRow dr = dt_template.NewRow();
                    dr[0] = aliquot_id;
                    dr[1] = analyte_id;
                    dr[2] = measured_val;
                    dr[4] = dilution_factor;
                    dr[5] = analysis_datetime;
                    dr[6] = comment;

                    dt_template.Rows.Add(dr);
                }

                rm.TemplateData = dt_template;
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