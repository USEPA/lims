using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using OfficeOpenXml;
using PluginBase;

namespace Tracefinder
{
    public class TracefinderProcessor : Processor
    {
        public override string UniqueId { get => "tracefinder_version1.0"; }
        public override string Name { get => "Tracefinder"; }
        public override string Description { get => "Processor used for Tracefinder translation to universal template"; }
        public override string InstrumentFileType { get => ".xlsx"; }
        public override string InputFile { get; set; }
        public override string Path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                DataTable dt = GetDataTable();
                FileInfo fi = new FileInfo(InputFile);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);
                
                
                //Start Sheet 2 -------------------------------------------------------------------------------
                //Data is in the 2nd sheet
                var worksheet2 = package.Workbook.Worksheets[1];  //Worksheets are zero-based index                
                string name = worksheet2.Name;
                //File validation
                if (worksheet2.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet2 in InputFile:  {0}", InputFile);
                    rm.AddErrorAndLogMessage(msg);
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
                    string msg = string.Format("Input file is not in correct format. String 'Data File' missing from cell A8.  {0}", InputFile);
                    rm.AddErrorAndLogMessage(msg);
                    return rm;
                }

                List<string> lstAnalyteIDs = new List<string>();
                //Sheet 2, Row 8, starting at column 2 contains Aliquots 
                for (int col = 2; col <= numCols; col++)
                {
                    string sCell = GetXLStringValue(worksheet2.Cells[8, col]);
                    if (!"All Flags".Equals(sCell, StringComparison.OrdinalIgnoreCase))
                        lstAnalyteIDs.Add(sCell);
                    else
                        break;
                    
                }

                string analyzeDate = GetXLStringValue(worksheet2.Cells[8, numCols]);
                if (!analyzeDate.Equals("Sample Acquisition Date", StringComparison.OrdinalIgnoreCase))
                {
                    string msg = "Sample Acquisition Date not in right column: Row {0}, Column {1}. File: {2}";
                    rm.AddErrorAndLogMessage(String.Format(msg, 8, numCols, InputFile));
                    return rm;   
                }

                int numAliquots = lstAnalyteIDs.Count;

                //Sheet 2, Row 9 down, Column 1 contains Aliquot name
                //Sheet 2, Row 9 down, Column 2 until 'All Flags' contain data
                for (int row=9;row <= numRows; row++)
                {
                    //Sheet 2, Row 9 down, Column 1 contains Aliquot name
                    string aliquot = GetXLStringValue(worksheet2.Cells[row, 1]);
                    analyzeDate = GetXLStringValue(worksheet2.Cells[row, numCols]);
                    for (int col = 2; col < numAliquots; col++)
                    {
                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = lstAnalyteIDs[col - 2];
                        dr["Analysis Date/Time"] = analyzeDate;
                        dr["Measured Value"] = GetXLStringValue(worksheet2.Cells[row, col]);
                        dt.Rows.Add(dr);
                    }

                }
                //End Sheet 2 -------------------------------------------------------------------------------

                //Start Sheet 4 -----------------------------------------------------------------------------
                var worksheet4 = package.Workbook.Worksheets[3];  //Worksheets are zero-based index        
                name = worksheet4.Name;
                //File validation
                if (worksheet4.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet4 in InputFile:  {0}", InputFile);
                    rm.AddErrorAndLogMessage(msg);
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

                numAliquots = lstAnalyteIDs.Count;

                //Sheet 4, Row 9 down, Column 1 contains Aliquot name
                //Sheet 4, Row 9 down, Column 2 until 'All Flags' contain data
                for (int row = 9; row <= numRows; row++)
                {
                    //Sheet 2, Row 9 down, Column 1 contains Aliquot name
                    string aliquot = GetXLStringValue(worksheet4.Cells[row, 1]);
                    //analyzeDate = worksheet4.Cells[row, numCols].Value.ToString().Trim();
                    for (int col = 2; col < numAliquots; col++)
                    {
                        string analyte = lstAnalyteIDs[col - 2];                        
                        string[] keys = new string[2];
                        keys[0] = aliquot;
                        keys[1] = analyte;
                        DataRow drow = dt.Rows.Find(keys);
                        //DataRow[] rows = dt.Select(qry);
                        if (drow == null)
                        {
                            DataRow dr = dt.NewRow();
                            dr["Aliquot"] = aliquot;
                            dr["Analyte Identifier"] = analyte;
                            dr["User Defined 1"] = GetXLStringValue(worksheet4.Cells[row, col]);
                            dt.Rows.Add(dr);
                        }
                        else
                        {
                            drow["User Defined 1"] = GetXLStringValue(worksheet4.Cells[row, col]);
                        }                                                                           
                    }                    
                }

                rm.TemplateData = dt;

            }
            catch (Exception ex)
            {
                rm.AddErrorAndLogMessage(string.Format("Error processing input file: {0}", InputFile));
            }
            return rm;
        }
    }
}
