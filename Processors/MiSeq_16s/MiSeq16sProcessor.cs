using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;

namespace MiSeq_16s
{
    public class MiSeq16sProcessor : DataProcessor
    {
        public override string id { get => "miseq_16s_version1.0"; }
        public override string name { get => "MiSeq_16S"; }
        public override string description { get => "Processor used for MiSeq_16S translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public MiSeq16sProcessor()
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

                var worksheet = package.Workbook.Worksheets[0];  //Worksheets are zero-based index                
                string name = worksheet.Name;

                //File validation
                if (worksheet.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet 1 in InputFile:  {0}", input_file);
                    rm.AddErrorAndLogMessage(msg);
                    return rm;
                }

                string aliquot = "";

                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                //Build list of aliquots
                List<string> lstAliquots = new List<string>();
                for (int row=1; row<=numRows; row++)
                {
                    string sval = GetXLStringValue(worksheet.Cells[row, 1]);
                    if (string.IsNullOrWhiteSpace(sval))
                        break;

                    lstAliquots.Add(sval);
                }

                int row_kingdom = 0;
                int row_phylum = 0;
                int row_class = 0;
                int row_order = 0;
                int row_family = 0;
                int row_genus = 0;

                int numAliquots = lstAliquots.Count;
                //Get description row range            
                for (int row = numAliquots + 2; row < numRows + 1; row++)
                {
                    string cell_val = Convert.ToString(worksheet.Cells[row, 1].Value);

                    if (string.IsNullOrWhiteSpace(cell_val))
                        continue;
                    cell_val = cell_val.Trim().ToLower();
                    if (cell_val == "kingdom") row_kingdom = row;
                    else if (cell_val == "phylum") row_phylum = row;
                    else if (cell_val == "class")  row_class  = row;
                    else if (cell_val == "order")  row_order  = row;
                    else if (cell_val == "family") row_family = row;
                    else if (cell_val == "genus")  row_genus  = row;
                }
            }
            catch (Exception ex)
            {
                rm.AddErrorAndLogMessage(string.Format("Problem transferring data file {0}  to template file", input_file));
            }

            return rm;
        }
    }
}
