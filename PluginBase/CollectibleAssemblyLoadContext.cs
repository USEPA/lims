using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.Loader;

namespace PluginBase
{
    public class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext() : base(isCollectible: true)
        { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
