using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;


namespace CMTB_Titration
{
    public class CMTB_Titration : DataProcessor
    {
        public override string id { get => "cmtb_titration1.0"; }
        public override string name { get => "CMTB_Titration"; }
        public override string description { get => "Processor used for CMTB Titration translation to universal template"; }
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
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                var worksheet = package.Workbook.Worksheets[0];  //Worksheets are zero-based index
                string name = worksheet.Name;

                //File validation
                if (worksheet.Dimension == null)
                {
                    string msg = string.Format("No data in first sheet in InputFile:  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }

                //Rows start at 1 not 0
                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                for (int rowIdx = 1; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    aliquot = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.C]);
                    analyteID = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.J]);
                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.G]);
                    analysisDateTime = GetXLDateTimeValue(worksheet.Cells[rowIdx, ColumnIndex1.A]);                    

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Analysis Date/Time"] = analysisDateTime;
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