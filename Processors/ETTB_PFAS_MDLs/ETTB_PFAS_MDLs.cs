using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PluginBase;
using OfficeOpenXml;

namespace ETTB_PFAS_MDLs
{
    public class ETTB_PFAS_MDLs : DataProcessor
    {
        public override string id { get => "ettb_pfas_mdls"; }
        public override string name { get => "ETTB_PFAS_MDLs"; }
        public override string description { get => "Processor used for ETTB_PFAS_MDLs translation to universal template"; }
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

                aliquot = GetXLStringValue(worksheet.Cells[1, ColumnIndex1.L]);

                for (int rowIdx = 2; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;

                    analyteID = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.B]);
                    if (string.IsNullOrWhiteSpace(analyteID))
                        continue;

                    measuredVal = GetXLDoubleValue(worksheet.Cells[current_row, ColumnIndex1.J]);

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);
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