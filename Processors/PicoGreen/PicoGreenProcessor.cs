using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using OfficeOpenXml;
using PluginBase;

namespace PicoGreen
{
    public class PicoGreenProcessor : DataProcessor
    {
        public override string id { get => "pico_green_version1.0.1"; }
        public override string name { get => "PicoGreen"; }
        public override string description { get => "Processor used for PicoGreen translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0.1"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        //KW
        //May 13, 2021
        //Changed on request by Curtis Callahan via email on April 06, 2021
        //private readonly string analyteID = "dsDNA";

        //KW: July 20, 2021
        //Changed on request by Curtis Callahan via email on July 20, 2021
        //Analyte ID is now in cell I1.
        //private readonly string analyteID = "dsDNA HS";        


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

                string analyteID = GetXLStringValue(worksheet.Cells[1, 9]);
                if (string.IsNullOrWhiteSpace(analyteID))
                {
                    string msg = string.Format("Input file is not in correct format. Row 1, Column 9 should contain an analyte ID", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }

                string wellID = GetXLStringValue(worksheet.Cells[18, 1]);
                if (!"Well ID".Equals(wellID, StringComparison.OrdinalIgnoreCase))
                {

                    string msg = string.Format("Input file is not in correct format. Row 18, Column 1 should contain value 'Well ID'.  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }

                //              Row 18 starts the data
                //Data File||     Well ID	Name	 Well	      485/20,528/20	  [Concentration]
                //Template ||               Aliquot  Description                  Measured Value      

                //Analyte Identifier is dsDNA for every record

                
                for (int row = 18; row<=numRows; row++)
                {
                    current_row = row;
                    wellID = GetXLStringValue(worksheet.Cells[row, 1]);
                    //if the cell is empty then start new data block 
                    if (string.IsNullOrWhiteSpace(wellID))
                        continue;

                    //if the cell contains 'Well ID' then start new data block 
                    if (wellID.Equals("Well ID", StringComparison.OrdinalIgnoreCase))
                        continue;

                    //string[] aliquot_dilFactor = GetAliquotDilutionFactor(GetXLStringValue(worksheet.Cells[row, 2]));                    
                    //string aliquot = aliquot_dilFactor[0];
                    string aliquot = GetXLStringValue(worksheet.Cells[row, 2]);

                    string description = GetXLStringValue(worksheet.Cells[row, 3]);

                    string measuredVal = GetXLStringValue(worksheet.Cells[row, 6]);

                    string dilFactor = GetXLStringValue(worksheet.Cells[row, 4]);
                    

                    //Need to account for empty cell
                    if (string.IsNullOrWhiteSpace(measuredVal))
                        continue;

                    //string dilFactor = aliquot_dilFactor[1];
                    //KW June 9, 2021
                    //This change implemented at the request of Curtis Callahan
                    //string dilFactor = GetXLStringValue(worksheet.Cells[row, 6]);
                    //If measured value is “<0.0” report value as 0
                    //If measured value is “>1050.0” report value as 1050
                    if (measuredVal.Contains("<"))
                        measuredVal = "0";
                    else if (measuredVal.Contains(">"))
                        measuredVal = "1050";

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;

                    if (!string.IsNullOrWhiteSpace(dilFactor))
                        dr["Dilution Factor"] = dilFactor;
                    else
                        dr["Dilution Factor"] = 1;

                    dr["Description"] = description;
                    dr["Measured Value"] = measuredVal;
                    dr["Analyte Identifier"] = analyteID;

                    dt.Rows.Add(dr);
                }
                rm.TemplateData = dt.Copy();
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
