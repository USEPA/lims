using System;
using PluginBase;
using System.Reflection;
using System.IO;
using System.Data;

namespace MassLynx
{
    public class MassLynxProcessor : DataProcessor
    {
        public override string id { get => "masslynx_version1.0"; }
        public override string name { get => "MassLynx"; }
        public override string description { get => "Processor used for MassLynx translation to universal template"; }
        public override string file_type { get => ".txt"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public MassLynxProcessor()
        {
            
        }
        

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            DataTable dt = GetDataTable();
            dt.TableName = System.IO.Path.GetFileNameWithoutExtension(input_file);
            string aliquot = "";
            string dilutionFactor = "";
            DateTime analysisDateTime = DateTime.MinValue;

            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();

                using (StreamReader sr = new StreamReader(input_file))
                {
                    int idxRow = 1;
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        idxRow++;
                        
                        //Data starts on line 21
                        if (idxRow < 21)
                            continue;

                        //Should not be any more blank lines until we get to the end of the data
                        if (string.IsNullOrWhiteSpace(line))
                            break;

                        //The sheet can contain mulitple data sets
                        //The first row of each data set block contains the LIMS ID and analysis date
                        //e.g. SW846_01DEC_18-2,AW325-S-38-01,,,,,,01-Dec-18,13:34:02
                        //     ^^^^^^^^^^^^^^^^ ^^^^^^^^^^^^^      ^^^^^^^^^^^^^^^^^^
                        //     aliquot          LIMS ID           analysis date time

                        //The data looks like:
                        //e.g. 1,PFOA,30,7.0112,0,214.703,4589,16.0753,dd,2.6196,0,412.9 > 369,,20,26712.1,607300,7.0112,,,,,0,7.0112,0,0,0,6.9624,7.0491,214.703,0,412.9 > 169,1,4.5385,1,47.307,1035,3.169,556.637,108.689,1e-012,1e-012,0,,
                        //       ^^^^             ^^^^^^^                 ^^^^^^
                        //     analyte id         Area                    Measured conc   

                        string[] tokens = line.Split(',');
                        //If this is an int then its data, otherwise start of new dataset
                        string col1 = tokens[0];
                        int id;
                        if (!Int32.TryParse(col1, out id))
                        {
                            string[] aliquot_dilFactor = GetAliquotDilutionFactor(tokens[1]);
                            aliquot = aliquot_dilFactor[0];
                            dilutionFactor = aliquot_dilFactor[1];

                            string date = tokens[tokens.Length - 2] + " " + tokens[tokens.Length - 1];
                            analysisDateTime = Convert.ToDateTime(date);
                            continue;
                        }

                        DataRow dr = dt.NewRow();
                        //Aliquot
                        dr[0] = aliquot;

                        //Analyte id
                        dr[1] = tokens[1];

                        //Measured value
                        if (string.IsNullOrWhiteSpace(tokens[9]))
                            dr[2] = 0.0;
                        else
                            dr[2] = tokens[9];

                        //Dilution factor
                        dr[4] = dilutionFactor;

                        //Date/time
                        dr[5] = analysisDateTime;

                        //User defined 1
                        if (string.IsNullOrWhiteSpace(tokens[9]))
                            dr[8] = 0.0;
                        else
                            dr[8] = tokens[5];

                        dt.Rows.Add(dr);

                    }

                    rm.TemplateData = dt;
                }
            }
            catch (Exception ex)
            {
                rm.AddLogMessage(string.Format("Processor: {0}, Exception: {1}", name, ex.Message));
                rm.AddErrorAndLogMessage(string.Format("Problem executing processor {0} on input file {1}.", name, input_file));
            }
            
            return rm;
        }
    }
}
