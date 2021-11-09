using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PluginBase;
using OfficeOpenXml;


namespace Shimadzu_TOC_VCPH
{
    public class Shimadzu_TOC_VCPH : DataProcessor
    {

        public override string id { get => "shimadzu_toc_vcph_version1.0"; }
        public override string name { get => "Shimadzu_TOC_VCPH"; }
        public override string description { get => "Processor used for Shimadzu TOC VCPH translation to universal template"; }
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

                //Data starts in row 15
                //Column O (column 14) contains Measured Value and units -needs to be parsed. Has a label of 'Result' in row 14
                //Examples include: IC:50.07mg/L    or      NPOC:19.51mg/L
                for (int rowIdx=15;rowIdx<=numRows;rowIdx++)
                {
                    string result = GetXLStringValue(worksheet.Cells[rowIdx, 15]);
                    
                    //Stop reading when we get to an empty cell
                    if (string.IsNullOrWhiteSpace(result))
                        break;

                    string[] tokens = result.Split(':');
                    if (tokens.Length < 2)
                    {
                        //Result field is not formatted correctly
                        rm.ErrorMessage = String.Format("File: {0} - Error in format of value of column O, row {1}: {2}", input_file, rowIdx, result);
                        rm.LogMessage = String.Format("File: {0} - Error in format of value of column O, row {1}: {2}", input_file, rowIdx, result);
                        return rm;
                    }

                    //Everything left of the : should be gone - e.g 50.07mg/L
                    //Pull out the number from the string
                    var lstMeasuredVal = Regex.Split(tokens[1], @"[^0-9\.\-]+").Where(w => !String.IsNullOrEmpty(w)).ToList();
                    var lstUnits = Regex.Split(tokens[1], @"\d+").Where(c => c != "." && c != "-" && c.Trim() != "").ToList();
                    double measured_val = Convert.ToDouble(lstMeasuredVal[0]);
                    string units = lstUnits[0];

                    //Aliquot in column C - some rows are string some are numbers
                    string aliquot_id = GetXLStringValue(worksheet.Cells[rowIdx, 3]);

                    //Date time in column I - e.g. 5/27/2021 14:25
                    DateTime analysis_datetime = Convert.ToDateTime(GetXLStringValue(worksheet.Cells[rowIdx, 9]));

                    string analyteTmp = GetXLStringValue(worksheet.Cells[rowIdx, 2]);
                    string analyte_id = "";
                    if (string.Compare(analyteTmp, "IC", true) == 0)
                        analyte_id = "DIC";
                    else if (string.Compare(analyteTmp, "NPOC", true) == 0)
                        analyte_id = "DOC";
                    else
                    {
                        //Analyte ID in file is not IC or NPOC
                        rm.ErrorMessage = String.Format("File: {0} - Analyte ID is not IC or NPOC. column B, row {1}: {2}", input_file, rowIdx, analyteTmp);
                        rm.LogMessage = String.Format("File: {0} - Analyte ID is not IC or NPOC. column B, row {1}: {2}", input_file, rowIdx, analyteTmp);
                        return rm;
                    }

                    DataRow dr = dt.NewRow();
                    dr[0] = aliquot_id;
                    dr[1] = analyte_id;
                    dr[2] = measured_val;
                    dr[3] = units;
                    dr[5] = analysis_datetime;                    
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
