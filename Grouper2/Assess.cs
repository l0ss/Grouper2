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
            JArray IntPrivRights = (JArray)JsonData["privRights"]["item"];
            JArray WellKnownSIDS = (JArray)JsonData["trustees"]["item"];
            JArray IntRegKeys = (JArray)JsonData["regKeys"]["item"];

            // an array for GPTmpl headings to ignore.
            string[] KnownKeys = new string[]
            {
                "[Unicode]",
                "[Version]"
            };


            /*
            // Checks for interesting priv assignments.
            if (InfToAssess.ContainsKey("[Privilege Rights]")) {
                KnownKeys.Add("[Privilege Rights]");
                Dictionary<string, string[]> PrivRights = InfToAssess["[Privilege Rights]"];
                //look at each value
                foreach (KeyValuePair<string, string[]> PrivRight in PrivRights)
                {
                    foreach (JToken IntPrivRight in IntPrivRights)
                    {
                        // if the priv is interesting
                        if ((string)IntPrivRight["privRight"] == PrivRight.Key)
                        {
                            // tell us it's interesting
                            Console.WriteLine("Interesting privilege " + PrivRight.Key + " is granted to:");
                            //then for each trustee it's granted to
                            foreach (string trustee in PrivRight.Value)
                            {
                                // clean up the trustee SID
                                string TrusteeClean = trustee.Trim('*'); 
                                // iterate over the list of well known sids
                                foreach (JToken WellKnownSID in WellKnownSIDS)
                                {
                                    bool SIDmatches = false;

                                    string SIDToMatch = (string)WellKnownSID["SID"];
                                    
                                    //Utility.DebugWrite("Comparing trustee : " + TrusteeClean);
                                    //Utility.DebugWrite("to WellKnownSID : " + (string)WellKnownSID["SID"]);
                                    if ((SIDToMatch.Contains("DOMAIN")) && (TrusteeClean.Length >= 14))
                                    {
                                        string[] TrusteeSplit = trustee.Split("-".ToCharArray());
                                        string[] WKSIDSplit = SIDToMatch.Split("-".ToCharArray());

                                        Utility.DebugWrite("tsplit " + TrusteeSplit[TrusteeSplit.Length - 1]);
                                        Utility.DebugWrite("wksidsplit " + WKSIDSplit[WKSIDSplit.Length - 1]);

                                        //TODO - needs to add some well known user sids to test data.
                                        if (TrusteeSplit[TrusteeSplit.Length -1] == WKSIDSplit[WKSIDSplit.Length - 1])
                                        {
                                            Console.WriteLine("VeryMatchy");
                                            SIDmatches = true;
                                            // NFI if this works in the absence of suitable test data.
                                        }

                                        Console.WriteLine("matchymatchy");
                                    }

                                    // short sids are going to be ~12 chars long.

                                    // check if we have a direct match
                                    if ((string)WellKnownSID["SID"] == TrusteeClean)
                                    {
                                        SIDmatches = true;
                                    }

                                    if (SIDmatches == true)
                                    {
                                        Console.WriteLine("Display Name : " + (string)WellKnownSID["displayName"]);
                                        Console.WriteLine("SID Matched : " + TrusteeClean);
                                        Console.WriteLine("");
                                    }
                                }
                            }
                        }
                    }
                }
            }*/

            /*
            if (InfToAssess.ContainsKey("[Registry Values]")) {
                KnownKeys.Add("[Registry Values]");
                JArray MatchedRegKeys = new JArray();

                Dictionary<string, string[]> RegVals = InfToAssess["[Registry Values]"];
                foreach (KeyValuePair<string, string[]> RegVal in RegVals)
                {
                    
                    // iterate over the list of interesting keys in our json "db".
                    foreach (JToken IntRegKey in IntRegKeys)
                    {
                        // if it matches
                        if ((string)IntRegKey["regKey"] == RegVal.Key)
                        {
                            // add our match to the JArray we created
                            MatchedRegKeys.Add(IntRegKey);
                            // print the shit out
                            Console.WriteLine("Check out this reg key:");
                            Console.WriteLine(RegVal.Key);
                            Console.WriteLine("With these values");
                            foreach (string thing in RegVal.Value)
                            {
                                Console.WriteLine(thing);
                            }
                        }
                    }

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
            }*/

            /*
            //TODO
            if (InfToAssess.ContainsKey("[System Access]"))
            {
                KnownKeys.Add("[System Access]");
                Dictionary<string, string[]> RegVals = InfToAssess["[System Access]"];
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
            }
            */

            /*
            //TODO
            if (InfToAssess.ContainsKey("[Registry Keys]"))
            {
                KnownKeys.Add("[Registry Keys]");
                Dictionary<string, string[]> RegVals = InfToAssess["[Registry Keys]"];
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
            }
            */

            /*
            // TODO
            if (InfToAssess.ContainsKey("[Group Membership]"))
            {
                KnownKeys.Add("[Group Membership]");
                Dictionary<string, string[]> RegVals = InfToAssess["[Group Membership]"];
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
            }
            */

            /*
           // TODO
           if (InfToAssess.ContainsKey("[Service General Setting]"))
           {
               KnownKeys.Add("[Service General Setting]");
               Dictionary<string, string[]> RegVals = InfToAssess["[Service General Setting]"];
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
           }
           */

            //catch any stuff that falls through the cracks
            string[] KeysInInf = (InfToAssess.Keys.ToArray());

            //  System Access +
            //  Kerberos Policy -
            //  Event Audit -
            //  Registry Values +
            //  Registry Keys +
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