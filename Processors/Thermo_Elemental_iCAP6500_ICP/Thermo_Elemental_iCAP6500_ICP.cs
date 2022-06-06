using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using PluginBase;
using ExcelDataReader;

namespace Thermo_Elemental_iCAP6500_ICP
{
    public class Thermo_Elemental_iCAP6500_ICP : DataProcessor
    {
        public override string id { get => "thermo_elemental_icap6500_icp"; }
        public override string name { get => "Thermo_Elemental_iCAP6500_ICP"; }
        public override string description { get => "Processor used for Thermo_Elemental_iCAP6500_ICP translation to universal template"; }
        public override string file_type { get => ".xls"; }
        public override string version { get => "1.0"; }
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

                DataTableCollection tables;
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = File.Open(input_file, FileMode.Open, FileAccess.Read);

                using var reader = ExcelReaderFactory.CreateReader(stream);                 
                var result = reader.AsDataSet();
                tables = result.Tables;
                if (tables == null || tables.Count < 1)
                    throw new Exception("No worksheets found in file");

                DataTable worksheet = tables[0];

                for (int rowIdx=0; rowIdx<worksheet.Rows.Count; rowIdx++)
                {
                    current_row = rowIdx;
                    if (rowIdx < 6)
                        continue;

                    aliquot = worksheet.Rows[rowIdx][ColumnIndex0.B].ToString();
                    string? dateTime = worksheet.Rows[rowIdx][ColumnIndex0.A].ToString();
                    analysisDateTime = Convert.ToDateTime(dateTime);

                    for (int colIdx=ColumnIndex0.E; colIdx<worksheet.Columns.Count; colIdx++)
                    {
                        analyteID = worksheet.Rows[4][colIdx].ToString();
                        string mval = worksheet.Rows[rowIdx][colIdx].ToString().Trim();
                        //Some data looks like 'F .050'
                        if (!Double.TryParse(mval, out measuredVal))
                        {
                            string[] tokens = mval.Split(" ");
                            if (tokens.Length > 1)
                            {
                                if (!Double.TryParse(tokens[1], out measuredVal))
                                    measuredVal = 0.0;
                            }
                            else
                                measuredVal = 0.0;                         
                        }
                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;

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
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row + 1);
                rm.ErrorMessage = errorMsg;
            }

            rm.TemplateData = dt;
            return rm;


        }
    }
}