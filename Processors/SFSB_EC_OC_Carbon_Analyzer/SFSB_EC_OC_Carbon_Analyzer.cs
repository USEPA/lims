﻿using System;
using PluginBase;
using System.Reflection;
using System.IO;
using System.Data;
using System.Globalization;

namespace SFSB_EC_OC_Carbon_Analyzer
{
    public class SFSB_EC_OC_Carbon_Analyzer : DataProcessor
    {
        public override string id { get => "sfsb_ec_oc_carbon_analyzer.0"; }
        public override string name { get => "SFSB_EC_OC_Carbon_Analyzer"; }
        public override string description { get => "Processor used for SFSB EC OC Carbon Analyzer translation to universal template"; }
        public override string file_type { get => ".csv"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public SFSB_EC_OC_Carbon_Analyzer()
        {
        }

        //There are possible multiple blocks of data in the CSV file
        //Block starts with 'Sample ID' in column A
        //Block ends with blank row

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            int idxRow = 0;
            try
            {
                rm = VerifyInputFile();
                if (!rm.IsValid)
                    return rm;

                DataTable dt = GetDataTable();
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(input_file);

                using StreamReader sr = new StreamReader(input_file);                

                string line;
                string[] colNames;
                bool bStartDataBlock = false;
                string sampleID = "Sample ID";

                string analysisDate = "";
                string analysisTime = "";
                current_row = 0;
                DataRow dr = null;
                while ((line = sr.ReadLine()) != null)
                {                    
                    current_row++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        bStartDataBlock = false;
                        analysisDate = "";
                        analysisTime = "";
                        continue;
                    }
                    
                    string[] tokens = line.Split(",");
                    if (sampleID.Equals(tokens[0],StringComparison.OrdinalIgnoreCase))
                    {
                        bStartDataBlock = true;                        
                        continue;
                    }

                    if (!bStartDataBlock)
                        continue;

                    analysisDate = tokens[ColumnIndex0.AA];
                    analysisTime = tokens[ColumnIndex0.AB];
                    //Used for block of data
                    analysisDateTime = DateTime.Parse(analysisDate + " " + analysisTime);

                    //Used for records in this row
                    aliquot = tokens[0];

                    //Individual measurement
                    analyteID = "OC";
                    measuredVal = GetStringDoubleValue(tokens[ColumnIndex0.C]);                                       
                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Analysis Date/Time"] = analysisDateTime;                    
                    dt.Rows.Add(dr);

                    analyteID = "EC";
                    measuredVal = GetStringDoubleValue(tokens[ColumnIndex0.E]);
                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dt.Rows.Add(dr);

                    analyteID = "TC";
                    measuredVal = GetStringDoubleValue(tokens[ColumnIndex0.I]);
                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dt.Rows.Add(dr);

                    analyteID = "EC/TC";
                    measuredVal = GetStringDoubleValue(tokens[ColumnIndex0.K]);
                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dt.Rows.Add(dr);

                    analyteID = "Punch Area";
                    measuredVal = GetStringDoubleValue(tokens[ColumnIndex0.AD]);
                    dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dt.Rows.Add(dr);
                }
                

                rm.TemplateData = dt;

            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + ex.Message;
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row);
                rm.ErrorMessage = errorMsg;
                //rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                //rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }
            return rm;
        }
    }
}