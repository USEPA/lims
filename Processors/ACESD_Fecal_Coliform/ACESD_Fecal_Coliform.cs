using System;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using PluginBase;
using System.Globalization;

namespace ACESD_Fecal_Coliform
{
    public class ACESD_Fecal_Coliform : DataProcessor
    {
        public override string id { get => "acesd_fecal_coliform1.0"; }
        public override string name { get => "ACESD_Fecal_Coliform"; }
        public override string description { get => "Processor used for ACESD_Fecal_Coliform translation to universal template"; }
        public override string file_type { get => ".pdf"; }
        public override string version { get => "1.0"; }
        public override string? input_file { get; set; }
        public override string? path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            DataTable? dt = null;
            try
            {
                rm = VerifyInputFile();
                if (!rm.IsValid)
                    return rm;

                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                using StreamReader sr = new StreamReader(input_file);
                string? line;

                string[] tokens;
                int rowIdx = 0;
                bool bDataFile = false;
                bool bQuantTime = false;
                Match match;
                while ((line = sr.ReadLine()) != null)
                {
                    rowIdx++;
                    current_row = rowIdx;
                    string currentLine = line.Trim();
                    if (!bDataFile)
                    {
                        //e.g.  Data	File	:	062521_10.D
                        match = Regex.Match(line, @"Data\s{1,}File");
                        if (match.Success)
                        {
                            tokens = currentLine.Split(":");
                            aliquot = tokens[1].Trim();
                            bDataFile = true;
                            continue;
                        }
                    }

                    if (!bQuantTime)
                    {
                        //e.g.  Quant Time: Oct 15 10:00:08 2021                        
                        match = Regex.Match(line, @"Quant\s{1,}Time");
                        if (match.Success)
                        {
                            int idx = line.IndexOf(":");
                            if (idx != -1)
                            {
                                string tmpDT = line.Substring(idx + 1).Trim();
                                tmpDT = tmpDT.Replace("\t", " ");
                                string pattern = "MMM dd HH:mm:ss yyyy";
                                if (!DateTime.TryParseExact(tmpDT, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out analysisDateTime))
                                    throw new Exception("Invalid format for Quant Time");
                                bQuantTime = true;
                            }
                            continue;
                        }
                    }

                    if (!bDataFile || !bQuantTime)
                        continue;

                    //This regular expression will match any number of digits at the beginning of the string followed by a )
                    string regexExp = @"^[0-9]+[)]";
                    Match regexMatch = Regex.Match(currentLine, regexExp);
                    if (!regexMatch.Success)
                        continue;

                    /*
                     * 
                                  Compound                     R.T.       Response    Conc Units
                           ---------------------------------------------------------------------------

                           Target Compounds
                           1)     Methane                     8.816          28699    N.D.  ppbv m
                           2)     Ethane                     10.268          81417    1.398 ppbv m
                     *
                     */

                    //This is different from SOP_4426_AMCD_SFSB processor                                  
                    tokens = Regex.Split(currentLine, @"\s{1,}");
                    analyteID = tokens[1].Trim();

                    string measuredValTmp = tokens[4].Trim();
                    if (!Double.TryParse(measuredValTmp, out measuredVal))
                        measuredVal = 0.0;

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;

                    dt.Rows.Add(dr);

                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + ex.Message;
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row + 1);
                rm.ErrorMessage = errorMsg;
            }

            rm.TemplateData = dt;

            return rm;

        }
    }
}