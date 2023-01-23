using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PluginBase;


namespace CMTB_TOC
{
    public class CMTB_TOC : DataProcessor
    {

        public override string id { get => "cmtb_toc_version1.0"; }
        public override string name { get => "CMTB_TOC"; }
        public override string description { get => "Processor used for CMTB_TOC translation to universal template"; }
        public override string file_type { get => ".txt"; }
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
                if (!rm.IsValid)
                    return rm;

                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                using (StreamReader sr = new StreamReader(input_file))
                {
                    int idxRow = 0;
                    string line;
                    string[] dataTableColumnNames = null;

                    while ((line = sr.ReadLine()) != null)
                    {
                        current_row = idxRow;
                        // this mapping from Jakob Fox assume the first row is 1
                        // 'Row 11[-1]; Column E through Column J These cells will read “Result( )” The value in the parenthesis will need to be used for the analyte identifier'

                        // store data table column names
                        if (idxRow == 10)
                        {
                            dataTableColumnNames = line.Split("\t");
                            idxRow++;
                            continue;
                        }

                        //Data starts in row 12[-1]
                        if (idxRow < 11)
                        {
                            idxRow++;
                            continue;
                        }

                        //We are finished when we reach and empty row
                        line = line.Trim();
                        if (string.IsNullOrWhiteSpace(line))
                            break;

                        //Parse the string - tab delimited
                        string[] tokens = line.Split("\t");

                        //Aliquot                        
                        string aliquot = tokens[ColumnIndex0.C].Trim();

                        //Analysis DateTime
                        //Date time in column M - e.g. 5/27/2021 14:25
                        DateTime analysisDateTime;
                        if (!DateTime.TryParse(tokens[ColumnIndex0.M].Trim(), out analysisDateTime))
                            throw new Exception(String.Format("File: {0} - Analysis DateTime is not valid. Row {1}", input_file, idxRow));

                        units = tokens[ColumnIndex0.K].Trim();

                        for (int colIndex = ColumnIndex0.E; colIndex <= ColumnIndex0.J; colIndex++)
                        {
                            string analyteId = dataTableColumnNames[colIndex].Split('(', ')')[1];
                            string measuredValTmp = tokens[colIndex].Trim();
                            double measuredVal;
                            // if measuredValTmp = "" measuredVal will be set to 0, but parsed will be false
                            bool parsed = double.TryParse(measuredValTmp, out measuredVal);
                            if (measuredValTmp.Length > 0 && !parsed)
                                throw new Exception("Invalid measured value for " + analyteId + " in row: " + idxRow.ToString());

                            DataRow dr = dt.NewRow();
                            dr["Aliquot"] = aliquot;
                            dr["Analyte Identifier"] = analyteId;
                            dr["Measured Value"] = measuredVal;
                            dr["Units"] = units;
                            dr["Analysis Date/Time"] = analysisDateTime;
                            dt.Rows.Add(dr);

                        }
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
