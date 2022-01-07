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

                DataTable dtReturn = GetDataTable();
                dtReturn.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
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
                for (int rowIdx = 6; rowIdx < numRows; rowIdx++)
                {
                    string numID = worksheet.Rows[rowIdx][0].ToString().Trim();
                    if (string.IsNullOrEmpty(numID))
                        continue;

                    //Aliquot value is in Column B (col 1 in zero based datatable)
                    string aliquot = worksheet.Rows[rowIdx][1].ToString().Trim();

                                        
                    
                    for (int colIdx=0; colIdx<(lstAnalyteIDs.Count); colIdx++)
                    {                        
                        DataRow dr = dtReturn.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = lstAnalyteIDs[colIdx];

                        double measuredVal;
                        //Remeber data starts in column C (col 2 in zero based datatable thus the +2)
                        string valTmp = worksheet.Rows[rowIdx][colIdx+2].ToString().Trim();
                        if (string.Compare(valTmp, "n.a.", true) == 0)
                            measuredVal = 0.0;
                        else if (!double.TryParse(valTmp, out measuredVal))
                            measuredVal = 0.0;

                        dr["Measured Value"] = measuredVal;

                        dtReturn.Rows.Add(dr);
                    }
                }

                rm.TemplateData = dtReturn;
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
