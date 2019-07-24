using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Host.SysVol.Files
{
    public class InfSysvolFile : SysvolFile
    {
        public InfSysvolFile(string path) : base(path)
        {
            this.Type = SysvolObjectType.InfFile;
        }

        public JObject ParseAsJsonObject()
        {
            //define what a heading looks like
            Regex headingRegex = new Regex(@"^\[(\w+\s?)+\]$");

            string[] infContentArray = File.ReadAllLines(this.Path);

            string infContentString = String.Join(Environment.NewLine, infContentArray);

            if (Util.IsEmptyOrWhiteSpace(infContentString))
            {
                return null;
            }

            List<int> headingLines = new List<int>();

            //find all the lines that look like a heading and put the line numbers in an array.
            int i = 0;
            foreach (string infLine in infContentArray)
            {
                Match headingMatch = headingRegex.Match(infLine);
                if (headingMatch.Success)
                {
                    headingLines.Add(i);
                }
                i++;
            }
            // make a dictionary with K/V = start/end of each section
            // this is extraordinarily janky but it works mostly.
            Dictionary<int, int> sectionSlices = new Dictionary<int, int>();
            int fuck = 0;
            while (true)
            {
                try
                {
                    int sectionHeading = headingLines[fuck];
                    int sectionFinalLine = headingLines[fuck + 1] - 1;
                    sectionSlices.Add(sectionHeading, sectionFinalLine);
                    fuck++;
                }
                catch (ArgumentOutOfRangeException)
                {
                    //Utility.Output.DebugWrite(e.ToString());
                    int sectionHeading = headingLines[fuck];
                    int sectionFinalLine = infContentArray.Length - 1;
                    sectionSlices.Add(sectionHeading, sectionFinalLine);
                    break;
                }
            }

            // define jobj that we're going to put all this in and return at the end
            JObject infResults = new JObject();

            // iterate over the identified sections and get the heading and contents of each.
            foreach (KeyValuePair<int, int> sectionSlice in sectionSlices)
            {
                //get the section heading
                char[] squareBrackets = { '[', ']' };
                string sectionSliceKey = infContentArray[sectionSlice.Key];
                string sectionHeading = sectionSliceKey.Trim(squareBrackets);
                //get the line where the section content starts by adding one to the heading's line
                int firstLineOfSection = sectionSlice.Key + 1;
                //get the first line of the next section
                int lastLineOfSection = sectionSlice.Value;
                //subtract one from the other to get the section length, without the heading.
                int sectionLength = lastLineOfSection - firstLineOfSection + 1;
                //get an array segment with the lines

                //if (sectionLength == 0) break;

                ArraySegment<string> sectionContent = new ArraySegment<string>(infContentArray, firstLineOfSection, sectionLength);
                //Console.WriteLine("This section contains: ");               
                //Utility.PrintIndexAndValues(sectionContent);
                //create the jobject that we're going to put the lines into.
                JObject section = new JObject();
                //iterate over the lines in the section
                for (int b = sectionContent.Offset; b < sectionContent.Offset + sectionContent.Count; b++)
                {
                    string line = sectionContent.Array[b];
                    if (line.Trim() == "") break;
                    // split the line into the key (before the =) and the values (after it)
                    string lineKey = "";


                    if (line.Contains('='))
                    {
                        string[] splitLine = line.Split('=');
                        lineKey = splitLine[0].Trim();
                        lineKey = lineKey.Trim('\\', '"');
                        // then get the values
                        string lineValues = splitLine[1].Trim();
                        // and split them into an array on ","
                        string[] splitValues = lineValues.Split(',');
                        // handle cases where we have multiple entries with the same 'key'
                        // check if we have an existing entry in 'section'
                        // if we don't.. 
                        if (section[lineKey] == null)
                        {
                            if (splitValues.Length == 1)
                            {
                                section.Add(lineKey, splitValues[0]);
                            }
                            else
                            {
                                //Add the restructured line into the jobject.
                                JArray splitValuesJArray = new JArray();
                                foreach (string thing in splitValues) splitValuesJArray.Add(thing);
                                section.Add(lineKey, splitValuesJArray);
                            }
                        }
                        // if we do...
                        else
                        {
                            // pull out our existing section
                            JArray existingSection = new JArray(section[lineKey]);
                            section.Remove(lineKey);
                            // add the new values to it
                            foreach (string splitValue in splitValues)
                            {
                                existingSection.Add(splitValue);
                            }
                            // add it back in.
                            section.Add(lineKey, existingSection);
                        }
                    }
                    else
                    {
                        string[] splitLine = line.Split(',');
                        lineKey = splitLine[0].Trim();
                        JArray splitValuesJArray = new JArray();
                        foreach (string value in splitLine)
                        {
                            if (value == splitLine[0])
                            {
                                continue;
                            }
                            else
                            {
                                splitValuesJArray.Add(value);
                            }
                        }

                        if (splitValuesJArray.Count == 1)
                        {
                            section.Add(lineKey, splitLine[0]);
                        }
                        else
                        {
                            try
                            {
                                section.Add(lineKey, splitValuesJArray);
                            }
                            catch (ArgumentException e)
                            {
                                Utility.Output.DebugWrite("Hit duplicate value in inf.");
                                Utility.Output.DebugWrite(e.ToString());
                            }
                        }
                    }

                    if (lineKey == "")
                    {
                        Utility.Output.DebugWrite("Something has gone wrong parsing an Inf/Ini file.");
                    }

                }
                //put the results into the dictionary we're gonna return
                infResults.Add(sectionHeading, section);
            }
            return infResults;
        }
    }
}