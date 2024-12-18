using OfficeOpenXml;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMB_ICP_MS
{
    public class MMB_ICP_MS : DataProcessor
    {
        public override string id { get => "mmb_icp_ms_version1.0"; }
        public override string name { get => "MMB_ICP_MS"; }
        public override string description { get => "Processor used for MMB_ICP_MS translation to universal template"; }
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
                for (int row = 7; row <= numRows - 3; row += 5)
                {
                    aliquot = GetXLStringValue(worksheet.Cells[row, 1]);
                    if (string.IsNullOrWhiteSpace(aliquot))
                        continue;

                    current_row = row;
                    for (int col = 7; col <= numCols; col += 2)
                    {
                        analyteID = GetXLStringValue(worksheet.Cells[1, col]);
                        //Measured value is below the analyte ID
                        measuredVal = GetXLDoubleValue(worksheet.Cells[row+2, col]);

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Measured Value"] = measuredVal;
                        dr["Analyte Identifier"] = analyteID;

                        dt.Rows.Add(dr);
                    }
                }
                rm.TemplateData = dt;

            }
            catch (Exception ex)
            {
                //rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                string errorMsg = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + ex.Message;
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row);
                rm.ErrorMessage = errorMsg;
                //rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }

            return rm;

        }
    }
}
