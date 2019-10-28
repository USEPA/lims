using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Qubit2_0
{
    public class Qubit20Processor : Processor, IProcessor
    {

        public string UniqueId { get => "qubit2.0_version1.0"; }

        public string Name { get => "Qubit2.0"; }

        public string Description { get => "Processor used for Qubit2.0 translation to universal template"; }

        public FileTypes InstrumentFileType { get => FileTypes.xlsx; }

        public string InputFile { get; set; }

        public string OutputFile { get; set; }

        public DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();            
            try
            {
                //Verify that the file exists
                if (!File.Exists(InputFile))
                {
                    rm.ErrorMessages.Add(string.Format("Input data file not found: {0}", InputFile));
                    rm.LogMessages.Add(string.Format("Input data file not found: {0}", InputFile));                    
                    return rm;
                }

                //Verify the file type extension is correct
                FileInfo fi = new FileInfo(InputFile);
                string ext = fi.Extension;
                if (string.Compare(ext, "xlsx", StringComparison.OrdinalIgnoreCase) != 0)
                {
                    rm.AddErrorAndLogMessage(string.Format("Input data file not correct file type. Need {0} , found {1}", InputFile, "xlsx"));                    
                    return rm;                    
                }

                using (var package = new ExcelPackage(fi))
                {
                    //Data is in the 2nd sheet
                    var worksheet = package.Workbook.Worksheets[2]; // Tip: To access the first worksheet, try index 1, not 0
                    string name = worksheet.Name;
                    int startRow = worksheet.Dimension.Start.Row;
                    int startCol = worksheet.Dimension.Start.Column;
                    int numRows = worksheet.Dimension.End.Row;
                    int numCols = worksheet.Dimension.End.Column;

                    DataTable dt_template = new DataTable();
                    TemplateField[] fields = Processor.Fields;

                    for (int idx = 0; idx < fields.Length; idx++)
                    {
                        DataColumn dc = new DataColumn(fields[idx].Name, fields[idx].DataType);
                        if (fields[idx].DataType == typeof(string))
                            dc.DefaultValue = "";
                        dt_template.Columns.Add(dc);
                    }

                    for (int row = 2; row <= numRows; row++)
                    {

                        string aliquot_id = GetStringValue(worksheet.Cells[row, 1]);

                        DateTime analysis_datetime = GetDateTimeValue(worksheet.Cells[row, 3]);

                        double measured_val = default;
                        ExcelRange rng_meas_val = worksheet.Cells[row, 4];
                        if (rng_meas_val != null && rng_meas_val.Value != null)
                        {
                            string msr_val = rng_meas_val.Value.ToString().Trim();
                            if (string.Compare(msr_val, "<0.50") == 0)
                                measured_val = default;
                            else
                                measured_val = GetDoubleValue(worksheet.Cells[row, 4]);
                        }


                        string analyte_id = GetStringValue(worksheet.Cells[row, 8]);

                        double dilution_factor = GetDoubleValue(worksheet.Cells[row, 10]);

                        DataRow dr = dt_template.NewRow();
                        dr[0] = aliquot_id;
                        dr[5] = analysis_datetime;
                        dr[2] = measured_val;
                        dr[1] = analyte_id;
                        dr[4] = dilution_factor;

                        dt_template.Rows.Add(dr);
                    }

                    rm.TemplateData = dt_template;
                }
            }
            catch(Exception ex)
            {
                rm.AddErrorAndLogMessage(string.Format("Problem transferring data file {0}  to template file {1}", InputFile, OutputFile));
            }

            return rm;           
        }

        private double GetDoubleValue(ExcelRange cell)
        {
            double retVal = 0.0;
            if (cell == null || cell.Value == null)
                retVal = default;
            else
                retVal = Convert.ToDouble(cell.Value.ToString().Trim());

            return retVal;
        }
        private string GetStringValue(ExcelRange cell)
        {
            string retVal = "";
            if (cell == null || cell.Value == null)
                retVal = default;
            else
                retVal = Convert.ToString(cell.Value.ToString().Trim());

            return retVal;
        }

        private DateTime GetDateTimeValue(ExcelRange cell)
        {
            DateTime retVal = default;
            if (cell == null || cell.Value == null)
                retVal = default;
            else
                retVal = Convert.ToDateTime(cell.Value.ToString().Trim());

            return retVal;
        }
    }
}
