using System;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using PluginBase;
using System.Globalization;

namespace Agilent_6890
{
    public class Agilent_6890 : DataProcessor
    {
        public override string id { get => "agilent_6890.0"; }
        public override string name { get => "Agilent_6890"; }
        public override string description { get => "Processor used for Agilent_6890 translation to universal template"; }
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
                    //Example of line 2 - used for aliquot
                    //Sample	Name:	Std	3_0.933
                    if (current_row == 2)
                    {
                        tokens = currentLine.Split(":");
                        for (int i = 1; i < tokens.Length; i++)
                            aliquot += tokens[i].Trim() + " ";
                        //Remove trailing space
                        aliquot = aliquot.Trim();
                        continue;
                    }

                    //Example of line 8 - used for analysis datetime
                    //Injection Date    :	4/8/2019    7:21:57 PM Inj :	1
                    if (current_row == 8)
                    {
                        int idx = currentLine.IndexOf(":");
                        if (idx == -1)
                            throw new Exception("Unable to parse datetime line- " + currentLine);
                        string tmpDateTime = currentLine.Substring(idx + 1).Trim();
                        //Split rest of string on spaces and tabs
                        tokens = Regex.Split(tmpDateTime, @"\s{1,}");
                        tmpDateTime = tokens[0].Trim() + " " + tokens[1].Trim() + " " + tokens[2].Trim();
                        if (!DateTime.TryParse(tmpDateTime, out analysisDateTime))
                            throw new Exception("Unable to parse datetime value- " + tmpDateTime);

                        continue;

                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + ex.Message;
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row );
                rm.ErrorMessage = errorMsg;
            }

            rm.TemplateData = dt;

            return rm;
        }
    }
}