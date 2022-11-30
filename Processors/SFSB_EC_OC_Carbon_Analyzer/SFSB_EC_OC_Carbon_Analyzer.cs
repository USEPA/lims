using System.IO;
using System.Data;
using PluginBase;


namespace SFSB_EC_OC_Carbon_Analyzer
{
    public class SFSB_EC_OC_Carbon_Analyzer : DataProcessor
    {
        public override string id { get => "sfsb_ec_oc_carbon_analyzer.0"; }
        public override string name { get => "SFSB_EC_OC_Carbon_Analyzer"; }
        public override string description { get => "Processor used for SFSB EC OC Carbon Analyzer translation to universal template"; }
        public override string file_type { get => ".csv"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public SFSB_EC_OC_Carbon_Analyzer()
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

                string line;
                string[] colNames;
                while ((line = sr.ReadLine()) != null)
                {
                    idxRow++;
                    if (idxRow ==1)
                        continue;
                    //Assume blank line is end of data in file
                    if (string.IsNullOrWhiteSpace(line))
                        break;

                    //First row is headers
                    if (idxRow == 0)
                    {
                        colNames = line.Split(',');
                        idxRow++;
                        continue;
                    }

                    idxRow++;
                    current_row = idxRow;

                    string[] data = line.Split(',');
                    //Column P is the last column that contains data we need
                    if (data.Length < 16)
                        throw new Exception("File does not contain necessary number of columns. Less than 16.");

                    string aliquot = data[0];

                    string element = data[7].Trim();
                    string waveLength = data[8].Trim();
                    string radialView = data[15].Trim();
                    if (radialView == "0")
                        radialView = " R";
                    else
                        radialView = "";

                    string analyteID = element + " " + waveLength + radialView;

                    double measuredVal;
                    //string tmpMeasuredVal = data[9].Trim();
                    string tmpMeasuredVal = data[ColumnIndex0.L].Trim();
                    if (string.IsNullOrWhiteSpace(tmpMeasuredVal))
                        measuredVal = 0.0;
                    else if (!double.TryParse(tmpMeasuredVal, out measuredVal))
                        measuredVal = 0.0;

                    DateTime analysisDateTime;
                    if (!DateTime.TryParse(data[1] + " " + data[2], out analysisDateTime))
                        throw new Exception("Invalid date-time value: " + data[1]);

                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = aliquot;
                    dr["Analyte Identifier"] = analyteID;
                    dr["Measured Value"] = measuredVal;
                    dr["Analysis Date/Time"] = analysisDateTime;
                    //dr["Dilution Factor"] = dilutionFactor;

                    dt.Rows.Add(dr);
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
                //rm.LogMessage = string.Format("Processor: {0},  InputFile: {1}, Exception: {2}", name, input_file, ex.Message);
                //rm.ErrorMessage = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
            }
            return rm;
        }
    }
}