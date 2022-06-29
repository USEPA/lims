using System;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using ExcelDataReader;

namespace CE_Instruments_NC2500_Elemental_Analyzer
{
    public class CE_Instruments_NC2500_Elemental_Analyzer : DataProcessor
    {
        public override string id { get => "ce_instruments_nc2500_elemental_analyzer"; }
        public override string name { get => "CE_Instruments_NC2500_Elemental_Analyzer"; }
        public override string description { get => "Processor used for CE Instruments NC2500 Elemental Analyzer translation to universal template"; }
        public override string file_type { get => ".xlt"; }
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
                if (!rm.IsValid)
                    return rm;

                FileInfo fi = new FileInfo(input_file);

                DataTableCollection tables;
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = File.Open(input_file, FileMode.Open, FileAccess.Read);

                using var reader = ExcelReaderFactory.CreateReader(stream);
                var result = reader.AsDataSet();
                tables = result.Tables;


                DataTable dt = GetDataTable();
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
                TemplateField[] fields = Fields;

                var worksheet = tables[0];
                int numRows = worksheet.Rows.Count;
                int numCols = worksheet.Columns.Count;

                //There are three measured values per row in columns D, F and L
                //Data starts are row 4 in file but we've import into a datatable which has 0 based rows
                for (int row = 3; row < numRows; row++)
                {
                    string tmpMeasuredVal = "";
                    DataRow dr = null;
                    aliquot = worksheet.Rows[row][ColumnIndex0.A].ToString().Trim();
                    

                    //Data for column D
                    analyteID = "Amount";
                    tmpMeasuredVal = worksheet.Rows[row][ColumnIndex0.D].ToString().Trim();
                    if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                        throw new Exception("Unable to parse measured value for column D: " + tmpMeasuredVal);

                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;                    
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);

                    //Data for column F
                    analyteID = "N Area";
                    tmpMeasuredVal = worksheet.Rows[row][ColumnIndex0.F].ToString().Trim();
                    if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                        throw new Exception("Unable to parse measured value for column F: " + tmpMeasuredVal);

                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);

                    //Data for column L
                    analyteID = "C Area";
                    tmpMeasuredVal = worksheet.Rows[row][ColumnIndex0.L].ToString().Trim();
                    if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                        throw new Exception("Unable to parse measured value for column L: " + tmpMeasuredVal);

                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);
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