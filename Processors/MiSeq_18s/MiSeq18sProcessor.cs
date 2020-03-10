using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using PluginBase;
using OfficeOpenXml;

namespace MiSeq_18s
{
    public class MiSeq18sProcessor : DataProcessor
    {
        public override string id { get => "miseq_18s_version1.0"; }
        public override string name { get => "MiSeq_18S"; }
        public override string description { get => "Processor used for MiSeq_18S translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
        public override string version { get => "1.0"; }
        public override string input_file { get; set; }
        public override string path { get; set; }

        public MiSeq18sProcessor()
        {

        }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            try
            {
                rm = VerifyInputFile();
                if (rm != null)
                    return rm;

                rm = new DataTableResponseMessage();
                DataTable dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                var worksheet = package.Workbook.Worksheets[0];  //Worksheets are zero-based index                
                string name = worksheet.Name;
            }
            catch (Exception ex)
            {

            }

            return rm;
        }
    }
}
