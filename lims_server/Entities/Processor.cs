using System;
using System.Collections.Generic;

namespace LimsServer.Entities
{
    public class Processor
    {
        public string instrument { get; set; }
        public DateTime date { get; set; }
        public string file_type { get; set; }
        public int data_sheet { get; set; }
        public List<Dictionary<string, string>> data_fields { get; set; }
        public Dictionary<string, string> field_mappings { get; set; }
    }

    public class Template
    {
        public static readonly string[] Fields =
        {
            "Aliquot",
            "Analyte Identifier",
            "Measured Value",
            "Units",
            "Dilution Factor",
            "Analysis Date/Time",
            "Comment",
            "Description",
            "User Defined 1",
            "User Defined 2",
            "User Defined 3",
            "User Defined 4",
            "User Defined 5",
            "User Defined 6",
            "User Defined 7",
            "User Defined 8",
            "User Defined 9",
            "User Defined 10",
            "User Defined 11",
            "User Defined 12",
            "User Defined 13",
            "User Defined 14",
            "User Defined 15",
            "User Defined 16",
            "User Defined 17",
            "User Defined 18",
            "User Defined 19",
            "User Defined 20"
        };
    }
}
