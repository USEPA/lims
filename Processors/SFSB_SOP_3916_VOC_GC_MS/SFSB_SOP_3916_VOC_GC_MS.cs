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
