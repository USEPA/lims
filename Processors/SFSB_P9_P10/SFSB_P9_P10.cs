using System;
using PluginBase;
using System.Reflection;
using System.IO;
using System.Data;

namespace SFSB_P9_P10
{
    public class SFSB_P9_P10 : DataProcessor
    {
        public override string id { get => "sfsb_p9_p10_1.0"; }
        public override string name { get => "SFSB_P9_P10"; }
        public override string description { get => "Processor used for SFSB P9 P10 translation to universal template"; }
        public override string file_type { get => ".csv"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }


        public SFSB_P9_P10()
        {

        }

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


                current_row = 0;
                DataRow dr = null;

                List<string> lstAliquots = null;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    current_row++;                    
                    string[] tokens = line.Split(",");

                    //Row 1 contains Aliquots
                    if (current_row == 1)
                    {
                        lstAliquots = new List<string>(tokens);
                        continue;
                    }
                    //Row 2 - skip
                    if (current_row == 2)
                        continue;

                    analyteID = tokens[0];
                    //Skip first column because we already have analyteID
                    for (int i = 1; i < lstAliquots.Count; i+=2)
                    {
                        aliquot = lstAliquots[i];
                        if (!double.TryParse(tokens[i], out measuredVal))
                            measuredVal = double.NaN;

                        double user_defined1;
                        if (!double.TryParse(tokens[i+1], out user_defined1))
                            user_defined1 = double.NaN;

                        dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;
                        dr["User Defined 1"] = user_defined1;

                        dt.Rows.Add(dr);
                    }                     
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
            }
            return rm;
        }
    }
}