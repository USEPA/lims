using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace PluginBase
{
    public class ProcessorDTO
    {
        public string UniqueId { get; }
        public string Name { get; }
        public string Description { get; }
        public string InstrumentFileType { get; }
        public string InputFile { get; set; }
        public string OutputFile { get; set; }

        public ProcessorDTO()
        {
        }
        public ProcessorDTO(IProcessor processor)
        {
            UniqueId = processor.UniqueId;
            Name = processor.Name;
            Description = processor.Description;
            InstrumentFileType = processor.InstrumentFileType;
            InputFile = processor.InputFile;
            OutputFile = processor.OutputFile;
        }
    }
    public class ProcessorManager
    {

        public List<ProcessorDTO> GetProcessors(string processorsPath)
        {
            List<ProcessorDTO> lstProcessors = new List<ProcessorDTO>();
            DirectoryInfo dirInfo = new DirectoryInfo(processorsPath);
            DirectoryInfo[] dirInfos = dirInfo.GetDirectories();
            foreach (DirectoryInfo di in dirInfos)
            {
                List<ProcessorDTO> processors = LoadProcessors(di.FullName);
                lstProcessors.AddRange(processors);
            }

            return lstProcessors;
        }

        private List<ProcessorDTO> LoadProcessors(string processorPath)
        {
            List<ProcessorDTO> lstProcessors = new List<ProcessorDTO>();
            DirectoryInfo di = new DirectoryInfo(processorPath);
            FileInfo[] fileInfos = di.GetFiles("*.dll");

            foreach (FileInfo fi in fileInfos)
            {
                using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        var context = new CollectibleAssemblyLoadContext();
                        var assembly = context.LoadFromStream(fs);
                        //Can have multiple processors implmented in a single assembly
                        foreach (Type type in assembly.GetTypes())
                        {
                            if (typeof(IProcessor).IsAssignableFrom(type))
                            {
                                IProcessor result = Activator.CreateInstance(type) as IProcessor;
                                if (result != null)
                                {
                                    ProcessorDTO pdto = new ProcessorDTO(result);
                                    lstProcessors.Add(pdto);
                                }
                            }
                        }
                        context.Unload();
                    }
                    //Handle unloadable libraries
                    catch (Exception ex)
                    {
                        string msg = ex.Message;
                    }
                }
            }

            return lstProcessors;

        }
    }
}
