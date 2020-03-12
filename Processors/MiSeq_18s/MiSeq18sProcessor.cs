using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;

namespace MiSeq_18s
{
    public class MiSeq18sProcessor : DataProcessor
    {
        public override string id { get => "miseq_18s_version1.0"; }
        public override string name { get => "MiSeq_18S"; }
        public override string description { get => "Processor used for MiSeq_18S translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public MiSeq18sProcessor()
        {

        }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                DataTable dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                var worksheet = package.Workbook.Worksheets[1];  //Worksheets are zero-based index                
                string name = worksheet.Name;

                //File validation
                if (worksheet.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet 1 in InputFile:  {0}", input_file);
                    rm.AddErrorAndLogMessage(msg);
                    return rm;
                }                

                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;                                               

                for (int col = 3; col <= numCols; col++)                    
                {
                    string aliquot = GetXLStringValue(worksheet.Cells[1, col]);
                    if (aliquot.ToLower() == "reads/otu")
                        break;

                    if (string.IsNullOrWhiteSpace(aliquot))
                        break;

                    
                    for (int row = 2; row <= numRows; row++)
                    {
                        DataRow dr = dt.NewRow();
                        string analyteID = GetXLStringValue(worksheet.Cells[row, 1]);
                        string desc = GetXLStringValue(worksheet.Cells[row, 2]);
                        string measuredVal = GetXLStringValue(worksheet.Cells[row, col]);
                        if (string.IsNullOrWhiteSpace(measuredVal))
                            measuredVal = "0";
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;
                        dr["Description"] = desc;

                        dt.Rows.Add(dr);

                    }
                }
                rm.TemplateData = dt;




                }
            catch (Exception ex)
            {
                rm.AddErrorAndLogMessage("Error processing file: " + input_file);
                rm.AddErrorAndLogMessage(ex.Message);
            }

            return rm;
        }
    }
}
