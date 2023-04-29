using System;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using PluginBase;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace SOP_4426_AMCD_SFSB
{
    public class SOP_4426_AMCD_SFSB : DataProcessor
    {
        public override string id { get => "sop_4426_amcd_sfsb1.0"; }
        public override string name { get => "SOP_4426_AMCD_SFSB"; }
        public override string description { get => "Processor used for SOP_4426_AMCD_SFSB translation to universal template"; }
        public override string file_type { get => ".txt"; }
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
                //bool bStart = false;
                bool bDataFile = false;
                bool bQuantTime = false;
                while ((line = sr.ReadLine()) != null)
                {
                    rowIdx++;
                    current_row = rowIdx;
                    string currentLine = line;
                    if (currentLine.Contains("Data File", StringComparison.OrdinalIgnoreCase))
                    {
                        //e.g.  Data File : 101421_6.d
                        tokens = currentLine.Split(":");
                        aliquot = tokens[1].Trim();
                        bDataFile = true;
                        continue;
                    }

                    //e.g.  Quant Time: Oct 15 10:00:08 2021
                    string target = @"Quant Time:";
                    if (currentLine.Contains(target, StringComparison.OrdinalIgnoreCase))
                    {
                        
                        int idx = currentLine.IndexOf(target);

                        //string sval3 = currentLine.Substring(idx, target.Length);
                        string tmpDT = currentLine.Substring(idx + target.Length).Trim();
                        //int idx = currentLine.Trim().IndexOf(':');
                        //string tmpDT = currentLine.Substring(idx + 1).Trim();
                        string pattern = "MMM dd HH:mm:ss yyyy";
                        if (!DateTime.TryParseExact(tmpDT, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out analysisDateTime))
                            throw new Exception("Invalid format for Quant Time");
                        bQuantTime = true;
                        continue;
                    }

                    if (!bDataFile || !bQuantTime)
                        continue;

                    //This regular expression will match any number of digits at the beginning of the string followed by a )
                    string regexExp = @"^[0-9]+[)]";
                    Match regexMatch = Regex.Match(currentLine.Trim(), regexExp);
                    if (!regexMatch.Success)
                        continue;

                    /*
                     * 
                               Compound                  R.T. QIon  Response  Conc Units Dev(Min)
                       --------------------------------------------------------------------------
                       Internal Standards
                         1) d5-chlorobenzene           34.246  117   423722    20.00 ppb     -0.02
                         2) 1,4 difluorobenzene        30.180  114  1145578    20.00 ppb     -0.06

                       Target Compounds                                                   Qvalue
                         3) CF4                         7.163   69       57     0.11 ppb  #     1
                         4) C2F6                        8.229  119      118     0.00 ppb  #    64
                         5) chlorotrifluoromethane      8.654   85      219     0.01 ppb  #     1 
                     *
                     */


                    //numID will be like 1)
                    //string numID = currentLine.Substring(0,7).Trim();

                    tokens = Regex.Split(currentLine.Trim(), @"\s{2,}");
                    analyteID = Regex.Split(tokens[0], @"\s{1,}")[1];

                    //analyteID = currentLine.Substring(8, 26).Trim();

                    //userDefined1 = currentLine.Substring(47, 9).Trim();
                    userDefined1 = tokens[3].Trim();

                    //string measuredValTmp = currentLine.Substring(57, 15).Trim();
                    string measuredValTmp = tokens[4].Trim();
                    tokens = Regex.Split(measuredValTmp, @"\s{1,}");

                    measuredValTmp = tokens[0].Trim();
                    //if (string.Compare(tokens[0].Trim(), "n.d.", true) ==0 || string.Compare(tokens[1].Trim(), "n.d.", true) == 0)
                    //    measuredVal = 0.0;
                    if (!Double.TryParse(measuredValTmp, out measuredVal))
                        measuredVal = 0.0;

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dr["User Defined 1"] = userDefined1;

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