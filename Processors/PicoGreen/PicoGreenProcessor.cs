using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using OfficeOpenXml;
using PluginBase;

namespace PicoGreen
{
    public class PicoGreenProcessor : Processor
    {
        public override string UniqueId { get => "pico_green_version1.0"; }

        public override string Name { get => "PicoGreen"; }

        public override string Description { get => "Processor used for PicoGreen translation to universal template"; }

        public override string InstrumentFileType { get => ".xlsx"; }

        public override string InputFile { get; set; }

        public override string Path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = new DataTableResponseMessage();
            try
            {
                rm = VerifyInputFile();
                FileInfo fi = new FileInfo(InputFile);

                using (var package = new ExcelPackage(fi))
                {
                    //Data is in the 2nd sheet
                    var worksheet = package.Workbook.Worksheets[1]; //Worksheets are zero-based index
                    string name = worksheet.Name;
                    int startRow = worksheet.Dimension.Start.Row;
                    int startCol = worksheet.Dimension.Start.Column;
                    int numRows = worksheet.Dimension.End.Row;
                    int numCols = worksheet.Dimension.End.Column;

                    DataTable dt_template = GetDataTable();
                    dt_template.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
                    TemplateField[] fields = Fields;
                }

            }
            catch (Exception ex)
            {
                rm.AddErrorAndLogMessage(string.Format("Problem transferring data file {0}  to template file", InputFile));
            }

            return rm;
        }
    }
}
