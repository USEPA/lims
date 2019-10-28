using System;

namespace PluginBase
{
    public interface IProcessor
    {
        public string UniqueId { get; }
        public string Name { get; }
        public string Description { get; }
        public FileTypes InstrumentFileType { get; }
        public string InputFile { get; set; }
        public string OutputFile { get; set; }

        public DataTableResponseMessage Execute();
                           
    }
}
