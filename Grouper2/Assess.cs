using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Grouper2
{
    class Assess
    {
        static readonly string JsonDataFile = File.ReadAllText("PolData.Json");
        static readonly JObject JsonData = JObject.Parse(JsonDataFile);
        
        // Assesses the contents of a GPTmpl
        public static void AssessGPTmpl(JObject InfToAssess)
        {
            //Utility.DebugWrite("Entered AssessGPTmpl");
            JToken InfResult = JsonData["Output Skeleton"]["Findings"];
            Utility.DebugWrite("Here's what InfResult looks like");
            Console.WriteLine(InfResult);
            // an array for GPTmpl headings to ignore.
            List<string> KnownKeys = new List<string>
            {
                "[Unicode]",
                "[Version]"
            };

            // go through each category we care about and look for goodies.
            JToken PrivRights = InfToAssess["Privilege Rights"];
            try
            {
                bool PRHasValues = PrivRights.HasValues;
                Utility.DebugWrite("Found Some Priv Rights");
                //Console.WriteLine(PrivRights);
                AssessPrivRights(PrivRights);
            }
            catch
            {
                Utility.DebugWrite("No Priv Rights Here");
            }

            JToken RegValues = InfToAssess["Registry Values"];
            try
            {
                bool PRHasValues = RegValues.HasValues;
                Utility.DebugWrite("Found Some Registry Values");
                //Console.WriteLine(RegValues);
                AssessRegValues(RegValues);
            }
            catch
            {
                Utility.DebugWrite("No Reg Values Here");
            }

            //Then do:
            //Registry Values
            //System Access
            //Registry Keys
            //Group Membership
            //Service General Setting
            //catch any stuff that falls through the cracks

            /*
            string[] KeysInInf = (InfToAssess.Keys.ToArray());
            var SlippedThrough = KeysInInf.Except(KnownKeys);

            if (SlippedThrough.Count() > 0)
            {
                Utility.DebugWrite("We didn't parse any of these sections:");
                foreach (var UnparsedHeader in SlippedThrough)
                {
                    Console.WriteLine(UnparsedHeader);

                    //  System Access +
                    //  Kerberos Policy -
                    //  Event Audit -
                    //  Registry Values +
                    //  Registry Keys +
                    //  Group Membership +
                    //  Service General Setting +
                }
            }
            //return InfResult;
            */
        }

        public static void AssessPrivRights(JToken PrivRights)
        {
            JArray IntPrivRights = (JArray)JsonData["privRights"]["item"];
            JArray WellKnownSIDS = (JArray)JsonData["trustees"]["item"];

            foreach (JProperty PrivRight in PrivRights.Children<JProperty>())
            {
                foreach (JToken IntPrivRight in IntPrivRights)
                {
                    // if the priv is interesting
                    if ((string)IntPrivRight["privRight"] == PrivRight.Name)
                    {
                        // tell us it's interesting
                        Console.WriteLine("Interesting privilege " + PrivRight.Name + " is granted to:");
                        //then for each trustee it's granted to
                        foreach (string trustee in PrivRight.Value)
                        {
                            // clean up the trustee SID
                            string TrusteeClean = trustee.Trim('*');

                            bool SIDmatches = false;
                            string WKSIDDisplay = "";
                            // iterate over the list of well known sids to see if any match.
                            foreach (JToken WellKnownSID in WellKnownSIDS)
                            {
                                string SIDToMatch = (string)WellKnownSID["SID"];
                                // a bunch of well known sids all include the domain-unique sid, so we gotta compensate for those.
                                if ((SIDToMatch.Contains("DOMAIN")) && (TrusteeClean.Length >= 14))
                                {
                                    string[] TrusteeSplit = trustee.Split("-".ToCharArray());
                                    string[] WKSIDSplit = SIDToMatch.Split("-".ToCharArray());

                                    if (TrusteeSplit[TrusteeSplit.Length - 1] == WKSIDSplit[WKSIDSplit.Length - 1])
                                    {
                                        SIDmatches = true;
                                    }

                                }
                                // check if we have a direct match
                                if ((string)WellKnownSID["SID"] == TrusteeClean)
                                {
                                    SIDmatches = true;
                                }

                                if (SIDmatches == true)
                                {
                                    WKSIDDisplay = (string)WellKnownSID["displayName"];
                                    break;
                                }
                            }
                            // display some info if they match.
                            if (SIDmatches == true)
                            {
                                Console.WriteLine("Display Name : " + WKSIDDisplay);
                                Console.WriteLine("SID Matched : " + TrusteeClean);
                                Console.WriteLine("");

                            }
                            // if they don't match, show the sid anyway. TODO: check these against the domain.
                            else
                            {
                                Console.Write("Unrecognised SID : ");
                                Console.WriteLine(TrusteeClean);
                                Console.WriteLine("");

                            }
                        }
                    }
                }
            }
        }

        public static void AssessRegValues(JToken RegValues)
        {
            JArray IntRegKeys = (JArray)JsonData["regKeys"]["item"];
            JArray MatchedRegKeys = new JArray();
            foreach (JProperty RegValue in RegValues.Children<JProperty>())
            {
                // iterate over the list of interesting keys in our json "db".
                foreach (JToken IntRegKey in IntRegKeys)
                {
                    // if it matches
                    if ((string)IntRegKey["regKey"] == RegValue.Name)
                    {
                        // add our match to the JArray we created
                        MatchedRegKeys.Add(IntRegKey);
                        // print the shit out
                        Console.WriteLine("Check out this reg key:");
                        Console.WriteLine(RegValue.Name);
                        Console.WriteLine("");
                        Console.WriteLine("With these values:");
                        foreach (string thing in RegValue.Value)
                        {
                            Console.WriteLine(thing);
                        }
                        Console.WriteLine("");
                    }
                }

                if (IntRegKeys.Contains(RegValue.Name))
                {
                    string PrintName = RegValue.Name;

                    Utility.DebugWrite("Name: ");
                    Console.WriteLine(PrintName);
                    Utility.DebugWrite("Values: ");
                    // the first value in these looks like a 'type' code.
                    // looks like they work like this:
                    // 4 = Int, but where it's 1 or 0 they use it as a bool
                    // 1 = String in double quotes, some of which are numbers
                    // 7 = Array
                    foreach (string value in RegValues[RegValue.Name])
                    {
                        Console.Write(value);
                        Console.WriteLine("");
                    }

                }
            }
        }

        public static void AssessGPPXml(JObject GPPToAssess)
        {
            string[] GPPCategories = GPPToAssess.Properties().Select(p => p.Name).ToArray();
            foreach (string GPPCategory in GPPCategories)
            {
                if (GPPCategory == "Groups")
                {
                    AssessGPPGroups(GPPToAssess["Groups"]);
                }
                if (GPPCategory == "NetworkOptions")
                {
                   AssessGPPNetworkOptions(GPPToAssess["NetworkOptions"]);
                }
                if (GPPCategory == "Files")
                {
                    AssessGPPFiles(GPPToAssess["Files"]);
                }
                if (GPPCategory == "RegistrySettings")
                {
                    AssessGPPRegSettings(GPPToAssess["RegistrySettings"]);
                }
                if (GPPCategory == "Shortcuts")
                {
                    AssessGPPShortcuts(GPPToAssess["Shortcuts"]);
                }
                if (GPPCategory == "ScheduledTasks")
                {
                    AssessGPPSchedTasks(GPPToAssess["ScheduledTasks"]);
                }
                if (GPPCategory == "NetworkShareSettings")
                {
                    AssessGPPNetShares(GPPToAssess["NetworkShareSettings"]);
                }
                if (GPPCategory == "Folders")
                {
                    AssessGPPFolders(GPPToAssess["Folders"]);
                }
                if (GPPCategory == "NTServices")
                {
                    AssessGPPNTServices(GPPToAssess["NTServices"]);
                }
                if (GPPCategory == "IniFiles")
                {
                    AssessGPPIniFiles(GPPToAssess["IniFiles"]);
                }
                if (GPPCategory == "EnvironmentVariables")
                {
                    Console.WriteLine("Nobody cares about environment variables.");
                }
            }
        }
        public static void AssessGPPIniFiles(JToken GPPIniFiles)
        {
            Utility.DebugWrite("GPP is about GPPIniFiles");
            Console.WriteLine(GPPIniFiles["Ini"]);
        }
        public static void AssessGPPGroups(JToken GPPGroups)
        {
            Utility.DebugWrite("GPP is about Groups");
            Console.WriteLine(GPPGroups["User"]);
            Console.WriteLine(GPPGroups["Group"]);
        }
        public static void AssessGPPNetworkOptions(JToken GPPNetworkOptions)
        {
            Utility.DebugWrite("GPP is about Network Options");
            Console.WriteLine(GPPNetworkOptions["DUN"]);
        }
        public static void AssessGPPFiles(JToken GPPFiles)
        {
            Utility.DebugWrite("GPP is about Files");
            Console.WriteLine(GPPFiles["File"]);
        }
        public static void AssessGPPShortcuts(JToken GPPShortcuts)
        {
            Utility.DebugWrite("GPP is about GPPShortcuts");
            Console.WriteLine(GPPShortcuts["Shortcut"]);
        }
        public static void AssessGPPRegSettings(JToken GPPRegSettings)
        {
            Utility.DebugWrite("GPP is about RegistrySettings");
            Console.WriteLine(GPPRegSettings["Registry"]);
        }
        public static void AssessGPPNTServices(JToken GPPNTServices)
        {
            Utility.DebugWrite("GPP is about NTServices");
            Console.WriteLine(GPPNTServices["NTService"]);
        }
        public static void AssessGPPFolders(JToken GPPFolders)
        {
            Utility.DebugWrite("GPP is about Folders");
            Console.WriteLine(GPPFolders["Folder"]);
        }
        public static void AssessGPPNetShares(JToken GPPNetShares)
        {
            Utility.DebugWrite("GPP is about Network Shares");
            Console.WriteLine(GPPNetShares["NetShare"]);
        }
        public static void AssessGPPSchedTasks(JToken GPPSchedTasks)
        {
            Utility.DebugWrite("GPP is about SchedTasks");
            Console.WriteLine(GPPSchedTasks["Task"]);
            Console.WriteLine(GPPSchedTasks["ImmediateTaskV2"]);
        }
    }
}