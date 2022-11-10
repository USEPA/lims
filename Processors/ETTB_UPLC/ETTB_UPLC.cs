using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PluginBase;
using OfficeOpenXml;

namespace ETTB_UPLC
{
    public class ETTB_UPLC : DataProcessor
    {
        public override string id { get => "ettb_uplc"; }
        public override string name { get => "ETTB_UPLC"; }
        public override string description { get => "Processor used for ETTB_UPLC translation to universal template"; }
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

                //First row just says Column1, Column2, etc...
                for (int rowIdx = 2; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;


                    //Looking for value like 'Compound 1:  Octafluoroadipic Acid'
                    string analyteIDTmp = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.A]);
                    string pattern = "^Compound \\d+:";                    
                    Regex regex = new Regex(pattern, RegexOptions.Compiled);
                    Match match = regex.Match(analyteIDTmp);
                    if (match.Success)
                    {
                        string[] tokens = analyteIDTmp.Split(":");
                        analyteID = tokens[1].Trim();
                        continue;
                    }


                    aliquot = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.C]);

                    //Skip blank cells or cell with 'Name'
                    if (string.IsNullOrWhiteSpace(aliquot) || string.Compare(aliquot, "name", true) == 0)
                        continue;

                    //Date and time are in two different columns
                    string date = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.O]);
                    DateTime dtDate = DateTime.Parse(date);

                    string time = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.P]);
                    DateTime dtTime = DateTime.Parse(time);


                    if (!DateTime.TryParse(dtDate.ToShortDateString() + " " + dtTime.ToLongTimeString(), out analysisDateTime))
                        throw new Exception("Invalid analysis DateTime: " + date + " " + time);

                    //There are a lot of empty cells in measured value column
                    string measuredValTmp = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.M]);
                    if (!string.IsNullOrEmpty(measuredValTmp))
                    {
                        if (!Double.TryParse(measuredValTmp, out measuredVal))
                            throw new Exception("Invalid measured value: " + measuredValTmp);
                    }
                    else
                        measuredVal = 0.0;

                    dataDescription = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.D]);

                    userDefined1 = GetXLStringValue(worksheet.Cells[current_row, ColumnIndex1.J]);


                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Description"] = dataDescription;
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