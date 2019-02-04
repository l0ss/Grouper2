using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Xml;
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
            //Utility.DebugWrite(parsedScriptsIniJson.ToString());
            return parsedScriptsIniJson;
        }

        public static JObject ParseInf(string infFile)
        {
            //define what a heading looks like
            Regex headingRegex = new Regex(@"^\[(\w+\s?)+\]$");

            string[] infContentArray = File.ReadAllLines(infFile);

            string infContentString = String.Join(Environment.NewLine, infContentArray);

            if (Utility.IsEmptyOrWhiteSpace(infContentString))
            {
                return null;
            }

            var headingLines = new List<int>();

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
                    //Utility.DebugWrite(e.ToString());
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
                //create the dictionary that we're going to put the lines into.
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
                        //Add the restructured line into the dictionary.
                        JArray splitValuesJArray = new JArray();
                        foreach (string thing in splitValues) splitValuesJArray.Add(thing);

                        if (splitValuesJArray.Count == 1)
                        {
                            section.Add(lineKey, splitValues[0]);
                        }
                        else
                        {
                            section.Add(lineKey, splitValuesJArray);
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
                            section.Add(lineKey, splitValuesJArray);
                        }
                    }

                    if (lineKey == "")
                    {
                        Utility.DebugWrite("Something has gone wrong parsing an Inf/Ini file.");
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

        public static JObject ParseAasFile(string aasFile)
        {
            byte[] aasBytes = File.ReadAllBytes(aasFile);
            string aasString = File.ReadAllText(aasFile);

            //Utility.DebugWrite(aasString);

            // guid regex
            // [{]?[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[}]


            // regex to find guids in the file
            string guidRegExPattern = @"[{]?[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[}]";
            Regex guidRegex = new Regex(guidRegExPattern);
            // find them
            MatchCollection aasGuids = guidRegex.Matches(aasString);
            // first one is the product Code, second and third are revision number, 4th is upgrade code.
            string productCode = aasGuids[0].Value;
            string revisionNumber = aasGuids[2].ToString();
            string upgradeCode = aasGuids[3].ToString();

            // product Name appears at offset 100, figure out how long it is by looking for null byte.
            int prodNameLength = 0;
            foreach (byte b in aasBytes.Skip(100))
            {
                if (b == 0x00)
                {
                    break;
                }
                else prodNameLength++;
            }

            byte[] prodNameBytes = new byte[prodNameLength - 1];
            // then actually get the thing.
            Array.Copy(aasBytes, 100, prodNameBytes, 0, (prodNameLength - 1));

            string productName = Encoding.UTF8.GetString(prodNameBytes, 0, prodNameBytes.Length);

            // file name starts 2 bytes after product name
            int fileNameOffset = 100 + prodNameLength + 1;
            // repeat process for that.
            int fileNameLength = 0;
            foreach (byte b in aasBytes.Skip(fileNameOffset))
            {
                if (b == 0x00)
                {
                    break;
                }
                else fileNameLength++;
            }

            byte[] fileNameBytes = new byte[fileNameLength];
            // then actually get the thing.
            Array.Copy(aasBytes, fileNameOffset, fileNameBytes, 0, fileNameLength);

            string fileName = Encoding.UTF8.GetString(fileNameBytes, 0, fileNameBytes.Length);

            byte[] upgradeCodeBytes = Encoding.UTF8.GetBytes(upgradeCode);

            int upgradeCodeOffset = new int();

            if ((upgradeCodeBytes != null) && (aasBytes.Length >= upgradeCodeBytes.Length))
            {
                for (int l = 0; l < aasBytes.Length - upgradeCodeBytes.Length + 1; l++)
                {
                    if (!upgradeCodeBytes.Where((data, index) => !aasBytes[l + index].Equals(data)).Any())
                    {
                        upgradeCodeOffset = l;
                    }
                }
            }

            int filePathOffset = (upgradeCodeOffset + 66);
            int filePathLength = 0;
            foreach (byte b in aasBytes.Skip(filePathOffset))
            {
                if (b == 0x00)
                {
                    break;
                }
                else filePathLength++;
            }

            byte[] filePathBytes = new byte[filePathLength - 2];
            Array.Copy(aasBytes, filePathOffset, filePathBytes, 0, filePathLength - 2);
            string filePath = Encoding.UTF8.GetString(filePathBytes, 0, filePathBytes.Length);

            string msiPath = Path.Combine(filePath, fileName);

            JObject parsedAasFile = new JObject(
                new JProperty("Package Name", productName),
                new JProperty("MSI Path", msiPath),
                new JProperty("AAS Path", aasFile),
                new JProperty("ProductCode", productCode),
                new JProperty("Revision Number", revisionNumber),
                new JProperty("Upgrade Code", upgradeCode)
                );

            return parsedAasFile;
        }
    }
}
 