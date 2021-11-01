using System;
using System.Data;
using System.IO;
using PluginBase;
using OfficeOpenXml;

namespace Shimadzu_TOC_VCPH
{
    public class Shimadzu_TOC_VCPH : DataProcessor
    {

        public override string id { get => "shimadzu_toc_vcph_version1.0"; }
        public override string name { get => "Shimadzu_TOC_VCPH"; }
        public override string description { get => "Processor used for Shimadzu TOC VCPH translation to universal template"; }
        public override string file_type { get => ".xlsx"; }
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

                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //This is a new way of using the 'using' keyword with braces
                using var package = new ExcelPackage(fi);

                var worksheet = package.Workbook.Worksheets[0];  //Worksheets are zero-based index                
                string name = worksheet.Name;

                //File validation
                if (worksheet.Dimension == null)
                {
                    string msg = string.Format("No data in Sheet 1 in InputFile:  {0}", input_file);
                    rm.LogMessage = msg;
                    rm.ErrorMessage = msg;
                    return rm;
                }

                int startRow = worksheet.Dimension.Start.Row;
                int startCol = worksheet.Dimension.Start.Column;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

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
