using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using PluginBase;
using OfficeOpenXml;


namespace IRSB_ICP_OES
{
    public class IRSB_ICP_OES : DataProcessor
    {
        public override string id { get => "irsb_icp_oes"; }
        public override string name { get => "IRSB_ICP_OES"; }
        public override string description { get => "Processor used for IRSB_ICP_OES translation to universal template"; }
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
                if (!rm.IsValid)
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
                    throw new Exception("Spreadsheet contains no data");

                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                for (int rowIdx = 5; rowIdx <= numRows; rowIdx++)
                {
                    current_row= rowIdx;
                    aliquot = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.D]);                    
                    analysisDateTime = GetXLDateTimeValue(worksheet.Cells[current_row, ColumnIndex1.C]);

                    for (int colIdx = ColumnIndex1.J; colIdx <= ColumnIndex1.AP; colIdx++)
                    {
                        analyteID = GetXLStringValue(worksheet.Cells[4, colIdx]);
                        string tmpMeasuredVal = GetXLStringValue(worksheet.Cells[current_row, colIdx]);
                        tmpMeasuredVal = GetNumbers(tmpMeasuredVal);
                        if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                            measuredVal = 0.0;

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;

                        dt.Rows.Add(dr);
                    }

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

        private string GetNumbers(string input)
        {
            string output = Regex.Replace(input, "[^0-9.-]", "");
            return output;
        }
    }
}