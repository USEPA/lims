using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using PluginBase;
using OfficeOpenXml;

namespace ETTB_UPLC
{
    public class ETTB_UPLC : DataProcessor
    {
        public override string id { get => "ettb_uplc"; }
        public override string name { get => "ETTB_UPLC"; }
        public override string description { get => "Processor used for ETTB_UPLC translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            DataTable dt = null;
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
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

                //First row just says Column1, Column2, etc...
                for (int rowIdx = 2; rowIdx < numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    aliquot = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.C]);

                    //Skip blank cells or cell with 'Name'
                    if (string.IsNullOrWhiteSpace(aliquot) || string.Compare(aliquot, "name", true) == 0)
                        continue;

                    string date = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.O]);
                    string time = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.P]);

                    if (!DateTime.TryParse(date + " " + time, out analysisDateTime))
                        throw new Exception("Invalid analysis DateTime: " + date + " " + time);


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