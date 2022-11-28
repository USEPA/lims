using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

using PluginBase;




namespace ACESD_Fecal_Coliform
{
    public class ACESD_Fecal_Coliform : DataProcessor
    {
        public override string id { get => "acesd_fecal_coliform1.0"; }
        public override string name { get => "ACESD_Fecal_Coliform"; }
        public override string description { get => "Processor used for ACESD_Fecal_Coliform translation to universal template"; }
        public override string file_type { get => ".pdf"; }
        public override string version { get => "1.0"; }
        public override string? input_file { get; set; }
        public override string? path { get; set; }

        public override DataTableResponseMessage Execute()
        {
            DataTableResponseMessage rm = null;
            DataTable? dt = null;
            try
            {
                rm = VerifyInputFile();
                if (!rm.IsValid)
                    return rm;

                dt = GetDataTable();
                FileInfo fi = new FileInfo(input_file);
                dt.TableName = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);

                PdfReader? reader = null;

                string tableHeaderRowStart = "PARAMETER";
                string tableDataRowStart = "Fecal";

                // parameters to be mapped
                // order is important!
                string[] parameters = new string[] {
                    "Sample Number",
                    "Sample Description",
                    "Sample Type",
                    "Sample Date / Time",
                    "PARAMETER",
                    "RESULTS",
                    "UNITS",
                    "METHOD"
                };

                // the header is read in in this order from the PDF
                // PARAMETER RESULTS LIMIT UNITS METHOD ANALYZED ANALYST
                // the data row is read in in this (incorrect) order from the PDF 
                // |Fecal Coliform (MF)||KAW||4/22/2021||SM9222D 19-21ed||CFU/100 ml||1||50||16:02|
                // the last element is discarded

                // this is the index of the corresponding value in relation to the header
                int[] pdfDataRowPropertyIndex = new int[] { 0, 6, 5, 4, 3, 2, 1 };

                List<string> extractedDataRows = new List<string>();

                string dataTableHeader = "";

                Dictionary<string, Dictionary<string, string>> outputData = new Dictionary<string, Dictionary<string, string>>();

                // read in input file
                try
                {
                    reader = new PdfReader(input_file);
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Problem creating PDF reader for input file {0}.", input_file);
                    errorMsg = errorMsg + Environment.NewLine;
                    errorMsg = errorMsg + ex.Message;
                    rm.ErrorMessage = errorMsg;
                }

                // parse PDF pages
                if (reader != null)
                {
                    try
                    {
                        // pages are numberd 1 -> n
                        for (var i = 0; i < reader.NumberOfPages; i++)
                        {
                            // get the body of the PDF
                            // this returns everything useable EXCEPT for the table data row,
                            // which has too many spaces to efficiently split
                            string pdfBody = PdfTextExtractor.GetTextFromPage(reader, i + 1);
                            string[] pdfLines = pdfBody.Split("\n");
                            string currentSample = "";
                            foreach (string line in pdfLines)
                            {
                                // line with "Sample Number" indicates a new record
                                if (line.StartsWith(parameters[0]))
                                {
                                    string sampleNumber = line.Split(":")[1];
                                    currentSample = sampleNumber;
                                    outputData.Add(sampleNumber, new Dictionary<string, string>());
                                }
                                foreach (string param in parameters)
                                {
                                    if (line.StartsWith(param))
                                    {
                                        if (line.StartsWith(tableHeaderRowStart))
                                        {
                                            dataTableHeader = line;
                                        }
                                        else
                                        {
                                            string[] sampleValue = line.Split(":");
                                            if (sampleValue.Length == 2)
                                            {
                                                outputData[currentSample].Add(sampleValue[0].Trim(), sampleValue[1]);
                                            }
                                            // the split splits the seconds off of the time so add them back on
                                            else if (sampleValue.Length == 3)
                                            {
                                                outputData[currentSample].Add(sampleValue[0].Trim(), sampleValue[1] + ":" + sampleValue[2]);
                                            }
                                        }
                                    }
                                }
                            }

                            // extract the table data row in a useable form
                            var lineText = LineUsingCoordinates.getLineText(input_file, i + 1);
                            foreach (var row in lineText)
                            {
                                if (row.Count > 1 && row[0].Contains(tableDataRowStart))
                                {
                                    string dataRow = "";
                                    for (var col = 0; col < row.Count; col++)
                                    {
                                        string trimmedValue = row[col].Trim();
                                        if (trimmedValue != "")
                                        {
                                            dataRow += "|" + trimmedValue + "|";
                                        }
                                    }
                                    extractedDataRows.Add(dataRow);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = string.Format("Problem parsing input file {0}.", input_file);
                        errorMsg = errorMsg + Environment.NewLine;
                        errorMsg = errorMsg + ex.Message;
                        rm.ErrorMessage = errorMsg;
                    }
                }

                // add the table data row to the dict
                string[] headerFields = dataTableHeader.Split(" ");
                for (int i = 0; i < extractedDataRows.Count; i++)
                {
                    string line = extractedDataRows[i];
                    line = line.TrimStart('|');
                    line = line.TrimEnd('|');
                    string[] fieldValues = line.Split("||");
                    for (int j = 0; j < headerFields.Length; j++)
                    {
                        outputData[outputData.ElementAt(i).Key].Add(headerFields[j], fieldValues[pdfDataRowPropertyIndex[j]]);
                    }
                }

                // convert to universal template
                for(int i = 0; i < outputData.Count; i++)
                {
                    Dictionary<string, string> record = outputData[outputData.ElementAt(i).Key];
                    DataRow dr = dt.NewRow();
                    dr["Aliquot"] = record["Sample Description"];
                    dr["Analyte Identifier"] = record["Sample Number"];
                    dr["Analysis Date/Time"] = record["Sample Date / Time"];
                    dr["Measured Value"] = record["RESULTS"];
                    dr["Units"] = record["UNITS"];
                    dr["User Defined 1"] = record["Sample Type"];
                    dr["User Defined 2"] = record["PARAMETER"];
                    dr["User Defined 3"] = record["METHOD"];

                    dt.Rows.Add(dr);
                }

            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Problem executing processor {0} on input file {1}.", name, input_file);
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + ex.Message;
                errorMsg = errorMsg + Environment.NewLine;
                errorMsg = errorMsg + string.Format("Error occurred on row: {0}", current_row + 1);
                rm.ErrorMessage = errorMsg;
            }

            rm.TemplateData = dt;

            return rm;
        }
    }

    // below classes are used to extract table type data | used here to get the table's data row only
    public class MyLocationTextExtractionStrategy : LocationTextExtractionStrategy
    {
        public List<RectAndText> myPoints = new List<RectAndText>();

        //Automatically called for each chunk of text in the PDF
        public override void RenderText(TextRenderInfo renderInfo)
        {
            base.RenderText(renderInfo);

            //Get the bounding box for the chunk of text
            var bottomLeft = renderInfo.GetDescentLine().GetStartPoint();
            var topRight = renderInfo.GetAscentLine().GetEndPoint();

            //Create a rectangle from it
            var rect = new iTextSharp.text.Rectangle(
                                                    bottomLeft[Vector.I1],
                                                    bottomLeft[Vector.I2],
                                                    topRight[Vector.I1],
                                                    topRight[Vector.I2]
                                                    );

            //Add this to our main collection
            this.myPoints.Add(new RectAndText(rect, renderInfo.GetText()));
        }
    }
    public class RectAndText
    {
        public iTextSharp.text.Rectangle Rect;
        public String Text;
        public RectAndText(iTextSharp.text.Rectangle rect, String text)
        {
            this.Rect = rect;
            this.Text = text;
        }

    }
    class LineUsingCoordinates
    {
        public static List<List<string>> getLineText(string path, int page)
        {
            //Create an instance of our strategy
            var t = new MyLocationTextExtractionStrategy();

            //Parse page 1 of the document above
            using (var r = new PdfReader(path))
            {
                var ex = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(r, page, t);
            }
            // List of columns in one line
            List<string> lineWord = new List<string>();
            // temporary list for working around appending the <List<List<string>>
            List<string> tempWord;
            // List of rows. rows are list of string
            List<List<string>> lineText = new List<List<string>>();
            // List consisting list of chunks related to each line
            List<List<RectAndText>> lineChunksList = new List<List<RectAndText>>();
            //List consisting the chunks for whole page;
            List<RectAndText> chunksList;
            // List consisting the list of Bottom coord of the lines present in the page 
            List<float> bottomPointList = new List<float>();

            //Getting List of Coordinates of Lines in the page no matter it's a table or not
            foreach (var i in t.myPoints)
            {
                // If the coords passed to the function is not null then process the part in the 
                // given coords of the page otherwise process the whole page
                float bottom = i.Rect.Bottom;
                if (bottomPointList.Count == 0)
                {
                    bottomPointList.Add(bottom);
                }
                else if (Math.Abs(bottomPointList.Last() - bottom) > 3)
                {
                    bottomPointList.Add(bottom);
                }
            }

            // Sometimes the above List will be having some elements which are from the same line but are
            // having different coordinates due to some characters like " ",".",etc.
            // And these coordinates will be having the difference of at most 4 points between 
            // their bottom coordinates. 

            //so to remove those elements we create two new lists which we need to remove from the original list 

            //This list will be having the elements which are having different but a little difference in coordinates 
            List<float> removeList = new List<float>();
            // This list is having the elements which are having the same coordinates
            List<float> sameList = new List<float>();

            // Here we are adding the elements in those two lists to remove the elements
            // from the original list later
            for (var i = 0; i < bottomPointList.Count; i++)
            {
                var basePoint = bottomPointList[i];
                for (var j = i + 1; j < bottomPointList.Count; j++)
                {
                    var comparePoint = bottomPointList[j];
                    //here we are getting the elements with same coordinates
                    if (Math.Abs(comparePoint - basePoint) == 0)
                    {
                        sameList.Add(comparePoint);
                    }
                    // here ae are getting the elements which are having different but the diference
                    // of less than 4 points
                    else if (Math.Abs(comparePoint - basePoint) < 4)
                    {
                        removeList.Add(comparePoint);
                    }
                }
            }

            // Here we are removing the matching elements of remove list from the original list 
            bottomPointList = bottomPointList.Where(item => !removeList.Contains(item)).ToList();

            //Here we are removing the first matching element of same list from the original list
            foreach (var r in sameList)
            {
                bottomPointList.Remove(r);
            }

            // Here we are getting the characters of the same line in a List 'chunkList'.
            foreach (var bottomPoint in bottomPointList)
            {
                chunksList = new List<RectAndText>();
                for (int i = 0; i < t.myPoints.Count; i++)
                {
                    // If the character is having same bottom coord then add it to chunkList
                    if (bottomPoint == t.myPoints[i].Rect.Bottom)
                    {
                        chunksList.Add(t.myPoints[i]);
                    }
                    // If character is having a difference of less than 3 in the bottom coord then also
                    // add it to chunkList because the coord of the next line will differ at least 10 points
                    // from the coord of current line
                    else if (Math.Abs(t.myPoints[i].Rect.Bottom - bottomPoint) < 3)
                    {
                        chunksList.Add(t.myPoints[i]);
                    }
                }
                // Here we are adding the chunkList related to each line
                lineChunksList.Add(chunksList);
            }

            //Here we are looping through the lines consisting the chunks related to each line 
            foreach (var linechunk in lineChunksList)
            {
                var text = "";
                // Here we are looping through the chunks of the specific line to put the texts
                // that are having a cord jump in their left coordinates.
                // because only the line having table will be having the coord jumps in their 
                // left coord not the line having texts
                for (var i = 0; i < linechunk.Count - 1; i++)
                {
                    // If the coord is having a jump of less than 3 points then it will be in the same
                    // column otherwise the next chunk belongs to different column
                    if (Math.Abs(linechunk[i].Rect.Right - linechunk[i + 1].Rect.Left) < 3)
                    {
                        if (i == linechunk.Count - 2)
                        {
                            text += linechunk[i].Text + linechunk[i + 1].Text;
                        }
                        else
                        {
                            text += linechunk[i].Text;
                        }
                    }
                    else
                    {
                        if (i == linechunk.Count - 2)
                        {
                            // add the text to the column and set the value of next column to ""
                            text += linechunk[i].Text;
                            // this is the list of columns in other word its the row
                            lineWord.Add(text);
                            text = "";
                            text += linechunk[i + 1].Text;
                            lineWord.Add(text);
                            text = "";
                        }
                        else
                        {
                            text += linechunk[i].Text;
                            lineWord.Add(text);
                            text = "";
                        }
                    }
                }
                if (text.Trim() != "")
                {
                    lineWord.Add(text);
                }
                // creating a temporary list of strings for the List<List<string>> manipulation
                tempWord = new List<string>();
                tempWord.AddRange(lineWord);
                // "lineText" is the type of List<List<string>>
                // this is our list of rows. and rows are List of strings
                // here we are adding the row to the list of rows
                lineText.Add(tempWord);
                lineWord.Clear();
            }
            return lineText;
        }
    }
}