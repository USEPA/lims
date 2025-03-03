using OfficeOpenXml;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Data;

namespace MMB_Weights
{
    public class MMB_Weights : DataProcessor
    {
        public override string id { get => "mmb_weights_version1.0"; }
        public override string name { get => "MMB_Weights"; }
        public override string description { get => "Processor used for MMB_Weights translation to universal template"; }
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
                if (!rm.IsValid)
                    return rm;

                DataTable dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                //Data is in the 1st sheet
                var worksheet = package.Workbook.Worksheets[0]; //Worksheets are zero-based index
                string name = worksheet.Name;
                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                //There are a couple of extra rows at the end of the file that we need to skip
                for (int row = 3; row <= numRows; row++)
                {
                    analysisDateTime = GetXLDateTimeValue(worksheet.Cells[row, 1]);
                    aliquot = GetXLStringValue(worksheet.Cells[row, 2]);
                    analyteID = "Weight 1";
                    measuredVal = GetXLDoubleValue(worksheet.Cells[row, 3]);

                    DataRow dr = dt.NewRow();
                    dr["AnalysisDateTime"] = analysisDateTime;
                    dr["Aliquot"] = aliquot;
                    dr["AnalyteID"] = analyteID;
                    dr["MeasuredValue"] = measuredVal;
                    dt.Rows.Add(dr);

                    analyteID = "Weight 2";
                    measuredVal = GetXLDoubleValue(worksheet.Cells[row, 5]);
                    dr["AnalysisDateTime"] = analysisDateTime;
                    dr["Aliquot"] = aliquot;
                    dr["AnalyteID"] = analyteID;
                    dr["MeasuredValue"] = measuredVal;
                    dt.Rows.Add(dr);
                }
                rm.TemplateData = dt;
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

            return rm;
        }
    }
}
