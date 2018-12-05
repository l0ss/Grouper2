/***
 *      .,-:::::/  :::::::..       ...      ...    :::::::::::::. .,::::::  :::::::..     .:::.  
 *    ,;;-'````'   ;;;;``;;;;   .;;;;;;;.   ;;     ;;; `;;;```.;;;;;;;''''  ;;;;``;;;;   ,;'``;. 
 *    [[[   [[[[[[/ [[[,/[[['  ,[[     \[[,[['     [[[  `]]nnn]]'  [[cccc    [[[,/[[['   ''  ,[['
 *    "$$c.    "$$  $$$$$$c    $$$,     $$$$$      $$$   $$$""     $$""""    $$$$$$c     .c$$P'  
 *     `Y8bo,,,o88o 888b "88bo,"888,_ _,88P88    .d888   888o      888oo,__  888b "88bo,d88 _,oo,
 *       `'YMUP"YMM MMMM   "W"   "YMMMMMP"  "YmmMMMM""   YMMMb     """"YUMMM MMMM   "W" MMMUP*"^^
 *                                                                                               
 *                        By Mike Loss (@mikeloss)                                                
 */
using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Grouper2
{
    class Assess
    {
        // Assesses the contents of a GPTmpl
        public static JObject AssessGPTmpl(JObject InfToAssess)
        {
            // create a dict to put all our results into
            Dictionary<string, JObject> AssessedGPTmpl = new Dictionary<string, JObject>();

            // an array for GPTmpl headings to ignore.
            List<string> KnownKeys = new List<string>
            {
                "Unicode",
                "Version"
            };

            // go through each category we care about and look for goodies.
            ///////////////////////////////////////////////////////////////
            // Privilege Rights
            ///////////////////////////////////////////////////////////////
            JToken PrivRights = InfToAssess["Privilege Rights"];
            try
            {
                bool PRHasValues = PrivRights.HasValues;
                if (PRHasValues)
                {
                    //
                    JObject PrivRightsResults = AssessPrivRights(PrivRights);
                    if (PrivRightsResults.Count > 0)
                    {
                        AssessedGPTmpl.Add("PrivRights", PrivRightsResults);
                    }
                    KnownKeys.Add("Privilege Rights");
                }
            }
            catch
            {
                Utility.DebugWrite("No Priv Rights Here - something's broke.");
            }
            ///////////////////////////////////////////////////////////////
            // Registry Values
            ///////////////////////////////////////////////////////////////
            JToken RegValues = InfToAssess["Registry Values"];
            try
            {
                bool PRHasValues = RegValues.HasValues;
                if (PRHasValues)
                {
                    //Utility.DebugWrite("Found Some Registry Values");
                    //Console.WriteLine(RegValues);
                    JObject MatchedRegValues = AssessRegValues(RegValues);
                    if (MatchedRegValues.Count > 0)
                    {
                        //Utility.DebugWrite("Here's what we got back from assessing reg values");
                        //Console.WriteLine(MatchedRegValues.ToString());
                        AssessedGPTmpl.Add("RegValues", MatchedRegValues);
                    }
                    KnownKeys.Add("Registry Values");
                }
            }
            catch
            {
                Utility.DebugWrite("No Reg Values Here - something's broke.");
            }

            //TODO:
            //System Access
            //Registry Keys
            //Group Membership
            //Service General Setting

            //catch any stuff that falls through the cracks, i.e. look for headings on sections that we aren't parsing.

            List<string> HeadingsInInf =  new List<string>();
            foreach (JProperty Section in InfToAssess.Children<JProperty>())
            {
                string SectionName = Section.Name;
                HeadingsInInf.Add(SectionName);
            }
            var SlippedThrough = HeadingsInInf.Except(KnownKeys);
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
            
            //mangle our json thing into a jobject and return it
            JObject AssessedGPTmplJson = (JObject)JToken.FromObject(AssessedGPTmpl);
            return AssessedGPTmplJson;
        }

        public static JObject AssessPrivRights(JToken PrivRights)
        {
            JObject JsonData = JankyDB.Instance;
            JArray IntPrivRights = (JArray)JsonData["privRights"]["item"];
            JArray WellKnownSIDS = (JArray)JsonData["trustees"]["item"];

            // create an object to put the results in
            Dictionary<string, Dictionary<string, string>> MatchedPrivRights = new Dictionary<string, Dictionary<string, string>>();

            foreach (JProperty PrivRight in PrivRights.Children<JProperty>())
            {
                foreach (JToken IntPrivRight in IntPrivRights)
                {
                    // if the priv is interesting
                    if ((string)IntPrivRight["privRight"] == PrivRight.Name)
                    {
                        //create a dict to put the trustees into
                        Dictionary<string, string> TrusteesDict = new Dictionary<string, string>();
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
                                // a bunch of well known sids all include the domain-unique sid, so we gotta check for matches amongst those.
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
                                //Utility.Debug("SID Matches");
                            }
                            // if they don't match, handle that.
                            else
                            {
                                WKSIDDisplay = "unknown";
                                //TODO: look up unknown SIDS in the domain if we can.
                                //Utility.DebugWrite("Unrecognised SID : " + TrusteeClean);
                                //Console.WriteLine("");
                            }
                            TrusteesDict.Add(TrusteeClean, WKSIDDisplay);
                        }
                        // add the results to our dictionary of trustees
                        string MatchedPrivRightName = PrivRight.Name;
                        MatchedPrivRights.Add(MatchedPrivRightName, TrusteesDict);
                    }
                }
            }
            // cast our dict to a jobject and return it.
            JObject MatchedPrivRightsJson = (JObject)JToken.FromObject(MatchedPrivRights);
            return MatchedPrivRightsJson;
        }

        public static JObject AssessRegValues(JToken RegValues)
        {
            JObject JsonData = JankyDB.Instance;
            // get our data about what regkeys are interesting
            JArray IntRegKeys = (JArray)JsonData["regKeys"]["item"];
            // set up a dictionary for our results to go into
            Dictionary<string, string[]> MatchedRegValues = new Dictionary<string, string[]>();

            foreach (JProperty RegValue in RegValues.Children<JProperty>())
            {
                // iterate over the list of interesting keys in our json "db".
                foreach (JToken IntRegKey in IntRegKeys)
                {
                    // if it matches
                    if ((string)IntRegKey["regKey"] == RegValue.Name)
                    {
                        string MatchedRegKey = RegValue.Name;
                        //create a list to put the values in
                        List<string> RegKeyValueList = new List<string>();
                        foreach (string thing in RegValue.Value)
                        {
                            // put the values in the list
                            RegKeyValueList.Add(thing);
                        }
                        //Console.WriteLine("");
                        string[] RegKeyValueArray = RegKeyValueList.ToArray();
                        MatchedRegValues.Add(MatchedRegKey, RegKeyValueArray);
                    }
                }
            }
            // cast our output into a jobject and return it
            JObject MatchedRegValuesJson = (JObject)JToken.FromObject(MatchedRegValues);
            return MatchedRegValuesJson;
        }

        public static JObject AssessGPPJson(JObject GPPToAssess)
        {
            // get an array of categories in our GPP to assess to look at
            string[] GPPCategories = GPPToAssess.Properties().Select(p => p.Name).ToArray();
            // create a dict to put our results into before returning them
            Dictionary<string, JObject> AssessedGPPDict = new Dictionary<string, JObject>();
            // iterate over the array sending appropriate gpp data to the appropriate assess() function.
            foreach (string GPPCategory in GPPCategories)
            {
                if (GPPCategory == "Groups")
                {
                    AssessGPPGroups((JObject)GPPToAssess["Groups"]);
                }
                if (GPPCategory == "NetworkOptions")
                {
                   AssessGPPNetworkOptions((JObject)GPPToAssess["NetworkOptions"]);
                }
                if (GPPCategory == "Files")
                {
                    AssessGPPFiles((JObject)GPPToAssess["Files"]);
                }
                if (GPPCategory == "RegistrySettings")
                {
                    AssessGPPRegSettings((JObject)GPPToAssess["RegistrySettings"]);
                }
                if (GPPCategory == "Shortcuts")
                {
                    AssessGPPShortcuts((JObject)GPPToAssess["Shortcuts"]);
                }
                if (GPPCategory == "ScheduledTasks")
                {
                    AssessGPPSchedTasks((JObject)GPPToAssess["ScheduledTasks"]);
                }
                if (GPPCategory == "NetworkShareSettings")
                {
                    AssessGPPNetShares((JObject)GPPToAssess["NetworkShareSettings"]);
                }
                if (GPPCategory == "Folders")
                {
                    AssessGPPFolders((JObject)GPPToAssess["Folders"]);
                }
                if (GPPCategory == "NTServices")
                {
                    AssessGPPNTServices((JObject)GPPToAssess["NTServices"]);
                }
                if (GPPCategory == "IniFiles")
                {
                    JObject AssessedIniFiles = AssessGPPIniFiles((JObject)GPPToAssess["IniFiles"]);
                    AssessedGPPDict.Add("IniFiles", AssessedIniFiles);
                }
            }
            JObject AssessedGPPJson = (JObject)JToken.FromObject(AssessedGPPDict);
            return AssessedGPPJson;
        }
        
        // none of these assess functions do anything but return the values from the GPP yet.
        public static JObject AssessGPPIniFiles(JObject GPPIniFiles)
        {
            //Utility.DebugWrite("GPP is about GPPIniFiles");
            JObject AssessedGPPIniFiles = (JObject)GPPIniFiles["Ini"];
            //Console.WriteLine(AssessedGPPIniFiles.ToString());
            return AssessedGPPIniFiles;
        }
        public static JObject AssessGPPGroups(JObject GPPGroups)
        {
            JProperty AssessedGPPGroups = new JProperty("User", GPPGroups["User"]);
            JProperty AssessedGPPUsers = new JProperty("Group", GPPGroups["Group"]);
            JObject AssessedGPPGroupsAllJson = new JObject(AssessedGPPGroups, AssessedGPPUsers);
            return AssessedGPPGroupsAllJson;
            //Utility.DebugWrite("GPP is about Groups");
            //Console.WriteLine(GPPGroups["User"]);
            //Console.WriteLine(GPPGroups["Group"]);
        }
        public static JObject AssessGPPNetworkOptions(JObject GPPNetworkOptions)
        {
            JProperty GPPNetworkOptionsProp = new JProperty("DUN", GPPNetworkOptions["DUN"]);
            JObject AssessedGPPNetworkOptions = new JObject(GPPNetworkOptionsProp);
            return AssessedGPPNetworkOptions;
            //Utility.DebugWrite("GPP is about Network Options");
            //Console.WriteLine(GPPNetworkOptions["DUN"]);
        }
        public static JObject AssessGPPFiles(JObject GPPFiles)
        {
            JProperty GPPFileProp = new JProperty("File", GPPFiles["File"]);
            JObject AssessedGPPFiles = new JObject(GPPFileProp);
            return AssessedGPPFiles;
            //Utility.DebugWrite("GPP is about Files");
            //Console.WriteLine(GPPFiles["File"]);
        }
        public static JObject AssessGPPShortcuts(JObject GPPShortcuts)
        {
            JProperty GPPShortcutProp = new JProperty("Shortcut", GPPShortcuts["Shortcut"]);
            JObject AssessedGPPShortcuts = new JObject(GPPShortcutProp);
            return AssessedGPPShortcuts;
            //Utility.DebugWrite("GPP is about GPPShortcuts");
            //Console.WriteLine(GPPShortcuts["Shortcut"]);
        }
        public static JObject AssessGPPRegSettings(JObject GPPRegSettings)
        {
            JProperty GPPRegSettingsProp = new JProperty("RegSettings", GPPRegSettings["Registry"]);
            JObject AssessedGPPRegSettings = new JObject(GPPRegSettingsProp);
            return AssessedGPPRegSettings;
            //Utility.DebugWrite("GPP is about RegistrySettings");
            //Console.WriteLine(GPPRegSettings["Registry"]);
        }
        public static JObject AssessGPPNTServices(JObject GPPNTServices)
        {
            JProperty NTServiceProp = new JProperty("NTService", GPPNTServices["NTService"]);
            JObject AssessedGPPNTServices = new JObject(NTServiceProp);
            return AssessedGPPNTServices;
            //Utility.DebugWrite("GPP is about NTServices");
            //Console.WriteLine(GPPNTServices["NTService"]);
        }
        public static JObject AssessGPPFolders(JObject GPPFolders)
        {
            JProperty GPPFoldersProp = new JProperty("Folder", GPPFolders["Folder"]);
            JObject AssessedGPPFolders = new JObject(GPPFoldersProp);
            return AssessedGPPFolders;
            //Utility.DebugWrite("GPP is about Folders");
            //Console.WriteLine(GPPFolders["Folder"]);
        }
        public static JObject AssessGPPNetShares(JObject GPPNetShares)
        {
            JProperty GPPNetSharesProp = new JProperty("NetShare", GPPNetShares["NetShare"]);
            JObject AssessedGPPNetShares = new JObject(GPPNetSharesProp);
            return AssessedGPPNetShares;
            //Utility.DebugWrite("GPP is about Network Shares");
            //Console.WriteLine(GPPNetShares["NetShare"]);
        }
        public static JObject AssessGPPSchedTasks(JObject GPPSchedTasks)
        {
            JProperty AssessedGPPSchedTasksTaskProp = new JProperty("Task", GPPSchedTasks["Task"]);
            JProperty AssessedGPPSchedTasksImmediateTaskProp = new JProperty("ImmediateTaskV2", GPPSchedTasks["ImmediateTaskV2"]);
            JObject AssessedGPPSchedTasksAllJson = new JObject(AssessedGPPSchedTasksTaskProp, AssessedGPPSchedTasksImmediateTaskProp);
            return AssessedGPPSchedTasksAllJson;
            //Utility.DebugWrite("GPP is about SchedTasks");
            //Console.WriteLine(GPPSchedTasks["Task"]);
            //Console.WriteLine(GPPSchedTasks["ImmediateTaskV2"]);
        }
    }
}