using System;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using ExcelDataReader;

namespace QuantStudio_qPCR
{
    public class QuantStudio_qPCR : DataProcessor
    {
        public override string id { get => "quantstudio_5_0"; }
        public override string name { get => "QuantStudio_5_0"; }
        public override string description { get => "Processor used for QuantStudio 5.0 translation to universal template"; }
        public override string file_type { get => ".xls"; }
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

                //var worksheet = tables[0];
                var worksheet = tables["Results"];
                int numRows = worksheet.Rows.Count;
                int numCols = worksheet.Columns.Count;

                //Unfortunately datetime value has format like this: "2021-12-30 09:56:22 AM EST"
                //The EST at end is not a standard. Going to drop it and then convert
                string tmpDateTime = worksheet.Rows[33][ColumnIndex0.B].ToString().Trim();
                int idx = tmpDateTime.LastIndexOf(" ");
                tmpDateTime = tmpDateTime.Substring(0, idx);
                if (!DateTime.TryParse(tmpDateTime, out analysisDateTime))
                    throw new Exception("Unable to parse datetime value- " + tmpDateTime);

                //If the aliquot name contains 'Pos Ctrl' then all those corresponding UserDefined1 values (column AF)
                //get averaged into a single value
                double dPosCtrl = 0.0;
                int numPosCtrl = 0;
                string posCtrlAliquot = "";
                string posCtrlAnalyteId = "";

                DataRow dr = null;
                for (int row = 48; row < numRows; row++)
                {
                    current_row = row;
                    string tmpMeasuredVal = "";
                    
                    aliquot = worksheet.Rows[row][ColumnIndex0.D].ToString().Trim();
                    analyteID = worksheet.Rows[row][ColumnIndex0.E].ToString().Trim();
                    userDefined1 = worksheet.Rows[row][ColumnIndex0.AF].ToString().Trim();
                    userDefined2 = worksheet.Rows[row][ColumnIndex0.I].ToString().Trim();

                    if (aliquot.Contains("Pos Ctrl",StringComparison.CurrentCultureIgnoreCase))
                    {                        
                        double tmpdPosCtrl;
                        string colAF = worksheet.Rows[row][ColumnIndex0.AF].ToString().Trim();
                        if (!Double.TryParse(colAF, out tmpdPosCtrl))
                            throw new Exception("Unable to parse User Defined1 value for column AF: " + aliquot);
                        dPosCtrl += tmpdPosCtrl;
                        numPosCtrl++;
                        posCtrlAliquot = aliquot;
                        posCtrlAnalyteId = analyteID;
                        continue;
                    }

                    

                    tmpMeasuredVal = worksheet.Rows[row][ColumnIndex0.L].ToString().Trim();
                    if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                        measuredVal = 0.0;

                    userDefined1 = worksheet.Rows[row][ColumnIndex0.AF].ToString().Trim();
                    userDefined2 = worksheet.Rows[row][ColumnIndex0.J].ToString().Trim();

                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dr["User Defined 1"] = userDefined1;
                    dr["User Defined 2"] = userDefined2;
                    dt.Rows.Add(dr);                    
                }

                dr = dt.NewRow();
                dr["Aliquot"] = posCtrlAliquot;
                dr["Analyte Identifier"] = posCtrlAnalyteId;
                dr["Analysis Date/Time"] = analysisDateTime;
                dr["Measured Value"] = (dPosCtrl / numPosCtrl).ToString();
                
                dt.Rows.Add(dr);

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