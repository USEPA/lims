using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.Loader;
using System.Text;


namespace PluginBase
{
    public class PluginFinder<TPlugin> where TPlugin : DataProcessor
    {
        public PluginFinder() { }

        public IReadOnlyCollection<string> FindAssemliesWithPlugins(string path)
        {
            var assemblies = Directory.GetFiles(path, "*.dll", new EnumerationOptions() { RecurseSubdirectories = true });
            return FindPluginsInAssemblies(path, assemblies);
        }

        private IReadOnlyCollection<string> FindPluginsInAssemblies(string pluginsPath, string[] assemblyPaths)
        {
            var assemblyPluginInfos = new List<string>();
            string fullAssemblyPath = null;
            try
            {                
                //var pluginFinderAssemblyContext = new PluginAssemblyLoadingContext("PluginFinderAssemblyContext");
                var pluginFinderAssemblyContext = new CollectibleAssemblyLoadContext(pluginsPath);
                //var pluginFinderAssemblyContext = new AssemblyLoadContext(name: "PluginFinderAssemblyContext"); 
                var cwd = System.IO.Directory.GetCurrentDirectory();
                foreach (var assemblyPath in assemblyPaths)
                {
                    fullAssemblyPath = System.IO.Path.Combine(cwd, assemblyPath);
                    var assembly = pluginFinderAssemblyContext.LoadFromAssemblyPath(fullAssemblyPath);
                    if (GetPluginTypes(assembly).Any())
                    {
                        assemblyPluginInfos.Add(assembly.Location);
                    }
                }
                pluginFinderAssemblyContext.Unload();
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message + " " + fullAssemblyPath);                                
            }
            finally
            {
                
            }
            return assemblyPluginInfos;
        }

        public static IReadOnlyCollection<Type> GetPluginTypes(Assembly assembly)
        {
            return assembly.GetTypes()
                            .Where(type =>
                            !type.IsAbstract &&
                            typeof(DataProcessor).IsAssignableFrom(type))
                            .ToArray();
        }

        private void LogMessage(string message)
        {
            string logPath = "";            
            try
            {
                string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                FileInfo fi = new FileInfo(loc);
                DirectoryInfo di = fi.Directory;
                logPath = Path.Combine(fi.Directory.FullName, "logs");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);

                logPath = Path.Combine(logPath, "lims_desktop.log");

                File.AppendAllText(logPath, message);
            }
            catch (Exception ex)
            {
                
            }

            return;
        }
    }
}

