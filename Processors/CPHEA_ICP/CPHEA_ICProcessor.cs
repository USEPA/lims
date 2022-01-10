using System;
using PluginBase;
using System.Reflection;
using System.IO;
using System.Data;

namespace CPHEA_ICP
{
    public class CPHEA_ICProcessor : DataProcessor
    {
        public override string id { get => "cphea_ic_version1.0"; }
        public override string name { get => "CPHEA_IC"; }
        public override string description { get => "Processor used for CPHEA IC translation to universal template"; }
        public override string file_type { get => ".csv"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public CPHEA_ICProcessor()
        {
        }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            DataTable dt = GetDataTable();
            dt.TableName = System.IO.Path.GetFileNameWithoutExtension(input_file);


            return rm;
        }
    }
}
