using System;
using System.Data;
using System.IO;
using PluginBase;


namespace CHL_TOC
{
    public class CHL_TOC : DataProcessor
    {
        public override string id { get => "chi_toc"; }
        public override string name { get => "CHL_TOC"; }
        public override string description { get => "Processor used for CHL_TOC translation to universal template"; }
        public override string file_type { get => ".txt"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            DataTable dt = null;
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                using StreamReader sr = new StreamReader(input_file);
               
                int rowIdx = 0;
                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    //First row is header. We'll skip it
                    if (rowIdx < 14)
                    {
                        rowIdx++;
                        continue;
                    }
                }

            }
            catch (Exception ex)
            {

            }

            rm.TemplateData = dt;
            return rm;
}