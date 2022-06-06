using System;
using PluginBase;
using System.Reflection;
using System.IO;
using System.Data;


namespace WinLab_ICP
{
    public class WinLabICPProcessor : DataProcessor
    {
        public override string id { get => "winlab_icp_version1.0"; }
        public override string name { get => "WinLab_ICP"; }
        public override string description { get => "Processor used for WinLab ICP translation to universal template"; }
        public override string file_type { get => ".csv"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public WinLabICPProcessor()
        {

        }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;

            DateTime analysisDateTime = DateTime.MinValue;

            //Data looks like this
            //Sample ID,A/S Loc,User Name 2,User Value 2,User Name 1,User Value 1,Sample Units,Aliquot Vol.,Diluted To Vol.,Analyst Name,Result Name,User Name 3,User Value 3,Analyte Name,Reported Conc (Samp),QC Recovery,Repl No1,Conc (Samp)1,Date1,Time1,Repl No2,Conc (Samp)2,Date2,Time2,Repl No3,Conc (Samp)3,Date3,Time3
            //ICV,5, ,4 / 1 / 2021, , , , , , ,STAD_7753_RW_051021, , ,Ag 328.068,4.96,99.39916292,1,4.87881223,5 / 10 / 2021,2:45:46 PM,2,4.971885733,5 / 10 / 2021,2:45:52 PM,3,5.029356726,5 / 10 / 2021,2:45:58 PM


            try
            {
                rm = VerifyInputFile();
                if (!rm.IsValid)
                    return rm;

                DataTable dt = GetDataTable();
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(input_file);

                using (StreamReader sr = new StreamReader(input_file))
                {
                    int idxRow = 0;                    
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        //First row is header. We'll skip it
                        if (idxRow < 1)
                        {
                            idxRow++;
                            continue;
                        }

                        line = line.Trim();
                        if (string.IsNullOrWhiteSpace(line))
                            break;

                        string[] tokens = line.Split(',');

                        string aliquot = tokens[0].Trim();

                        //Handle case of empty lines at end of file
                        if (string.IsNullOrWhiteSpace(aliquot))
                            break;

                        //Dilution factor
                        string dilFactor = tokens[12];

                        //Column M - User value 3
                        int dilutionFactor;
                        if (!int.TryParse(dilFactor, out dilutionFactor))
                            dilutionFactor = 1;

                        //Column N - Analyte Name
                        //Need to split this column components - Analyte ID and User Defined 1 - split on white space
                        string analyteName = tokens[13];
                        string[] analyteNameTokens = analyteName.Split(' ');

                        string analyteID = analyteNameTokens[0].Trim();
                        string userDefined1 = analyteNameTokens[1].Trim();

                        double measuredVal;
                        if (!double.TryParse(tokens[14], out measuredVal))
                            measuredVal = 0.0;

                        string date = tokens[26].Trim();
                        string time = tokens[27].Trim();
                        DateTime date_time = DateTime.Parse(date + " " + time);
                        string dateTime = date_time.ToString("MM/dd/yy hh:mm tt");

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;
                        dr["Analysis Date/Time"] = dateTime;

                        dr["Dilution Factor"] = dilutionFactor;
                        dr["User Defined 1"] = userDefined1;

                        dt.Rows.Add(dr);

                        idxRow++;
                    }
                    rm.TemplateData = dt;
                }
            }
            catch (Exception ex)
            {
                rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }

            return rm;
        }
    }
}

