using System;
using System.Data;
using System.IO;
using System.Linq;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;

namespace EGB_YSI6600
{
    public class EGB_YSI6600 : DataProcessor
    {
        public override string id { get => "egb_ysi66001.1"; }
        public override string name { get => "EGB_YSI6600"; }
        public override string description { get => "Processor used for EGB_YSI6600 translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.1"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            DataTable dt = null;
            try
            {
                rm = VerifyInputFile();
                if (rm.IsValid == false)
                    return rm;

                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                //Will this thro
                var worksheet = package.Workbook.Worksheets["SondeDiscreteData"];  //Worksheets are zero-based index
                if (worksheet == null)
                    throw new Exception($"Unable to find worksheet: SondeDiscreteData");

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
                                
                List<string> lstAnalyteIDs = new List<string>();
                for (int i = ColumnIndex1.K; i <= ColumnIndex1.U; i++)                
                    lstAnalyteIDs.Add(GetXLStringValue(worksheet.Cells[2, i]));
                
                //string analyteID_K = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.K]));
                //string analyteID_L = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.L]));
                //string analyteID_M = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.M]));
                //string analyteID_N = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.N]));
                //string analyteID_O = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.O]));
                //string analyteID_P = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.P]));
                //string analyteID_Q = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.Q]));
                //string analyteID_R = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.R]));
                //string analyteID_S = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.S]));
                //string analyteID_T = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.T]));
                //string analyteID_U = Convert.ToString(GetXLDoubleValue(worksheet.Cells[2, ColumnIndex1.U]));
                
                Dictionary<string, AliquotData> dctAliquots = new Dictionary<string, AliquotData>();
                Dictionary<string, int> dctCount = new Dictionary<string, int>();
                string currentAliquot = "";
                AliquotData aliquot = null;
                for (int rowIdx = 3; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    //If this value is 'QAC' then skip it
                    string sID = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.D]);
                    if (string.Compare("QAC", sID, StringComparison.OrdinalIgnoreCase) == 0)
                        continue;

                    currentAliquot = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.A]);
                    //Add this aliquot if its the first time we've seen it
                    if (!dctAliquots.ContainsKey(currentAliquot))
                    {
                                                
                        aliquot = new AliquotData(currentAliquot);
                        dctAliquots.Add(currentAliquot, aliquot);
                        string sDate = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.B]);
                        string sTime = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.C]);
                        DateTime date = Convert.ToDateTime(sDate);
                        DateTime time = Convert.ToDateTime(sTime);
                        DateTime date_time = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
                        
                        //if (!DateTime.TryParse(sDate + " " + sTime, out analysisDateTime))
                        //    throw new Exception($"Invalid Analysis DateTime: {sDate} {sTime}");
                        aliquot.AnalysisDateTime = date_time;

                        userDefined1 = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.X]);
                        aliquot.UserDefined1 = userDefined1;
                    }
                    else
                        aliquot = dctAliquots[currentAliquot];                                       

                    //Keep track of how many aliquots need to be averaged
                    aliquot.Count += 1;
                    int lstIdx = 0;
                    for (int colIdx = ColumnIndex1.K; colIdx <= ColumnIndex1.U; colIdx++)
                    {
                        double dval = GetXLDoubleValue(worksheet.Cells[rowIdx, colIdx]);
                        aliquot.MeasuredValues[lstIdx] += dval;
                        lstIdx++;
                    }                   
                }
                //We have all the data. Calculate the averages and build datatable
                foreach (AliquotData alqt in dctAliquots.Values)
                {
                    for (int analyteIdx = 0; analyteIdx < alqt.MeasuredValues.Count; analyteIdx++)
                    {
                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = alqt.Aliquot;
                        dr["Analyte Identifier"] = lstAnalyteIDs[analyteIdx];
                        dr["Measured Value"] = alqt.MeasuredValues.Sum() / (double)alqt.Count;
                        dr["Analysis Date/Time"] = alqt.AnalysisDateTime;
                        dr["User Defined 1"] = alqt.UserDefined1;

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
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row);
                rm.ErrorMessage = errorMsg;
            }

            rm.TemplateData = dt;

            return rm;

        }
    }

    public class AliquotData
    {
        public string Aliquot { get; set; }
        public int Count { get; set; }
        public DateTime AnalysisDateTime { get; set; }
        public List<double> MeasuredValues { get; set; }
        public string UserDefined1 { get; set; }



        public AliquotData(string aliquot)
        {
            Aliquot = aliquot;
            Count = 0;
            AnalysisDateTime = DateTime.MinValue;
            MeasuredValues = new List<double>(new double[11]);
        }
    }
}