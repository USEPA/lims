using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;

namespace SFSB_Slopes_Intercepts
{
    public class SFSB_Slopes_Intercepts : DataProcessor
    {
        public override string id { get => "Sfsb_slopes_intercepts1.0"; }
        public override string name { get => "SFSB_Slopes_Intercepts"; }
        public override string description { get => "Processor used for SFSB_Slopes_Intercepts translation to universal template"; }
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
                ExcelPackage.LicenseContext =OfficeOpenXml.LicenseContext.NonCommercial;
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

                string analysisDT = GetXLStringValue(worksheet.Cells[1, ColumnIndex1.H]);
                if (!DateTime.TryParse(analysisDT, out analysisDateTime))
                    throw new Exception("Invalid Analysis DateTime: " + analysisDT);

                string aliquotB = GetXLStringValue(worksheet.Cells[3, ColumnIndex1.B]);
                string aliquotC = GetXLStringValue(worksheet.Cells[3, ColumnIndex1.C]);
                string aliquotD = GetXLStringValue(worksheet.Cells[3, ColumnIndex1.D]);
                string aliquotE = GetXLStringValue(worksheet.Cells[3, ColumnIndex1.E]);

                for (int rowIdx = 4; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    analyteID = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.A]);

                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.B]);
                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquotB;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.C]);
                    dr["Aliquot"] = aliquotC;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.D]);
                    dr["Aliquot"] = aliquotD;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.E]);
                    dr["Aliquot"] = aliquotE;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);
                }                    
      
            }

            catch (Exception ex)
            {
                rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }

            rm.TemplateData = dt;

            return rm;
        }
    }
}