using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using PluginBase;
using OfficeOpenXml;
using ExcelDataReader;


namespace CHL_IC
{
    public class CHL_ICProcessor : DataProcessor
    {
        public override string id { get => "chl_ic_version1.3"; }
        public override string name { get => "CHL_IC"; }
        public override string description { get => "Processor used for CHL IC translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.3"; }
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
                var worksheet = package.Workbook.Worksheets["ALL DATA"]; //Worksheets are zero-based index
                string name = worksheet.Name;
                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                //Each sample has 2 values that need to be averaged
                //that's why we increment by 2
                for (int row = 5; row <= numRows; row+=2)
                {
                    current_row = row;
                    aliquot = GetXLStringValue(worksheet.Cells[row, ColumnIndex1.D]);
                    if (string.IsNullOrWhiteSpace(aliquot))
                        continue;

                    analysisDateTime = GetXLDateTimeValue(worksheet.Cells[row, ColumnIndex1.E]);
                    
                    for (int col = ColumnIndex1.F; col <= numCols; col++)
                    {
                        string analyteID = GetXLStringValue(worksheet.Cells[3, col]);
                        if (string.Compare(analyteID, "Phosphate", true) == 0)
                            analyteID = "Ortho-Phosphate";

                        //Each sample has 2 values that need to be averaged
                        string valTmp = GetXLStringValue(worksheet.Cells[row, col]);
                        string valTmp2 = GetXLStringValue(worksheet.Cells[row+1, col]);
                        double measuredVal1 = 0.0;
                        double measuredVal2 = 0.0;
                        if (string.Compare(valTmp, "n.a.", true) == 0)
                            measuredVal1 = 0.0;
                        else if (!double.TryParse(valTmp, out measuredVal1))
                            measuredVal1 = 0.0;

                        if (string.IsNullOrWhiteSpace(valTmp2))
                            valTmp2 = valTmp;
                        if (string.Compare(valTmp2, "n.a.", true) == 0)
                            measuredVal2 = 0.0;
                        else if (!double.TryParse(valTmp2, out measuredVal2))
                            measuredVal2 = 0.0;

                        measuredVal = (measuredVal1 + measuredVal2) / 2.0;

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;
                        dr["Analysis Date/Time"] = analysisDateTime;

                        dt.Rows.Add(dr);
                    }

                    rm.TemplateData = dt;
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
            
            return rm;
        }
    }
}
