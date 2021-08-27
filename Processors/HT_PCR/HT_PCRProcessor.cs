using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;
//using ExcelDataReader;

namespace HT_PCR
{
    public class HT_PCRProcessor : DataProcessor
    {        

        public override string id { get => "ht_pcr_version1.0"; }
        public override string name { get => "HT_PCR"; }
        public override string description { get => "Processor used for HT_PCR translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            const string REMOVE = "â€™";
            const string REMOVE2 = "â€";
            string dateTime = "";

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

                //Date time is in row 1 columh H (8)
                if (worksheet.Cells[1, 8].Value == null)
                    throw new Exception($"");

                dateTime = worksheet.Cells[1, 8].Value.ToString();


                DataTable dtTemp = new DataTable();

                //Get names of columns
                //Dont really need this but it constructs a datatable with correct number of columns
                for (int col = 1; col <= numCols; col++)
                {
                    string colName = "";
                    if (worksheet.Cells[11, col].Value != null)
                        colName = worksheet.Cells[11, col].Value.ToString();

                    string colName2 = "";
                    if (worksheet.Cells[12, col].Value != null)
                        colName2 = worksheet.Cells[12, col].Value.ToString();

                    DataColumn dc = new DataColumn(colName + "-" + colName2);
                    dtTemp.Columns.Add(dc);
                }

                //Loop through the data and push it into a datatable
                //Replace the first specified string with a single quote
                for (int row = 13; row <= numRows; row++)
                {
                    DataRow dr = dtTemp.NewRow();
                    for (int col = 1; col <= numCols; col++)
                    {
                        string val = "";
                        if (worksheet.Cells[12, col].Value != null)
                        {
                            val = worksheet.Cells[row, col].Value.ToString();
                            val = val.Replace(REMOVE, "'");
                        }
                        dr[col - 1] = val;
                    }
                    dtTemp.Rows.Add(dr);
                }

                //Loop through the datatable and replace the second specified string with two single quotes
                for (int row = 0; row < dtTemp.Rows.Count; row++)
                {
                    for (int col = 0; col < dtTemp.Columns.Count; col++)
                    {
                        string val = dtTemp.Rows[row][col].ToString();
                        val = val.Replace(REMOVE2, "''");
                        dtTemp.Rows[row][col] = val;
                    }
                }

                for (int row = 0; row < dtTemp.Rows.Count; row++)
                {
                    string analyteID = dtTemp.Rows[row][4].ToString();
                    string measuredVal = dtTemp.Rows[row][6].ToString().Trim();
                    if (measuredVal == "999")
                        measuredVal = "NA";

                    string aliquot = dtTemp.Rows[row][1].ToString();

                    string userDefined1 = dtTemp.Rows[row][7].ToString();
                    string userDefined2 = dtTemp.Rows[row][10].ToString();
                    string userDefined3 = dtTemp.Rows[row][11].ToString();
                    string userDefined4 = dtTemp.Rows[row][12].ToString();

                    DataRow dr = dt.NewRow();
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Aliquot"] = aliquot;
                    dr["Analysis Date/Time"] = dateTime;
                    dr["User Defined 1"] = userDefined1;
                    dr["User Defined 2"] = userDefined2;
                    dr["User Defined 3"] = userDefined3;
                    dr["User Defined 4"] = userDefined4;

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
