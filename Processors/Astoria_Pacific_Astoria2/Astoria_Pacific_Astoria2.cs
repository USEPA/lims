using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;


namespace Astoria_Pacific_Astoria2
{
    public class Astoria_Pacific_Astoria2 : DataProcessor
    {

        public override string id { get => "astoria_pacific_astoria2_version1.0"; }
        public override string name { get => "Astoria_Pacific_Astoria2"; }
        public override string description { get => "Processor used for Astoria Pacific Astoria2 translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "2.0"; }
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

                string run_date = GetXLStringValue(worksheet.Cells[3, 1]);
                //Looking for string like this- 'Run date: 6/17/2021'
                string[] run_date_tokens = run_date.Split(':');
                if (run_date_tokens.Length < 2)
                {
                    rm.LogMessage = string.Format("Invalid date time string in column 1, row 3. File: {0}", input_file);
                    rm.ErrorMessage = string.Format("Invalid date time string in column 1, row 3. File: {0}", input_file);
                    return rm;
                }
                string run_date_tmp = run_date_tokens[1].Trim();
                DateTime analysis_datetime = Convert.ToDateTime(run_date_tmp);

                //These are the analytes that map to the following values in the spreadsheet in row 5:
                //Orthophosphate, Ammonia, Nitrate+Nitrite, Nitrite
                //string[] analyteIDs = new string[] { "OP", "NH3", "NO3/NO2", "NO2" };
                //KW Sept 11 2023 - change order of analytes
                string[] analyteIDs = new string[] { "NO3/NO2", "NO2", "OP", "NH3" };

                //There are 4 analytes in this file
                //Measured values are in columns G, J, M, P                
                for (int idxAnalyte=0;idxAnalyte<4;idxAnalyte++)
                {
                    //This will start us in G and move to J, M and P
                    int colIdx = (idxAnalyte * 3) + 7;

                    //Units are at the top of the measured values
                    string units = GetXLStringValue(worksheet.Cells[6, colIdx]);
                    string analyte_id = analyteIDs[idxAnalyte];

                    for (int idxRow=7;idxRow<=numRows;idxRow++)
                    {
                        current_row = idxRow;
                        //We skip row 10, it has 'NO3 Efficiency' and no measured values
                        //Switched to 12
                        //if (idxRow == 10)
                        if (idxRow == 12)
                            continue;

                        string aliquot_id = GetXLStringValue(worksheet.Cells[idxRow, ColumnIndex1.C]);
                        
                        string measure_val_tmp = GetXLStringValue(worksheet.Cells[idxRow, colIdx]);
                        if (string.IsNullOrWhiteSpace(measure_val_tmp) || string.Compare(measure_val_tmp, "???") == 0)
                            continue;

                        double measured_val = Convert.ToDouble(measure_val_tmp);

                        DataRow dr = dt.NewRow();
                        dr[0] = aliquot_id;
                        dr[1] = analyte_id;
                        dr[2] = measured_val;
                        dr[3] = units;
                        dr[5] = analysis_datetime;
                        dt.Rows.Add(dr);
                    }
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
