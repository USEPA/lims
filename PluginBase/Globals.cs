using System;
using System.Collections.Generic;
using System.Text;

namespace PluginBase
{
    public static class Globals
    {
        //These could be read from a config file and passed into the processors at run time if needed
        
        private const string _baseNetworkPath = @"\\AA\ORD\ORD\PRIV\NERL_LIMS_Pilot_TEST";
        public static string GetBasePath() { return _baseNetworkPath; }

        private const string _inputFolder = @"Instrument_Data_Files";
        public static string GetInputPath() { return _inputFolder; }

        private const string _titanDropFolder = @"Titan_Drop_Folder";
        public static string GetTitanDropPath() { return _titanDropFolder; }
    }
}
