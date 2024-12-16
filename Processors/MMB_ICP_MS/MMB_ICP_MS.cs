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

                List<string> analytes = new List<string>();
                for (int col = 7; col <= numCols; col+=2)
                {
                    string analyte = GetXLStringValue(worksheet.Cells[1, col]);
                    analytes.Add(analyte);
                }

                for (int row = 2; row <= numRows; row+=5)
                {
                    current_row = row;
                    for (int col = 1; col <= numCols; col++)
                    {
                        string aliquot = GetXLStringValue(worksheet.Cells[row, 1]);
                    }
                    
                }
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
