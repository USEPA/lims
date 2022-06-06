using System;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using ExcelDataReader;

namespace Xcaliber

{

    public class XcaliberProcessor : DataProcessor
    {
        public override string id { get => "Xcaliber1.0"; }
        public override string name { get => "Xcaliber"; }
        public override string description { get => "Processor used for Xcaliber translation to universal template"; }
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
                using (var stream = File.Open(input_file, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();
                        tables = result.Tables;
                    }
                }

                DataTable dt_template = GetDataTable();
                dt_template.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
                TemplateField[] fields = Fields;

                int numTables = tables.Count;
                for (int i =0; i<numTables; i++)
                {
                    var worksheet = tables[i];
                    string analyte_id = worksheet.Rows[2][0].ToString();

                    int numRows = worksheet.Rows.Count;
                    int numCols = worksheet.Columns.Count;

                    //Data row starts on Excel row 6, 5 for zero based
                    for (int row = 5; row < numRows; row++)
                    {
                        string fileName = worksheet.Rows[row][0].ToString().Trim();
                        if (String.IsNullOrWhiteSpace(fileName))
                            break;

                        string aliquot_id = worksheet.Rows[row][2].ToString();                        

                        //Measured val can contain string of 'NA' or 'NF'
                        string measured_val = worksheet.Rows[row][5].ToString().Trim();
                        double d_measured_val;
                        if (!Double.TryParse(measured_val, out d_measured_val))
                            d_measured_val = 0.0;                        


                        double dilution_factor = Convert.ToDouble(worksheet.Rows[row][40].ToString());

                        DateTime analysis_datetime = fi.CreationTime.Date.Add(DateTime.Parse(worksheet.Rows[row][30].ToString()).TimeOfDay);

                        //Area
                        string userDefined1 = worksheet.Rows[row][14].ToString();

                        //ISTD Area
                        string userDefined2 = worksheet.Rows[row][16].ToString();

                        DataRow dr = dt_template.NewRow();
                        dr[0] = aliquot_id;
                        dr[1] = analyte_id;
                        dr[2] = d_measured_val;
                        dr[4] = dilution_factor;
                        dr[5] = analysis_datetime;
                        dr[8] = userDefined1;
                        dr[9] = userDefined2;

                        dt_template.Rows.Add(dr);
                    }
                }
                                                               
                rm.TemplateData = dt_template;
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