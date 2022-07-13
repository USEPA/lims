using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using OfficeOpenXml;
using PluginBase;

namespace EGB_OTU
{
    public class EGB_OTU : DataProcessor
    {
        public override string id { get => "egb_otu"; }
        public override string name { get => "EGB_OTU"; }
        public override string description { get => "Processor used for EGB_OTU translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0.0"; }
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
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                //Data is in the 1st sheet
                var worksheet = package.Workbook.Worksheets[0]; //Worksheets are zero-based index
                foreach( var ws in package.Workbook.Worksheets )
                {
                    if (string.Compare("sheet1", ws.Name, true) == 0)
                    {
                        worksheet = ws;
                        break;
                    }
                }
                //Rows and Columns are one-based
                string name = worksheet.Name;
                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                //Do on column at a time
                for (int idxCol=ColumnIndex1.C;idxCol<numCols;idxCol++)
                {
                    aliquot = GetXLStringValue(worksheet.Cells[2, idxCol]);
                    for (int idxRow = 3; idxRow < numRows; idxRow++)
                    {
                        analyteID = GetXLStringValue(worksheet.Cells[idxRow, ColumnIndex1.A]);
                        userDefined1 = GetXLStringValue(worksheet.Cells[idxRow, ColumnIndex1.B]);
                        string tmpMeasuredVal = GetXLStringValue(worksheet.Cells[idxRow, idxCol]);
                        if (string.IsNullOrWhiteSpace(tmpMeasuredVal))
                            measuredVal = 0.0;
                        else if (!Double.TryParse(tmpMeasuredVal, out measuredVal))
                            measuredVal = 0.0;

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;                        
                        dr["User Defined 1"] = userDefined1;

                        dt.Rows.Add(dr);
                    }
                }
                rm.TemplateData = dt.Copy();

            }
            catch (Exception ex)
            {
                //rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                string errorMsg = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + ex.Message;
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row);
                rm.ErrorMessage = errorMsg;
                //rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }

            return rm;

        }
    }


        }