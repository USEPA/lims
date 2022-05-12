using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using PluginBase;


namespace CHL_TOC
{
    public class CHL_TOC : DataProcessor
    {
        public override string id { get => "chi_toc"; }
        public override string name { get => "CHL_TOC"; }
        public override string description { get => "Processor used for CHL_TOC translation to universal template"; }
        public override string file_type { get => ".txt"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            DataTable dt = null;
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                using StreamReader sr = new StreamReader(input_file);

                string? line;
                bool startData = false;
                string analyteID1 = "";
                string analyteID2 = "";
                string analyteID3 = "";

                while ((line = sr.ReadLine()) != null)
                {
                    current_row++;
                    line = line.Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    //Tab delimited file
                    string[] tokens = line.Split('\t');

                    //Date time line looks like this 'Date/Time	10/26/2021 1:00:01 PM'                    
                    if (string.Compare(tokens[0].Trim(), "Date/Time", true) == 0)
                    {
                        //check that the string actually contains date time value
                        if (tokens.Length < 2)
                            throw new Exception("Analysis date time not in file.");

                        analysisDateTime = Convert.ToDateTime(tokens[1].Trim());
                        continue;
                    }

                    //As intitially described, this data file will have 3 analyte ids - columns E, F, G.
                    //They will look like: Result(TOC)	Result(TC)	Result(IC)
                    //Need to extract the value from within parentheses.
                    //Data starts in row after 'Type' in first column
                    if (string.Compare(tokens[0].Trim(), "Type", true) == 0)
                    {
                        startData = true;

                        analyteID1 = tokens[ColumnIndex0.E].Trim().Split('(', ')')[1];
                        analyteID2 = tokens[ColumnIndex0.F].Trim().Split('(', ')')[1];
                        analyteID3 = tokens[ColumnIndex0.G].Trim().Split('(', ')')[1];
                        continue;
                    }

                    if (!startData)
                        continue;

                    aliquot = tokens[ColumnIndex0.C].Trim();

                    //Will change this to a loop if we get an inderminate number of analytes
                    string mval = tokens[ColumnIndex0.E].Trim();
                    if (double.TryParse(mval, out measuredVal))
                    {
                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Analyte Identifier"] = analyteID1;
                        dr["Measured Value"] = measuredVal;
                        dt.Rows.Add(dr);
                    }

                    mval = tokens[ColumnIndex0.F].Trim();
                    if (double.TryParse(mval, out measuredVal))
                    {
                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Analyte Identifier"] = analyteID2;
                        dr["Measured Value"] = measuredVal;
                        dt.Rows.Add(dr);
                    }

                    mval = tokens[ColumnIndex0.F].Trim();
                    if (double.TryParse(mval, out measuredVal))
                    {
                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Analyte Identifier"] = analyteID3;
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