using System;
using PluginBase;
using System.IO;
using System.Data;

namespace SRB_ICP_MS
{
    public class SRB_ICP_MS : DataProcessor
    {
        public override string id { get => "SRB_ICP_MS_version1.0"; }
        public override string name { get => "SRB_ICP_MS"; }
        public override string description { get => "Processor used for SRB_ICP_MS translation to universal template"; }
        public override string file_type { get => ".ASC"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public SRB_ICP_MS()
        {
            
        }
        
        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            DataTable dt = GetDataTable();
            dt.TableName = System.IO.Path.GetFileNameWithoutExtension(input_file);

            try
            {
                rm = VerifyInputFile();
                if (!rm.IsValid)
                    return rm;

                using (StreamReader sr = new StreamReader(input_file))
                {
                    int rowIdx = 0;
                    string line;

                    string[] aliquot_names = new string[] { };

                    while ((line = sr.ReadLine()) != null)
                    {
                        rowIdx++;
                        current_row = rowIdx;
                        // row 1 has the aliquot names
                         if (current_row == 1)
                        {
                            aliquot_names = line.Split('\t');
                        }

                        // rows 7 - linebreak have data
                        if (current_row < 7)
                        {
                            continue;
                        }

                        string[] currentLine = line.Split('\t');
                        if (currentLine[0] == "")
                        {
                            break;
                        }
                        analyteID = currentLine[0];

                        for (int i = 1; i < currentLine.Length; i++)
                        {
                            if (aliquot_names[i] != "")
                            {
                                aliquot = aliquot_names[i];
                                Double.TryParse(currentLine[i], out measuredVal);

                                DataRow dr = dt.NewRow();
                                dr["Aliquot"] = aliquot;
                                dr["Analyte Identifier"] = analyteID;
                                dr["Measured Value"] = measuredVal;

                                dt.Rows.Add(dr);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }

            rm.TemplateData = dt;

            return rm;
        }
    }
}
