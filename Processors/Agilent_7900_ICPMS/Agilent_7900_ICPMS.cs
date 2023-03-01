using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;

namespace Agilent_7900_ICPMS
{
    public class Agilent_7900_ICPMS : DataProcessor
    {
        public override string id { get => "agilent_7900_icpms"; }
        public override string name { get => "Agilent_7900_ICPMS"; }
        public override string description { get => "Processor used for Agilent_7900_ICPMS translation to universal template"; }
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
                    current_row = rowIdx;
                    aliquot = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.D]);
                    string dateTime = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.D]);
                    analysisDateTime = Convert.ToDateTime(dateTime);

                    for (int colIdx = ColumnIndex1.H; colIdx <= numCols; colIdx=colIdx+2)
                    {
                        analyteID = GetXLStringValue(worksheet.Cells[1, colIdx]);
                        string mval = GetXLStringValue(worksheet.Cells[current_row, colIdx]);

                        //Convert blank cells, “N/A”, and “<0.00” to be imported as “0”
                        if (!Double.TryParse(mval, out measuredVal))
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
    }
}