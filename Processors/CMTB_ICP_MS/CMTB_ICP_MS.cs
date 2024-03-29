﻿using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;


namespace CMTB_ICP_MS
{
    public class CMTB_ICP_MS : DataProcessor
    {
        public override string id { get => "cmtb_icp_ms1.0"; }
        public override string name { get => "CMTB_ICP_MS"; }
        public override string description { get => "Processor used for CMTB ICP-MS translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
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
                if (rm.IsValid == false)
                    return rm;

                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                var worksheet = package.Workbook.Worksheets[0];  //Worksheets are zero-based index
                string name = worksheet.Name;

                //File validation
                if (worksheet.Dimension == null)
                {
                    string msg = string.Format("No data in first sheet in InputFile:  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }



                //Rows start at 1 not 0
                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                List<string> lstAnalyteIDs = new List<string>();
                for (int colIdx= ColumnIndex1.C; colIdx <= ColumnIndex1.H; colIdx++) 
                {
                    string temp = GetXLStringValue(worksheet.Cells[3, colIdx]);
                    temp = GetAnalyteID(temp);
                    lstAnalyteIDs.Add(temp);
                }
                
               
                for (int rowIdx = 5; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    aliquot = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.A]);
                    string tempAnalysisDateTime = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.I]);
                    tempAnalysisDateTime = tempAnalysisDateTime.Trim();
                    analysisDateTime = DateTime.Parse(tempAnalysisDateTime);
                    int analyteIDIdx = 0;
                    for (int colIdx = ColumnIndex1.C; colIdx <= ColumnIndex1.H; colIdx++)
                    {
                        analyteID = lstAnalyteIDs[analyteIDIdx];
                        analyteIDIdx++;                        
                        measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, colIdx]);
                        
                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;
                        dr["Analysis Date/Time"] = analysisDateTime;
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

        //Put this in to trim numeric characters and remove (KED) from analyte id
        //e.g. 75As (KED) - should return As
        //e.g. 89Y (KED)  - should return Y      
        private string GetAnalyteID(string input)
        {
            string output = "";
            string retVal = "";

            int idx = input.IndexOf("(KED");
            if (idx > 0)           
                output = input.Substring(0, idx);
            else
                output = input;

            foreach (char c in output)
            {
                if (!char.IsDigit(c))
                    retVal += c;
            }

            return retVal.Trim();
        }
    }
 }