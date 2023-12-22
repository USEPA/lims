using System;
using PluginBase;
using System.Reflection;
using System.IO;
using System.Data;

namespace EDEB_SOP4718_ICPMS
{
    public class EDEB_SOP4718_ICPMS : DataProcessor
    {
        public override string id { get => "edeb_sop4718_icpms"; }
        public override string name { get => "EDEB_SOP4718_ICPMS"; }
        public override string description { get => "Processor used for EDEB SOP-4718 ICP-MS translation to universal template"; }
        public override string file_type { get => ".csv"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public EDEB_SOP4718_ICPMS()
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

                List<List<string>> csvData = getCsvData(sr);
                List<string> analyteIDs = csvData[0];

                DataRow dr;
                int aliquotIndex = 0;
                int nextAnalyteIndex = ColumnIndex0.J + 1;

                for (int i = ColumnIndex0.J; i < analyteIDs.Count; i += nextAnalyteIndex - i)
                {
                    // NOTE: analyte IDs start at column J (I'm assuming that's always)

                    nextAnalyteIndex++;

                    while (nextAnalyteIndex < analyteIDs.Count && string.IsNullOrWhiteSpace(analyteIDs[nextAnalyteIndex]))
                    {
                        nextAnalyteIndex++; // Move to the next non-blank element
                    }

                    if (nextAnalyteIndex < analyteIDs.Count)
                    {
                        intiateRowBuild(i, nextAnalyteIndex, csvData, aliquotIndex, dt);
                    }
                    else if (nextAnalyteIndex >= analyteIDs.Count && !string.IsNullOrWhiteSpace(analyteIDs[i]))
                    {
                        intiateRowBuild(i, nextAnalyteIndex, csvData, aliquotIndex, dt);
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

        private string convertBlanks(string dataCell)
        {
            /*
            Converts blank cells, "N/A", and "<0.00" to be imported as "0".
            */
            if (dataCell == "" || dataCell == "N/A" || dataCell.Contains("<0.00"))
            {
                return "0";
            }
            else
            {
                return dataCell;
            }
        }

        private List<List<string>> getCsvData(StreamReader sr) {
            /*
            Reads CSV and returns a list of list.
            */
            List<List<string>> csvData = new List<List<string>>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                List<string> rowVals = new List<string>();
                string[] vals = line.Split(',');
                rowVals.AddRange(vals);
                csvData.Add(rowVals);
            }
            return csvData;
        }

        private int intiateRowBuild(int analyteIndex, int nextAnalyteIndex, List<List<string>> csvData, int aliquotIndex, DataTable dt)
        {
            /* 
            Returns aliquot index to continue the counting.
            TODO: Determine if aliquot is a sequential index for all rows built, or if it recycles based on each analyte.
            */

            if (nextAnalyteIndex == analyteIndex + 2)
            {
                // Next non-blank item is two positions away from current one
                // Get all data from row 3 onward for "CPS" 
                aliquotIndex = buildRow(analyteIndex, analyteIndex, csvData, aliquotIndex, dt);
            }
            else if (nextAnalyteIndex == analyteIndex + 4)
            {
                // The next non-blank item is exactly four positions away from the current one
                // Get all data from row 3 onward for "Conc. [ppm]" label.
                aliquotIndex = buildRow(analyteIndex, analyteIndex + 2, csvData, aliquotIndex, dt);
            }

            return aliquotIndex;
        }

        private int buildRow(int analyteIndex, int measuredValueIndex, List<List<string>> csvData, int aliquotIndex, DataTable dt) {
            /*
            Builds row for standardized output file.
            */
            for (int j = 2; j < csvData.Count; j++)
            {
                aliquotIndex++;

                List<string> inputDataRow = csvData[j];

                DataRow dr = dt.NewRow();
                dr["Aliquot"] = aliquotIndex;
                dr["Analyte Identifier"] = csvData[0][analyteIndex];
                dr["Measured Value"] = convertBlanks(csvData[j][measuredValueIndex]);  // value should be +2 from i with label "Conc. [ppm]"
                dr["Units"] = "";  // are there units for this? PPM?
                dr["Dilution Factor"] = csvData[j][ColumnIndex0.I];
                dr["Analysis Date/Time"] = csvData[j][ColumnIndex0.D];  // found in column D
                dt.Rows.Add(dr);
            }

            return aliquotIndex;

        }



    }
}
