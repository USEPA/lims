using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;

namespace Chlorophyll
{
    public class ChlorophyllProcessor : DataProcessor
    {
        public override string id { get => "chlorophyll_version1.0.0"; }
        public override string name { get => "Chlorophyll"; }
        public override string description { get => "Processor used for Chlorophyll translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0.0"; }
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

                string analyte_post_acidification = "post-acidification";

                //Rows and columns start at 1 not 0
                //First row is header data
                for (int row = 2; row <= numRows; row++)
                {
                    current_row = row;
                    string aliquot = GetXLStringValue(worksheet.Cells[row, 2]);
                    //Lets check for empty cell, assume we are done if we hit one
                    if (string.IsNullOrWhiteSpace(aliquot))
                        break;

                    string description = GetXLStringValue(worksheet.Cells[row, 11]);
                    double dilution_factor = GetXLDoubleValue(worksheet.Cells[row, 13]);
                    double user_defined1 = GetXLDoubleValue(worksheet.Cells[row, 14]);
                    DateTime analysis_date = GetXLDateTimeValue(worksheet.Cells[row, 26]);                    

                    //Columns O-W contain measured values
                    //Analyte ID is based on value in column L (0 or 1);
                    int acidified = Convert.ToInt32(GetXLDoubleValue(worksheet.Cells[row, 12]));
                    for (int col = 15; col < 24; col++)
                    {
                        //Column header will look something like: abs_400
                        //Analyte ID will look like 400nm or 400nm post-acidification depending on acidified value in column L
                        string inst_analyte_name = GetXLStringValue(worksheet.Cells[1, col]);
                        string inst_analyte_num = inst_analyte_name.Split("_")[1].Trim();
                        string analyteID = "";
                        double measured_val = 0.0;
                        if (acidified == 0)
                            analyteID = inst_analyte_num + "nm";
                        else if (acidified ==1)
                            analyteID = inst_analyte_num + "nm " + analyte_post_acidification;

                        measured_val = GetXLDoubleValue(worksheet.Cells[row, col]);

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measured_val;
                        dr["Dilution Factor"] = dilution_factor;
                        dr["Analysis Date/Time"] = analysis_date;
                        dr["User Defined 1"] = user_defined1;

                        dt.Rows.Add(dr);

                    }
                                                            
                }
                rm.TemplateData = dt;

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
