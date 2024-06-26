﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PluginBase;
//using OfficeOpenXml;


namespace ACESD_DOC
{
    public class ACESD_DOC : DataProcessor
    {

        public override string id { get => "acesd_doc_version1.0"; }
        public override string name { get => "ACESD_DOC"; }
        public override string description { get => "Processor used for ACESD DOC translation to universal template"; }
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

                //KW - Jan 20, 2022
                //Data file changed to tab delimited - no longer Excel

                using (StreamReader sr = new StreamReader(input_file))
                {
                    int idxRow = 0;
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        current_row = idxRow;
                        //Data starts in row 12
                        if (idxRow < 12)
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

                        //Analyte ID
                        string analyteTmp = tokens[ColumnIndex0.B].Trim();
                        string analyteID = "";
                        if (string.Compare(analyteTmp, "IC", true) == 0)
                            analyteID = "DIC";
                        else if (string.Compare(analyteTmp, "NPOC", true) == 0)
                            analyteID = "DOC";
                        else //Analyte ID in file is not IC or NPOC
                            throw new Exception(String.Format("File: {0} - Analyte ID is not IC or NPOC. column B, row {1}: {2}", input_file, idxRow, analyteTmp));

                        //Aliquot                        
                        string aliquot = tokens[ColumnIndex0.C].Trim();

                        //Analysis DateTime
                        //Date time in column M - e.g. 5/27/2021 14:25
                        DateTime analysisDateTime;
                        if (!DateTime.TryParse(tokens[ColumnIndex0.M].Trim(), out analysisDateTime))
                            throw new Exception(String.Format("File: {0} - Analysis DateTime is not valid. Row {1}", input_file, idxRow));

                        //Maps to column N
                        string measuredValTmp = tokens[ColumnIndex0.I].Trim();
                        double measuredVal;
                        if (!double.TryParse(measuredValTmp, out measuredVal))
                            // jd 08/22/2023 - per Richard Osborne:
                            // update to skip lines that have invalid values

                            // throw new Exception("Invalid measured value in row: " + idxRow.ToString());
                            continue;

                        units = tokens[ColumnIndex0.K].Trim();

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;
                        dr["Units"] = units;
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
    }
}
