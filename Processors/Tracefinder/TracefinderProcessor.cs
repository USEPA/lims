﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using OfficeOpenXml;
using PluginBase;

namespace Tracefinder
{
    class AliquotAnalyte
    {
        public string Aliquot { get; set; }
        public string AnalyteID { get; set; }
        public string MeasuredValue { get; set; }
        public string AnalysisDateTime { get; set; }
        public double DilutionFactor { get; set; }
        public string UserDefined1 { get; set; }

        public AliquotAnalyte(string aliquot, string analyteID, string measuredVal="", string analysisDateTime="", double dilutionFactor = double.NaN, string userDefined1 = "")
        {
            Aliquot = aliquot;
            AnalyteID = analyteID;
            MeasuredValue = measuredVal;
            AnalysisDateTime = analysisDateTime;
            DilutionFactor = dilutionFactor;
            UserDefined1 = userDefined1;
        }
    }
    public class TracefinderProcessor : DataProcessor
    {
        public override string id { get => "tracefinder_version1.0"; }
        public override string name { get => "Tracefinder"; }
        public override string description { get => "Processor used for Tracefinder translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            try
            {
                rm = VerifyInputFile();
                if (!rm.IsValid)
                    return rm;

                DataTable dt = GetDataTable();
                List<AliquotAnalyte> lstAliquotAnalytes = new List<AliquotAnalyte>();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);
                
                
                //Start Sheet 2 -------------------------------------------------------------------------------
                //Data is in the 2nd sheet
                //var worksheet2 = package.Workbook.Worksheets[1];  //Worksheets are zero-based index                
                var worksheet2 = package.Workbook.Worksheets["Sheet2"];
                string name = worksheet2.Name;
                //File validation
                if (worksheet2.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet2 in InputFile:  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }

                int startRow = worksheet2.Dimension.Start.Row;
                int startCol = worksheet2.Dimension.Start.Column;
                int numRows = worksheet2.Dimension.End.Row;
                int numCols = worksheet2.Dimension.End.Column;

                //More file validation
                string sval = GetXLStringValue(worksheet2.Cells[8, 1]);
                if (string.Compare(sval, "Data File", true) != 0)
                {
                    string msg = string.Format("Input file is not in correct format. String 'Data File' missing from cell A8.  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }

                List<string> lstAnalyteIDs = new List<string>();
                List<string> lstAliquotNum = new List<string>();

                //Sheet 2, Row 8, starting at column 2 contains Aliquots 
                for (int col = 2; col <= numCols; col++)
                {
                    string sCell = GetXLStringValue(worksheet2.Cells[8, col]);
                    if (!"All Flags".Equals(sCell, StringComparison.OrdinalIgnoreCase))
                        lstAnalyteIDs.Add(sCell);
                    else
                        break;
                    
                }

                //KW: July 19, 2021
                //"Sample Acquisition Date" - was assumed to be in last column
                //We will search for it instead
                string analyzeDate = "";
                int analyzeDateColNum = -1;
                for (int i = numCols; i > 1; i--)
                {
                    analyzeDate = GetXLStringValue(worksheet2.Cells[8, i]);
                    if (analyzeDate.Equals("Sample Acquisition Date", StringComparison.OrdinalIgnoreCase))
                    {
                        analyzeDateColNum = i;
                        break;
                    }
                }

                if (analyzeDateColNum < 0)
                {
                    string msg = "Sample Acquisition Date not in right column: Row {0}, Column {1}. File: {2}";
                    rm.LogMessage = string.Format(msg, 8, numCols, input_file);
                    rm.ErrorMessage = string.Format(msg, 8, numCols, input_file);
                    return rm;
                }


                // JD: 4/2023
                // The dilution factor will be a column [on] Sheet2 towards the end titled “Dilution Factor”
                string dilutionFactorHeader = "";
                int dilutionFactorColNum = -1;
                for (int i = numCols; i > 1; i--)
                {
                    dilutionFactorHeader = GetXLStringValue(worksheet2.Cells[8, i]);
                    if (dilutionFactorHeader.Equals("Dilution Factor", StringComparison.OrdinalIgnoreCase))
                    {
                        dilutionFactorColNum = i;
                        break;
                    }
                }

                if (dilutionFactorColNum < 0)
                {
                    string msg = "Dilution Factor not found: Row {0}, Column {1}. File: {2}";
                    rm.LogMessage = string.Format(msg, 8, numCols, input_file);
                    rm.ErrorMessage = string.Format(msg, 8, numCols, input_file);
                    return rm;
                }

                int numAnalytes = lstAnalyteIDs.Count;

                //Sheet 2, Row 9 down, Column 1 contains Aliquot name
                //Sheet 2, Row 9 down, Column 2 until 'All Flags' contain data
                for (int row=9;row <= numRows; row++)
                {
                    //Sheet 2, Row 9 down, Column 1 contains Aliquot name
                    string aliquot = GetXLStringValue(worksheet2.Cells[row, 1]);
                                      
                    analyzeDate = GetXLStringValue(worksheet2.Cells[row, analyzeDateColNum]);
                    double dilutionFactor = GetXLDoubleValue(worksheet2.Cells[row, dilutionFactorColNum]);
                    for (int col = 2; col <= numAnalytes + 1; col++)
                    {                        
                        string analyteID = lstAnalyteIDs[col - 2];
                        string measuredVal = GetXLStringValue(worksheet2.Cells[row, col]);
                        AliquotAnalyte al = new AliquotAnalyte(aliquot, analyteID, measuredVal, analyzeDate, dilutionFactor, "");
                        lstAliquotAnalytes.Add(al);
                    }

                }
                //End Sheet 2 -------------------------------------------------------------------------------

                //Start Sheet 4 -----------------------------------------------------------------------------
                //var worksheet4 = package.Workbook.Worksheets[3];  //Worksheets are zero-based index
                var worksheet4 = package.Workbook.Worksheets["Sheet4"];  //Worksheets are zero-based index        
                name = worksheet4.Name;
                //File validation
                if (worksheet4.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet4 in InputFile:  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }
                startRow = worksheet4.Dimension.Start.Row;
                startCol = worksheet4.Dimension.Start.Column;
                numRows = worksheet4.Dimension.End.Row;
                numCols = worksheet4.Dimension.End.Column;

                lstAnalyteIDs = new List<string>();
                //Sheet 4, Row 8, starting at column 2 contains Aliquots 
                for (int col = 2; col <= numCols; col++)
                {
                    string sCell = GetXLStringValue(worksheet4.Cells[8, col]);
                    if (!"All Flags".Equals(sCell, StringComparison.OrdinalIgnoreCase))
                        lstAnalyteIDs.Add(sCell);
                    else
                        break;

                }

                numAnalytes = lstAnalyteIDs.Count;

                //Sheet 4, Row 9 down, Column 1 contains Aliquot name
                //Sheet 4, Row 9 down, Column 2 until 'All Flags' contain data
                for (int row = 9; row <= numRows; row++)
                {
                    //Sheet 2, Row 9 down, Column 1 contains Aliquot name
                    string aliquot = GetXLStringValue(worksheet4.Cells[row, 1]);                    
                    for (int col = 2; col <= numAnalytes + 1; col++)
                    {
                        string analyte = lstAnalyteIDs[col - 2];
                        //Sheet 2, Row 9 down, Column 1 contains Aliquot name
                        aliquot = GetXLStringValue(worksheet4.Cells[row, 1]);
                        //string measuredVal = GetXLStringValue(worksheet4.Cells[row, col]);

                        //Note: Analytes that are only present on sheet 4 will
                        //have no measured value to report so this cell will be blank (e.g. M2-4:2FTS, etc.)

                        //I dont think above statement is true anymore
                        var al = lstAliquotAnalytes.Find(x => x.Aliquot.Equals(aliquot, StringComparison.OrdinalIgnoreCase)
                            && x.AnalyteID.Equals(analyte, StringComparison.OrdinalIgnoreCase));
                        
                        if (al == null)
                        {
                            string userDefined1= GetXLStringValue(worksheet4.Cells[row, col]);
                            AliquotAnalyte al2 = new AliquotAnalyte(aliquot, analyte, "0", "", double.NaN, userDefined1);
                            lstAliquotAnalytes.Add(al2);
                        }
                        else
                        {                            
                            al.UserDefined1 = GetXLStringValue(worksheet4.Cells[row, col]);
                        }                                                                           
                    }                    
                }

                for(int i=0;i<lstAliquotAnalytes.Count;i++)
                {
                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = lstAliquotAnalytes[i].Aliquot;
                    dr["Analyte Identifier"] = lstAliquotAnalytes[i].AnalyteID;
                    dr["Analysis Date/Time"] = lstAliquotAnalytes[i].AnalysisDateTime;

                    if (!string.IsNullOrWhiteSpace(lstAliquotAnalytes[i].MeasuredValue))
                        dr["Measured Value"] = lstAliquotAnalytes[i].MeasuredValue;

                    if (!double.IsNaN(lstAliquotAnalytes[i].DilutionFactor))
                    {
                        dr["Dilution Factor"] = lstAliquotAnalytes[i].DilutionFactor;
                    }

                    dr["User Defined 1"] = lstAliquotAnalytes[i].UserDefined1;

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
