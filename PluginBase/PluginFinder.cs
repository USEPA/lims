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
            //var pluginFinderAssemblyContext = new PluginAssemblyLoadingContext("PluginFinderAssemblyContext");
            var pluginFinderAssemblyContext = new CollectibleAssemblyLoadContext(pluginsPath);
            //var pluginFinderAssemblyContext = new AssemblyLoadContext(name: "PluginFinderAssemblyContext"); 
            foreach (var assemblyPath in assemblyPaths)
            {
                var assembly = pluginFinderAssemblyContext.LoadFromAssemblyPath(assemblyPath);
                if (GetPluginTypes(assembly).Any())
                {
                    assemblyPluginInfos.Add(assembly.Location);
                }
            }
            pluginFinderAssemblyContext.Unload();
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
    }
}

