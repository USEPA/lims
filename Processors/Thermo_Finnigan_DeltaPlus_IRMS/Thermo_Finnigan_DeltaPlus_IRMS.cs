using System;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using ExcelDataReader;

namespace Thermo_Finnigan_DeltaPlus_IRMS
{
    public class Thermo_Finnigan_DeltaPlus_IRMS : DataProcessor
    {
        public override string id { get => "thermo_finnigan_deltaplus_irms"; }
        public override string name { get => "Thermo_Finnigan_DeltaPlus_IRMS"; }
        public override string description { get => "Processor used for Thermo Finnigan DeltaPlus IRMS translation to universal template"; }
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

                for (int rowIdx = 1; rowIdx < numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    //Aliquot is same for all rows
                    aliquot = worksheet.Rows[rowIdx][ColumnIndex0.B].ToString();

                    //DateTime is same for all rows
                    string tmpDate = worksheet.Rows[rowIdx][ColumnIndex0.C].ToString().Trim();
                    string tmpTime = worksheet.Rows[rowIdx][ColumnIndex0.D].ToString().Trim();
                    if (!DateTime.TryParse(tmpDate + " " + tmpTime, out analysisDateTime))
                        throw new Exception("Invalid DateTime value - " + tmpDate + " " + tmpTime);

                    string tmpMeasuredVal = "";
                    DataRow dr;

                    //Each row will have 4 measured values 
                    //Data will be in H, J, W, AC

                    //Handle analytes for column H
                    //There are two measured values for each aliquot. Only need one
                    if (rowIdx % 2 == 1)
                    {
                        //Same for all rows
                        analyteID = worksheet.Rows[0][ColumnIndex0.H].ToString().Trim();

                        tmpMeasuredVal = worksheet.Rows[rowIdx][ColumnIndex0.H].ToString();
                        if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                            throw new Exception("Unable to parse measured value for column H: " + tmpMeasuredVal);

                        dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Measured Value"] = measuredVal;

                        dt.Rows.Add(dr);
                    }

                    //The rest are dependent on value in column F.
                    string colF = worksheet.Rows[rowIdx][ColumnIndex0.F].ToString();

                    bool bN2 = false;
                    bool bCO2 = false;

                    //Handle analytes for column J      
                    if (string.Compare(colF, "N2", true) == 0)
                    {
                        analyteID = "N Area";
                        bN2 = true;
                    }
                    else if (string.Compare(colF, "CO2", true) == 0)
                    {
                        analyteID = "C Area";
                        bCO2 = true;
                    }

                    tmpMeasuredVal = worksheet.Rows[rowIdx][ColumnIndex0.J].ToString().Trim();
                    if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                        throw new Exception("Unable to parse measured value for column J: " + tmpMeasuredVal);

                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dt.Rows.Add(dr);
                    //End handle analytes for column J


                    //Handle analytes for column W
                    if (bCO2)
                    {
                        analyteID = "Raw 13C";
                        tmpMeasuredVal = worksheet.Rows[rowIdx][ColumnIndex0.W].ToString().Trim();
                        if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                            throw new Exception("Unable to parse measured value for column W: " + tmpMeasuredVal);

                        dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Measured Value"] = measuredVal;
                        dt.Rows.Add(dr);

                    }

                    //Handle analytes for column W
                    if (bN2)
                    {
                        analyteID = "Raw 15N";
                        tmpMeasuredVal = worksheet.Rows[rowIdx][ColumnIndex0.AC].ToString().Trim();
                        if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                            throw new Exception("Unable to parse measured value for column AC: " + tmpMeasuredVal);

                        dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Measured Value"] = measuredVal;
                        dt.Rows.Add(dr);

                    }

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