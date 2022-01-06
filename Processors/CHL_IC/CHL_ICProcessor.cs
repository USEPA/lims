using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using PluginBase;
using ExcelDataReader;


namespace CHL_IC
{
    public class CHL_ICProcessor : DataProcessor
    {
        public override string id { get => "chl_ic_version1.0"; }
        public override string name { get => "CHL_IC"; }
        public override string description { get => "Processor used for CHL IC translation to universal template"; }
        public override string file_type { get => ".XLS"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            try
            {
                //Using ExcelDataReader Package
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                FileInfo fi = new FileInfo(input_file);

                DataTableCollection tables;
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using (var stream = File.Open(input_file, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();
                        tables = result.Tables;
                    }
                }

                //We are looking for a worksheet named: 'Summary - INJ. vs ANION'
                string tableName = "Summary - INJ. vs ANION";
                DataTable worksheet = null;
                foreach (DataTable table in tables)
                {
                    int i = 1;
                    if (string.Compare(table.TableName, tableName, true) == 0)
                    {
                        worksheet = table;
                        break;
                    }
                }

                //We have a problem if we can't find the right worksheet
                if (worksheet == null)
                {
                    rm.ErrorMessage = $"Processor: {name},  InputFile: {input_file}, Worksheet {tableName} not found.";
                    rm.LogMessage = $"Processor: {name},  InputFile: {input_file}, Worksheet {tableName} not found.";
                    return rm;
                }

                DataTable dt = GetDataTable();
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
                TemplateField[] fields = Fields;
                
                int numRows = worksheet.Rows.Count;
                int numCols = worksheet.Columns.Count;

                //Analyte IDs are in row 6 starting in column C (which is column 2 - since datatables are zero based)
                List<string> lstAnalyteIDs = new List<string>();
                for (int colIdx = 2; colIdx < numCols; colIdx++)
                {
                    string analyteID = worksheet.Rows[5][colIdx].ToString();
                    if (string.IsNullOrEmpty(analyteID))
                        break;
                    if (string.Compare(analyteID, "Phosphate", true) == 0)
                        analyteID = "Ortho-Phosphate";

                    lstAnalyteIDs.Add(analyteID);
                }


                //Data starts in row 7 (rowIdx 6 since datatables are zero based)
                for (int rowIdx = 6; rowIdx <= numRows; rowIdx++)
                {
                    string numID = worksheet.Rows[rowIdx][0].ToString().Trim();
                    if (string.IsNullOrEmpty(numID))
                        continue;

                    string aliquot_id = worksheet.Rows[rowIdx][0].ToString();
                    DateTime analysis_datetime = fi.CreationTime.Date.Add(DateTime.Parse(worksheet.Rows[rowIdx][8].ToString()).TimeOfDay);
                    double measured_val = Convert.ToDouble(worksheet.Rows[rowIdx][1].ToString());
                    string analyte_id = "NH3";
                    double dilution_factor = Convert.ToDouble(worksheet.Rows[rowIdx][3].ToString());
                    string comment = worksheet.Rows[rowIdx][4].ToString();

                    DataRow dr = dt.NewRow();
                    dr[0] = aliquot_id;
                    dr[1] = analyte_id;
                    dr[2] = measured_val;
                    dr[4] = dilution_factor;
                    dr[5] = analysis_datetime;
                    dr[6] = comment;

                    dt.Rows.Add(dr);
                }

                rm.TemplateData = dt;
            }
            catch (Exception ex)
            {
                rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }
            return rm;
        }
    }
}
