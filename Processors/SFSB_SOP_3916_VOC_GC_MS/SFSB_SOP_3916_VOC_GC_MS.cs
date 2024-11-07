using System;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using PluginBase;
using System.Globalization;

namespace SFSB_SOP_3916_VOC_GC_MS
{
    public class SFSB_SOP_3916_VOC_GC_MS : DataProcessor
    {
        public override string id { get => "sfsb_sop_3916_voc_gc_ms1.0"; }
        public override string name { get => "SFSB_SOP_3916_VOC_GC_MS"; }
        public override string description { get => "Processor used for SFSB_SOP_3916_VOC_GC_MS translation to universal template"; }
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
                            aliquot = aliquot.Split(".")[0];
                            bDataFile = true;
                            continue;
                        }
                    }
                    if (!bQuantTime)
                    {
                        //e.g.  Quant Time: Jul 08 14:08:06 2024
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

                    //This regular expression will match any number of digits at the beginning of the string followed by a ) or ]
                    string regexExp = @"^[0-9]+[)\]]";
                    Match regexMatch = Regex.Match(currentLine, regexExp);
                    if (!regexMatch.Success)
                        continue;

                    /*
                     * 
                        Target Compounds                                                   Qvalue
                            2] Formaldehyde                3.932   29   214452m    5.27 ppbv        
                            3] Acetaldehyde                5.026   29   440957m    8.90 ppbv        
                            4] Methanol                    5.367   29   604417    23.50 ppbv #    64
                            5] Furan                       7.481   68   657252     2.32 ppbv      93
                            6] Propanal                    7.909   58    58453m    1.42 ppbv        
                            7] Methacrolein               10.878   41    35972     0.38 ppbv #    87
                     * 
                     */
                                                   
                    tokens = Regex.Split(currentLine, @"\s{1,}");
                    analyteID = tokens[1].Trim();

                    string measuredValTmp = tokens[4].Trim();
                    int idx2 = measuredValTmp.LastIndexOf("m", StringComparison.OrdinalIgnoreCase);
                    if (idx2 != -1)
                        measuredValTmp = measuredValTmp.Substring(0, idx2).Trim();

                    if (!Double.TryParse(measuredValTmp, out measuredVal))
                        measuredVal = 0.0;

                    userDefined2 = tokens[2].Trim();
                    userDefined3 = tokens[3].Trim();

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    dr["Measured Value"] = measuredVal;
                    dr["User Defined 2"] = userDefined2;
                    dr["User Defined 3"] = userDefined3;
                    if (idx2 != -1)
                        dr["User Defined 1"] = "m";
                    

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
