using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;

namespace ACESD_Chlorophyll
{
    public class ACESD_Chlorophyll : DataProcessor
    {
        public override string id { get => "acesd_chlorophyll1.0"; }
        public override string name { get => "ACESD_Chlorophyll"; }
        public override string description { get => "Processor used for ACESD_Chlorophyll translation to universal template"; }
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

                //This value is used for every sample
                string user_defined1 = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, 6]));
                //string analyteIDBase = "Raw Fluorescence ";
                //KW - Jan 19, 2022
                // in response to email from Jakob Fox on Jan 19,2022
                string analyteID = "Raw Fluorescence";

                for (int rowIdx=7; rowIdx<numRows; rowIdx++)
                {
                    string aliquot = GetXLStringValue(worksheet.Cells[rowIdx, 1]);
                    //Number of rows extends beyond data
                    if (string.IsNullOrWhiteSpace(aliquot))
                        break;

                    double measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, 3]);
                    //KW - Jan 19, 2022
                    //double analyteIDVal = GetXLDoubleValue(worksheet.Cells[rowIdx, 2]);
                    //string analyteID = analyteIDBase + analyteIDVal.ToString();

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;                    
                    dr["User Defined 1"] = user_defined1;

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