using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;

namespace EGB_YSI6600
{
    public class EGB_YSI6600 : DataProcessor
    {
        public override string id { get => "egb_ysi66001.0"; }
        public override string name { get => "EGB_YSI6600"; }
        public override string description { get => "Processor used for EGB_YSI6600 translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            DataTable dt = null;
            try
            {
                rm = VerifyInputFile();
                if (rm.IsValid == false)
                    return rm;

                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                var worksheet = package.Workbook.Worksheets[0];  //Worksheets are zero-based index                
                string name = worksheet.Name;

                //File validation
                if (worksheet.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet 1 in InputFile:  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }

                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;
                
                string analyteID_J = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.J]));
                string analyteID_K = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.K]));
                string analyteID_L = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.L]));
                string analyteID_M = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.M]));
                string analyteID_N = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.N]));
                string analyteID_O = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.O]));
                string analyteID_P = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.P]));
                string analyteID_Q = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.Q]));
                string analyteID_R = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.R]));
                string analyteID_S = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.S]));
                string analyteID_T = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.T]));

                aliquot = "";
                string currentAliquot = "";
                for (int rowIdx = 3; rowIdx < numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    currentAliquot = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.A]);
                    string sDate = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.B]);
                    string sTime = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.C]);
                    if (!DateTime.TryParse(sDate + " " + sTime, out analysisDateTime))
                        throw  new Exception($"Invalid Analysis DateTime: {sDate} {sTime}");

                    userDefined1 = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.I]);

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