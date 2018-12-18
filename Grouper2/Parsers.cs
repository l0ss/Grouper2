using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Xml;
using Newtonsoft.Json;

namespace Grouper2
{
    class Parsers
    {
        public static JObject ParseInf(string infFile)
        {
            //define what a heading looks like
            Regex headingRegex = new Regex(@"^\[(\w+\s?)+\]$");
            string[] infContent = File.ReadAllLines(infFile);
            var headingLines = new List<int>();

            //find all the lines that look like a heading and put the line numbers in an array.
            int i = 0;
            foreach (string infLine in infContent)
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
            Dictionary<string, Dictionary<string, string[]>> infResults = new Dictionary<string, Dictionary<string, string[]>>();

            // iterate over the identified sections and get the heading and contents of each.
            foreach (KeyValuePair<int, int> sectionSlice in sectionSlices)
            {
                //get the section heading
                char[] squareBrackets = { '[', ']' };
                string sectionSliceKey = infContent[sectionSlice.Key];
                string sectionHeading = sectionSliceKey.Trim(squareBrackets);
                //get the line where the section content starts by adding one to the heading's line
                int startSection = (sectionSlice.Key + 1);
                //get the end line of the section
                int nextSection = sectionSlice.Value;
                //subtract one from the other to get the section length, without the heading.
                int sectionLength = (nextSection - startSection);
                //get an array segment with the lines
                ArraySegment<string> sectionContent = new ArraySegment<string>(infContent, startSection, sectionLength);
                //Console.WriteLine("This section contains: ");               
                //Utility.PrintIndexAndValues(sectionContent);
                //create the dictionary that we're going to put the lines into.
                Dictionary<string, string[]> sectionDict = new Dictionary<string, string[]>();
                //iterate over the lines in the section
                
                for (int b = sectionContent.Offset; b < (sectionContent.Offset + sectionContent.Count); b++)
                    {
                    string line = sectionContent.Array[b];
                    // split the line into the key (before the =) and the values (after it)
                    string[] splitLine = line.Split('=');
                    string lineKey = (splitLine[0]).Trim();
                    // then get the values
                    string lineValues = (splitLine[1]).Trim();
                    // and split them into an array on ","
                    string[] splitValues = lineValues.Split(',');
                    //Add the restructured line into the dictionary.
                   sectionDict.Add(lineKey, splitValues);
                }
                //put the results into the dictionary we're gonna return
                infResults.Add(sectionHeading, sectionDict);
            }
            JObject infResultsJson = (JObject)JToken.FromObject(infResults);
            return infResultsJson;
        }

        public static JObject ParseGppXmlToJson(string xmlFile)
        {
            //grab the contents of the file path in the argument
            string rawXmlFileContent = File.ReadAllText(xmlFile);
            //create an xml object
            XmlDocument xmlFileContent = new XmlDocument();
            //put the file contents in the object
            xmlFileContent.LoadXml(rawXmlFileContent);
            // turn the Xml into Json
            string jsonFromXml = JsonConvert.SerializeXmlNode(xmlFileContent.DocumentElement, Newtonsoft.Json.Formatting.Indented);
            // debug write the json
            //Console.WriteLine(JsonFromXml);
            // put the json into a JObject
            JObject parsedXmlFileToJson = JObject.Parse(jsonFromXml);
            // return the JObject
            return parsedXmlFileToJson;
        }
    }
}