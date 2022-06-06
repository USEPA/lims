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
                    rm.LogMessage =msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }                

                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                //Build list of aliquots
                List<string> lstAliquots = new List<string>();
                for (int row=2; row<=numRows; row++)
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

                // Build list of analyte ids
                List<string> lstAnalytes = new List<string>();

                for (int col_idx = 2; col_idx <= numCols; col_idx++)
                {
                    int row = 1;
                    string analyte = GetXLStringValue(worksheet.Cells[row, col_idx]);
                    if (string.IsNullOrWhiteSpace(analyte))
                        break;
                    
                    lstAnalytes.Add(analyte);
                }

                //Loop over data to get measured values
                List<string> data = new List<string>();
                for (int row_idx = 2; row_idx <= lstAliquots.Count; row_idx++)
                {
                    for (int col_idx = 2; col_idx <= lstAnalytes.Count; col_idx++)
                    {
                        DataRow row = dt.NewRow();

                        row["Aliquot"] = lstAliquots[row_idx - 2];

                        row["Analyte Identifier"] = lstAnalytes[col_idx - 2];

                        string measured_val = GetXLStringValue(worksheet.Cells[row_idx, col_idx]);
                        //If meausured value is None set to 0        
                        double dval;
                        if (!string.IsNullOrWhiteSpace(measured_val))
                        {
                            if (!double.TryParse(measured_val, out dval))
                                measured_val = "0";
                        }
                        else
                            measured_val = "0";

                        row["Measured Value"] = measured_val;

                        string desc = GetXLStringValue(worksheet.Cells[row_kingdom, col_idx]) + ";";
                        desc += GetXLStringValue(worksheet.Cells[row_phylum, col_idx]) + ";";
                        desc += GetXLStringValue(worksheet.Cells[row_class, col_idx]) + ";";
                        desc += GetXLStringValue(worksheet.Cells[row_order, col_idx]) + ";";
                        desc += GetXLStringValue(worksheet.Cells[row_family, col_idx]) + ";";
                        desc += GetXLStringValue(worksheet.Cells[row_genus, col_idx]);
                        row["Description"] = desc;

                        dt.Rows.Add(row);

                    }
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
