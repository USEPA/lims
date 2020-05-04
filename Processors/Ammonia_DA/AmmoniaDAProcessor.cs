using System;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;

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
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                FileInfo fi = new FileInfo(input_file);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(fi);

                //Data is in the 1st sheet
                var worksheet = package.Workbook.Worksheets[0]; //Worksheets are zero-based index
                string name = worksheet.Name;
                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                DataTable dt_template = GetDataTable();
                dt_template.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
                TemplateField[] fields = Fields;

                for (int row = 2; row <= numRows; row++)
                {

                    string aliquot_id = GetXLStringValue(worksheet.Cells[row, 1]);
                    DateTime analysis_datetime = fi.CreationTime.Date.Add(GetXLDateTimeValue(worksheet.Cells[row, 9]).TimeOfDay);//Time is on column 9 but no date?
                    double measured_val = GetXLDoubleValue(worksheet.Cells[row, 2]);
                    if(measured_val == null)
                    {
                        measured_val = default;
                    }
                    string analyte_id = "NH3";
                    double dilution_factor = GetXLDoubleValue(worksheet.Cells[row, 4]);
                    string comment = GetXLStringValue(worksheet.Cells[row, 5]);

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