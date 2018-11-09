using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Grouper2
{
    public class ParsedInf : Dictionary<string, Dictionary<string, string[]>>
    {
        //WHAT AM I DOIN HERE?
    }


    public class ParsedXml : Dictionary<string, Dictionary<string, string[]>>
    {
        //WHAT AM I DOIN HERE EITHER?
    }

    class Parsers
    {
        

        static public ParsedInf ParseInf(string infFile)
        {
            //define what a heading looks like
            Regex headingRegex = new Regex(@"^\[(\w+\s?)+\]$");
            string infRaw = File.ReadAllText(infFile);
            string[] infContent = File.ReadAllLines(infFile);
            var headingLines = new List<int>();

            //find all the lines that look like a heading
            int i = 0;
            foreach (string infLine in infContent)
            {
                Match headingMatch = headingRegex.Match(infLine);
                if (headingMatch.Success)
                {
                    headingLines.Add(i);
                    //Console.Write("Match at: ");
                    //Console.WriteLine(i.ToString());
                    //Console.WriteLine(infLine);
                }
                i++;
            }
            // make a dictionary with K/V = start/end of each section
            Dictionary<int, int> sectionSlices = new Dictionary<int, int>();
            int fuck = 0;
            while (true)
            {
                try
                {
                    int sectionHeading = headingLines[fuck];
                    int sectionFinalLine = (headingLines[(fuck + 1)] - 1);
                    sectionSlices.Add(sectionHeading, sectionFinalLine);
                    fuck++;
                }
                catch
                {
                    int sectionHeading = headingLines[fuck];
                    int sectionFinalLine = (infContent.Length - 1);
                    sectionSlices.Add(sectionHeading, sectionFinalLine);
                    break;
                }
            }
            
            // define dict that we're going to put all this in and return at the end
            ParsedInf infResults = new ParsedInf();

            // iterate over the identified sections and get the heading and contents of each.
            foreach (KeyValuePair<int, int> sectionSlice in sectionSlices)
            {
                try
                {
                    //get the section heading
                    string sectionHeading = infContent[sectionSlice.Key];
                    //get the line where the section content starts by adding one to the heading's line
                    int startSection = (sectionSlice.Key + 1);
                    //get the end line of the section
                    int nextSection = sectionSlice.Value;
                    //subtract one from the other to get the section length, without the heading.
                    int sectionLength = (nextSection - startSection);
                    //get an arraysegment with the lines
                    ArraySegment<string> sectionContent = new ArraySegment<string>(infContent, startSection, sectionLength);
                    //Console.WriteLine("This section contains: ");               
                    //Utility.PrintIndexAndValues(sectionContent);
                    //create the dictionary that we're going to put the lines into.
                    Dictionary<string, string[]> SectionDict = new Dictionary<string, string[]>();
                    //iterate over the lines in the section
                    
                    for (int b = sectionContent.Offset; b < (sectionContent.Offset + sectionContent.Count); b++)
                        {
                        string line = sectionContent.Array[b];
                        // split the line into the key (before the =) and the values (after it)
                        string[] SplitLine = line.Split('=');
                        string LineKey = (SplitLine[0]).Trim();
                        // then get the values
                        string LineValues = (SplitLine[1]).Trim();
                        // and split them into an array on ","
                        string[] SplitValues = LineValues.Split(',');
                        //Add the restructured line into the dictionary.
                       SectionDict.Add(LineKey, SplitValues);
                    }
                    //put the results into the dictionary we're gonna return
                    infResults.Add(sectionHeading, SectionDict);
                }
                catch
                {
                    Utility.WriteColor("Pooped 'em", ConsoleColor.Red, ConsoleColor.Black);
                }
            }
            return infResults;
        }

        

        static public ParsedXml ParseXml(string xmlFile)
        {
            return null;
        }
    }

    
}