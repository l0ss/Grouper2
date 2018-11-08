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

            //Console.WriteLine("");
            //Console.WriteLine("Contents of File:");

            //Console.Write(infRaw);
            //Console.WriteLine("");

            //Console.WriteLine("Sections Identified: ");

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

            // debug shit, can del.
            /*
            Console.WriteLine("Debug content of sectionSlices");
            foreach (KeyValuePair<int, int> kvp in sectionSlices)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }*/

            // define dict that we're going to put all this in and return at the end
            ParsedInf infResults = new ParsedInf();

            // iterate over the identified sections and get the heading and contents of each.
            foreach (KeyValuePair<int, int> sectionSlice in sectionSlices)
            {
                try
                {
                    //get the section heading
                    string sectionHeading = infContent[sectionSlice.Key];
                    //Utility.DebugWrite("This section's heading is:" + sectionHeading);
                    //get the line where the section content starts by adding one to the heading's line
                    int startSection = (sectionSlice.Key + 1);
                    //Console.WriteLine("This section starts at: " + startSection);
                    //get the end line of the section
                    int nextSection = sectionSlice.Value;
                    //Console.WriteLine("The next section starts at: " + nextSection);
                    //subtract one from the other to get the section length, without the heading.
                    int sectionLength = (nextSection - startSection);
                    //Console.WriteLine("This section is " + sectionLength + " lines long.");
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
                        // debug writes
                        //Console.WriteLine("LineKey = " + LineKey);
                        // then get the values
                        string LineValues = (SplitLine[1]).Trim();
                        // and split them into an array on ","
                        string[] SplitValues = LineValues.Split(',');
                        // debug writes
                        /*
                        foreach (string SplitValue in SplitValues)
                        {
                            Console.WriteLine("SplitValue = " + SplitValue);
                        }
                        */
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
    }
}