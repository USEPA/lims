using System;
using PluginBase;
using System.Reflection;
using System.IO;
using System.Data;
using System.Globalization;

namespace CPHEA_ICP
{
    public class CPHEA_ICPProcessor : DataProcessor
    {
        public override string id { get => "cphea_icp_version1.0"; }
        public override string name { get => "CPHEA_ICP"; }
        public override string description { get => "Processor used for CPHEA IC translation to universal template"; }
        public override string file_type { get => ".csv"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public CPHEA_ICPProcessor()
        {
        }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            int idxRow = 0;
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                DataTable dt = GetDataTable();
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(input_file);
                
                using (StreamReader sr = new StreamReader(input_file))
                {
                    
                    string line;
                    string[] colNames;
                    while ((line = sr.ReadLine()) != null)
                    {
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
                        if (!double.TryParse(data[9].Trim(), out measuredVal))
                            throw new Exception("Invalid measured value in line: " + idxRow.ToString());
                        
                        DateTime analysisDateTime;
                        if (!DateTime.TryParse(data[1] + " " + data[2], out analysisDateTime ))
                            throw new Exception("Invalid date-time value in line: " + idxRow.ToString());


   
                        double dilutionFactor = 0.0;
                        string vol = data[4].Trim();
                        string dilution = data[5].Trim();
                        if (string.IsNullOrWhiteSpace(vol) || string.IsNullOrWhiteSpace(dilution))
                            dilutionFactor = 0.0;
                        else
                        {
                            double dvol;
                            double ddilution;
                            if (!double.TryParse(vol, out dvol))
                                break;
                            if (!double.TryParse(dilution, out ddilution))
                                break;

                            dilutionFactor = dvol / ddilution;
                        }

                        DataRow dr = dt.NewRow();
                        dr["Aliquot"] = aliquot;
                        dr["Analyte Identifier"] = analyteID;
                        dr["Measured Value"] = measuredVal;
                        dr["Analysis Date/Time"] = analysisDateTime;
                        dr["Dilution Factor"] = dilutionFactor;

                        dt.Rows.Add(dr);
                    }
                }

                rm.TemplateData = dt;

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
