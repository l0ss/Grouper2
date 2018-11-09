using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Grouper2
{

    class Assess
    {
        static readonly string JsonDataFile = File.ReadAllText("PolData.Json");
        static readonly JObject JsonData = JObject.Parse(JsonDataFile);
        
        // Assesses the contents of a GPTmpl
        public static void AssessGPTmpl(ParsedInf InfToAssess)
        {
            var IntPrivRights =
                from r in JsonData["privRights"]["item"]
                select (string)r["searchString"];

            var IntRegKeys =
                from r in JsonData["regKeys"]["item"]
                select (string)r["regKey"];
                      
            //JArray PrivRightsJson = (JArray)JsonData["privRights"]["item"]["searchstring"];
            //Console.WriteLine(PrivRightsJson);
            
            // Checks for interesting priv assignments.
            if (InfToAssess.ContainsKey("[Privilege Rights]")) {
                Dictionary<string, string[]> privRights = InfToAssess["[Privilege Rights]"];
                //look at each value
                foreach (KeyValuePair<string, string[]> privRight in privRights)
                {
                    if (IntPrivRights.Contains(privRight.Key))
                    {
                        Console.WriteLine("Interesting privilege " + privRight.Key + " is granted to:");
                        Utility.PrintIndexAndValues(privRight.Value);
                    }
                }
            }

            if (InfToAssess.ContainsKey("[Registry Values]")) {
                Dictionary<string, string[]> RegVals = InfToAssess["[Registry Values]"];
                foreach (KeyValuePair<string, string[]> RegVal in RegVals)
                {
                    if (IntRegKeys.Contains(RegVal.Key)) {
                        string PrintKey = RegVal.Key;


                        Utility.DebugWrite("Key: ");
                        Console.WriteLine(PrintKey);
                        Utility.DebugWrite("Actual Values: ");
                        // the first value in these looks like a 'type' code.
                        // looks like they work like this:
                        // 4 = Int, but where it's 1 or 0 they use it as a bool
                        // 1 = String in double quotes, some of which are numbers
                        // 7 = Array
                        foreach (string value in RegVals[RegVal.Key])
                        {
                            Console.Write(value);
                            Console.WriteLine("");
                        }
                        
                    }
                }
            }
            

            /* skeleton to just read out all the values
            if (InfToAssess.ContainsKey("[Registry Values]"))
            {
                Dictionary<string, string[]> RegVals = InfToAssess["[Registry Values]"];
                foreach (KeyValuePair<string, string[]> RegVal in RegVals)
                {
                    string PrintKey = RegVal.Key;
                    Utility.DebugWrite("Key: ");
                    Console.WriteLine(PrintKey);
                    Utility.DebugWrite("Values: ");
                    foreach (string value in RegVals[RegVal.Key])
                    {
                        Console.WriteLine(value);
                    }
                }
            }*/

            //catch any stuff that falls through the cracks
            string[] KeysInInf = (InfToAssess.Keys.ToArray());

            string[] KnownKeys = new string[]
            {
                "[Privilege Rights]",
                "[Unicode]",
                "[Version]"
            };

            //  System Access +
            //  Kerberos Policy -
            //  Event Audit -
            //  Registry Values +
            //  Registry Keys |
            //  Group Membership +
            //  Service General Setting +


            var SlippedThrough = KeysInInf.Except(KnownKeys);

            if (SlippedThrough.Count() > 0)
            {
                Utility.DebugWrite("We didn't parse any of these sections:");
                foreach (var UnparsedHeader in SlippedThrough)
                {
                    Console.WriteLine(UnparsedHeader);
                }
            }
        }
    }
}