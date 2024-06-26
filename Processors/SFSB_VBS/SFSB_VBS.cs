﻿using System.Data;
using System.IO;
using System;
using System.Collections.Generic;
using PluginBase;
using OfficeOpenXml;

namespace SFSB_VBS
{
    public class SFSB_VBS : DataProcessor
    {
        public override string id { get => "sfsb_vbs.0"; }
        public override string name { get => "SFSB_VBS"; }
        public override string description { get => "Processor used for SFSB_VBS translation to universal template"; }
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
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                var worksheet = package.Workbook.Worksheets["Binmass"];  //Worksheets are zero-based index                
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

                for (int rowIdx = 2; rowIdx <= numRows; rowIdx++)
                {
                    current_row = rowIdx;
                    analyteID = GetXLStringValue(worksheet.Cells[rowIdx, ColumnIndex1.A]);                    

                    for (int colIdx = ColumnIndex1.B; colIdx <= numCols; colIdx++)
                    {
                        aliquot = GetXLStringValue(worksheet.Cells[1, colIdx]);
                        if (string.IsNullOrWhiteSpace(analyteID))
                            break;

                        measuredVal = GetXLDoubleValue(worksheet.Cells[rowIdx, colIdx]);

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;                        
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
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row);
                rm.ErrorMessage = errorMsg;
            }

            rm.TemplateData = dt;

            return rm;

        }
    }
}