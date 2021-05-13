using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text;

namespace PluginBase
{
    public class PluginHost<TPlugin> where TPlugin : DataProcessor
    {
        private Dictionary<string, TPlugin> _plugins = new Dictionary<string, TPlugin>();
        private readonly CollectibleAssemblyLoadContext _pluginAssemblyLoadingContext;
        //private readonly AssemblyLoadContext _pluginAssemblyLoadingContext;

        public PluginHost(string pluginsPath)
        {
            _pluginAssemblyLoadingContext = new CollectibleAssemblyLoadContext(pluginsPath);
            //_pluginAssemblyLoadingContext = new AssemblyLoadContext("PluginAssemblyContext");
        }

        public TPlugin GetPlugin(string pluginName)
        {
            if (_plugins.ContainsKey(pluginName))
                return _plugins[pluginName];
            else
                return null;
        }

        private void RegisterPlugin(TPlugin pluginInstance)
        {
            string name = pluginInstance.name;
            if (!_plugins.ContainsKey(name))
                _plugins.Add(name, pluginInstance);
        }

        public IReadOnlyCollection<TPlugin> GetPlugins()
        {
            return _plugins.Values;
        }

        public void LoadPlugins(IReadOnlyCollection<string> assembliesWithPlugins)
        {
            foreach (var assemblyPath in assembliesWithPlugins)
            {
                var assembly = _pluginAssemblyLoadingContext.LoadFromAssemblyPath(assemblyPath);
                var validPluginTypes = PluginFinder<TPlugin>.GetPluginTypes(assembly);
                foreach (var pluginType in validPluginTypes)
                {
                    var pluginInstance = (TPlugin)Activator.CreateInstance(pluginType);
                    pluginInstance.path = assemblyPath;
                    RegisterPlugin(pluginInstance);
                }
            }
        }

        public void Unload()
        {
            _plugins.Clear();
            _pluginAssemblyLoadingContext.Unload();
        }
    }
}
