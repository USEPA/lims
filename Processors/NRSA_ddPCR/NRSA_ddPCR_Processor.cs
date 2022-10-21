using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;

namespace NRSA_ddPCR
{
    public class NRSA_ddPCR_Processor : DataProcessor
    {               

        public override string id { get => "nrsa_ddpcr1.0"; }
        public override string name { get => "NRSA_ddPCR"; }
        public override string description { get => "Processor used for NRSA_ddPCR translation to universal template"; }
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
                                    
                //Rows and columns start at 1 not 0
                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;                

                //We get 4 measured values from each row. Data will be in columns D,P,Q,R
                for (int rowIdx = 1; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    if (rowIdx == 1)
                        continue;

                    //This will be the same for all measured values in the row
                    aliquot = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.A]);
                    userDefined1 = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.B]);

                    //Info for measured value in column D
                    DataRow dr = dt.NewRow();
                    analyteID = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.D]);
                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx,ColumnIndex1.E]);
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["User Defined 1"] = userDefined1;
                    dt.Rows.Add(dr);

                    //Info for measured value in column Q
                    //These values will appear twice but only need to import one value
                    if (rowIdx % 2 == 0)
                    {
                        dr = dt.NewRow();
                        measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.Q]);
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = "Accepted Droplets";
                        dr["Measured Value"] = measuredVal;
                        dr["User Defined 1"] = userDefined1;
                        dt.Rows.Add(dr);
                    }
                    //Info for measured value in column R
                    dr = dt.NewRow();
                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.R]);
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID + " Positive Droplets";
                    dr["Measured Value"] = measuredVal;
                    dr["User Defined 1"] = userDefined1;
                    dt.Rows.Add(dr);

                    //Info for measured value in column S
                    dr = dt.NewRow();
                    measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, ColumnIndex1.S]);
                    if (measuredVal == double.NaN)
                        measuredVal = 0.0;
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID + " Negative Droplets";
                    dr["Measured Value"] = measuredVal;
                    dr["User Defined 1"] = userDefined1;
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
