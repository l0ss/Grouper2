﻿using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Xml;
using Grouper2.Utility;
using Newtonsoft.Json;

namespace Grouper2
{
    class Parsers
    {
        public static JObject ParseScriptsIniJson(JObject scriptsIniJson)
        {
            // take the partially parsed Ini File
            // create an object for us to put output into
            JObject parsedScriptsIniJson = new JObject();

            // iterate over the types of script (e.g. startup, shutdown, logon, etc)
            foreach (KeyValuePair<string, JToken> item in scriptsIniJson)
            {
                //get the type of the script into a string for output.
                string scriptType = item.Key;
                // cast the settings from JToken to JObject.
                JObject settingsJObject = (JObject)item.Value;

                // each script has a numeric index at the beginning of each of its settings. first we need to figure out how many of these there are.
                int maxIndex = 0;
                foreach (KeyValuePair<string, JToken> setting in settingsJObject)
                {
                    string index = setting.Key.Substring(0, 1);
                    int indexInt = Convert.ToInt32(index);
                    if (maxIndex < indexInt)
                    {
                        maxIndex = indexInt;
                    }
                }
                
                JObject settingsWithIndexes =new JObject();
               
                foreach (int i in Enumerable.Range(0, (maxIndex+1)))
                {
                    string iString = i.ToString();
                    
                    JObject parsedSettings = new JObject();
                    foreach (KeyValuePair<string, JToken> thing in settingsJObject)
                    {
                        string index = thing.Key.Substring(0, 1);
                        string settingName = thing.Key.Substring(1);
                        JToken settingValue = thing.Value;
                        int indexInt = Convert.ToInt32(index);
                        // if the line starts with the int we're currently indexing off, add line setting to Dict.
                        if (indexInt == i)
                        {
                            parsedSettings.Add(settingName, settingValue);
                        }
                    }
                    settingsWithIndexes.Add(iString, parsedSettings);
                }
                
                // put it in a jprop and add it to the output jobj
                JProperty parsedItemJProp = new JProperty(scriptType, settingsWithIndexes);
                parsedScriptsIniJson.Add(parsedItemJProp);
            }
            //Console.WriteLine("return: ");
            //Utility.Output.DebugWrite(parsedScriptsIniJson.ToString());
            return parsedScriptsIniJson;
        }

        public static JObject ParseInf(string infFile)
        {
            //define what a heading looks like
            Regex headingRegex = new Regex(@"^\[(\w+\s?)+\]$");

            string[] infContentArray = File.ReadAllLines(infFile);

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
                    int sectionFinalLine = (headingLines[(fuck + 1)] - 1);
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
                int firstLineOfSection = (sectionSlice.Key + 1);
                //get the first line of the next section
                int lastLineOfSection = sectionSlice.Value;
                //subtract one from the other to get the section length, without the heading.
                int sectionLength = (lastLineOfSection - firstLineOfSection + 1);
                //get an array segment with the lines

                //if (sectionLength == 0) break;

                ArraySegment<string> sectionContent = new ArraySegment<string>(infContentArray, firstLineOfSection, sectionLength);
                //Console.WriteLine("This section contains: ");               
                //Utility.PrintIndexAndValues(sectionContent);
                //create the jobject that we're going to put the lines into.
                JObject section = new JObject();
                //iterate over the lines in the section
                for (int b = sectionContent.Offset; b < (sectionContent.Offset + sectionContent.Count); b++)
                {
                    string line = sectionContent.Array[b];
                    if (line.Trim() == "") break;
                    // split the line into the key (before the =) and the values (after it)
                    string lineKey = "";

                    
                    if (line.Contains('='))
                    {
                        string[] splitLine = line.Split('=');
                        lineKey = (splitLine[0]).Trim();
                        lineKey = lineKey.Trim('\\','"');
                        // then get the values
                        string lineValues = (splitLine[1]).Trim();
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
                        lineKey = (splitLine[0]).Trim();
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

        public static JObject ParseGppXmlToJson(string xmlFile)
        {
            //grab the contents of the file path in the argument
            string rawXmlFileContent = "";
            try
            {
                rawXmlFileContent = File.ReadAllText(xmlFile);
            }
            catch (UnauthorizedAccessException e)
            {
                Utility.Output.DebugWrite(e.ToString());
                return null;
            }

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

        public static JObject ParseRegistryPol(string registryFile)
        {
            return RegistryPolParser.Read(registryFile);
        }
    }
}
 