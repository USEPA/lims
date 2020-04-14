using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OfficeOpenXml;
using System.Data;
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
        //public string OutputFile { get; set; }
        public string Path { get; set; }

        public ProcessorDTO()
        {
        }
        public ProcessorDTO(DataProcessor processor)
        {
            UniqueId = processor.id;
            Name = processor.name;
            Description = processor.description;
            InstrumentFileType = processor.file_type;
            InputFile = processor.input_file;
            //OutputFile = processor.OutputFile;
            Path = processor.path;
        }
    }
    public class ProcessorManager
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public List<ProcessorDTO> GetProcessors(string processorsPath)
        {
            List<ProcessorDTO> lstProcessors = new List<ProcessorDTO>();
            var pluginFinder = new PluginFinder<DataProcessor>();
            var assemblyPaths = pluginFinder.FindAssemliesWithPlugins(processorsPath);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var pluginHost = new PluginHost<DataProcessor>(processorsPath);
            pluginHost.LoadPlugins(assemblyPaths);

            foreach (var processor in pluginHost.GetPlugins())
            {
                ProcessorDTO pdto = new ProcessorDTO(processor);
                lstProcessors.Add(pdto);
            }

            //string[] assemblies = Directory.GetFiles(processorsPath, "*.dll", new EnumerationOptions() { RecurseSubdirectories = true });
            //DirectoryInfo dirInfo = new DirectoryInfo(processorsPath);
            //DirectoryInfo[] dirInfos = dirInfo.GetDirectories();
            //foreach (DirectoryInfo di in dirInfos)
            //{
            //    List<ProcessorDTO> processors = LoadProcessors(di.FullName);
            //    lstProcessors.AddRange(processors);
            //}

            return lstProcessors;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private List<ProcessorDTO> LoadProcessors(string processorPath)
        {
            List<ProcessorDTO> lstProcessors = new List<ProcessorDTO>();
            DirectoryInfo di = new DirectoryInfo(processorPath);
            FileInfo[] fileInfos = di.GetFiles("*.dll");

            foreach (FileInfo fi in fileInfos)
            {
                //using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
                //{
                try
                {
                    var context = new CollectibleAssemblyLoadContext(processorPath);
                    //var assembly = context.LoadFromStream(fs);
                    var assembly = context.LoadFromAssemblyPath(fi.FullName);
                    //Can have multiple processors implmented in a single assembly
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (typeof(DataProcessor).IsAssignableFrom(type))
                        {
                            DataProcessor result = Activator.CreateInstance(type) as DataProcessor;
                            if (result != null)
                            {
                                ProcessorDTO pdto = new ProcessorDTO(result);
                                pdto.Path = fi.FullName;
                                lstProcessors.Add(pdto);
                            }
                        }
                    }
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                //Handle unloadable libraries
                catch (Exception ex)
                {
                    string msg = ex.Message;
                }
                //}
            }

            return lstProcessors;

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DataTableResponseMessage ExecuteProcessor(string processorsPath, string processorName, string inputFile)
        {
            DataTableResponseMessage dtRespMsg = null;
            PluginHost<DataProcessor> pluginHost = null;
            try
            {
                List<string> lstPlugin = new List<string>();
                lstPlugin.Add(processorsPath);
                pluginHost = new PluginHost<DataProcessor>(processorsPath);
                pluginHost.LoadPlugins(lstPlugin);

                DataProcessor proc = pluginHost.GetPlugin(processorName);
                if (proc != null)
                {
                    proc.input_file = inputFile;                    
                    dtRespMsg = proc.Execute();
                }
                //foreach (var processor in pluginHost.GetPlugins())
                //{
                //    if (string.Compare(processor.name, processorName, true) == 0)
                //    {
                //        processor.input_file = inputFile;
                //        //result.OutputFile = outputFile;
                //        dtRespMsg = processor.Execute();
                //        break;
                //    }

                //}

            }
            catch (Exception ex)
            {
                if (dtRespMsg == null)
                    dtRespMsg = new DataTableResponseMessage();
                dtRespMsg.LogMessage = string.Format("Error processing file: {0}  with Exception - {1}", inputFile, ex.Message);
                dtRespMsg.ErrorMessage = string.Format("Error processing file: {0}  with Exception - {1}", inputFile, ex.Message);
            }

            if (pluginHost != null)
            {
                pluginHost.Unload();                                
            }
            return dtRespMsg;
        
    


            //using (var fs = new FileStream(processorsPath, FileMode.Open, FileAccess.Read))
            //{
            //    try
            //    {
            //        var context = new CollectibleAssemblyLoadContext(processorsPath);
            //        var assembly = context.LoadFromStream(fs);
            //        //Can have multiple processors implmented in a single assembly
            //        foreach (Type type in assembly.GetTypes())
            //        {
            //            if (typeof(DataProcessor).IsAssignableFrom(type))
            //            {
            //                DataProcessor result = Activator.CreateInstance(type) as DataProcessor;
            //                if (result != null)
            //                {
            //                    if (string.Compare(result.id, processorID, true) == 0)
            //                    {
            //                        result.input_file = inputFile;
            //                        //result.OutputFile = outputFile;
            //                        dtRespMsg = result.Execute();
            //                        break;
            //                    }
            //                }
            //            }
            //        }                    
            //        context.Unload();
            //        GC.Collect();
            //        GC.WaitForPendingFinalizers();

            //    }
            //    //Handle unloadable libraries
            //    catch (Exception ex)
            //    {
            //        if (dtRespMsg == null)
            //            dtRespMsg = new DataTableResponseMessage();
            //        dtRespMsg.LogMessage = string.Format("Error processing file: {0}  with Exception - {1}", inputFile,  ex.Message);
            //    }
            //    return dtRespMsg;            
        }

        public ResponseMessage WriteTemplateOutputFile(string outputPath, DataTable dt)
        {
            ResponseMessage rm = new ResponseMessage();
            string fileName = Path.Combine(outputPath, dt.TableName + ".xlsx");
            try
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);

                FileInfo fi = new FileInfo(fileName);
                //New in version 5 - must deal with License
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using ExcelPackage xlPkg = new ExcelPackage(fi);
                                   
                ExcelWorksheet workSheet = xlPkg.Workbook.Worksheets.Add("Sheet1");
                int numRows = dt.Rows.Count;
                int numCols = dt.Columns.Count;

                //Add column names
                for (int i = 0; i < numCols; i++)                    
                    workSheet.Cells[1, i + 1].Value = dt.Columns[i].ColumnName;

                for (int i = 0; i < numRows; i++)
                {
                    DataRow row = dt.Rows[i];
                    for (int j = 0; j < numCols; j++)
                    {
                        workSheet.Cells[i + 2, j + 1].Value = row[j];
                    }
                }

                xlPkg.Save();
                    
                rm.Message = "";
                rm.OutputFile = fileName;
                
            }
            catch(Exception ex)
            {
                rm.ErrorMessage = string.Format("Error writing template file: {0}",  fileName);
            }
            return rm;
        }
    }
}
