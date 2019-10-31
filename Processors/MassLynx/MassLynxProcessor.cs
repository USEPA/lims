using System;
using PluginBase;

namespace MassLynx
{
    public class MassLynxProcessor : Processor, IProcessor
    {
        public string UniqueId { get => "maxlynx_version1.0"; }

        public string Name { get => "MaxLynx"; }

        public string Description { get => "Processor used for MaxLynx translation to universal template"; }

        public string InstrumentFileType { get => "xlsx"; }

        public string InputFile { get; set; }
        public string OutputFile { get; set; }

        public DataTableResponseMessage Execute()
        {
            throw new NotImplementedException();
        }
    }
}
