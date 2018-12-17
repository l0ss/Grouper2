using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Grouper2
{
    public class AssessGPP
    {
        private readonly JObject _GPP;

        public AssessGPP(JObject GPP)
        {
            _GPP = GPP;
        }

        public JObject GetAssessed(string assessName)
        {
            //construct the method name based on the assessName and get it using reflection
            MethodInfo mi = this.GetType().GetMethod("GetAssessed" + assessName);
            //TODO check if mi exists, error out if not implemented
            //invoke the found method

            try
            {
                JObject AssessedThing = (JObject)mi.Invoke(this, parameters: new object[] { assessName });
                if (AssessedThing.HasValues)
                {
                    return AssessedThing;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        // none of these assess functions do anything but return the values from the GPP yet.
        private JObject GetAssessedShortcuts(string assessName)
        {
            JObject GPPCategory = (JObject)_GPP[assessName];
            JProperty GPPShortcutProp = new JProperty(assessName, GPPCategory[assessName]);
            JObject AssessedGPPShortcuts = new JObject(GPPShortcutProp);
            return AssessedGPPShortcuts;
            //Utility.DebugWrite("GPP is about GPPShortcuts");
            //Console.WriteLine(GPPShortcuts["Shortcut"]);
        }

        //TODO ALL THE METHODS BELOW SHOULD BE CHANGED TO MATCH THE SIGNATURE OF GetAssessedShortcuts ABOVE
        public static JObject GetAssessedSchedTasks(JObject GPPSchedTasks)
        {
            JProperty AssessedGPPSchedTasksTaskProp = new JProperty("Task", GPPSchedTasks["Task"]);
            JProperty AssessedGPPSchedTasksImmediateTaskProp = new JProperty("ImmediateTaskV2", GPPSchedTasks["ImmediateTaskV2"]);
            JObject AssessedGPPSchedTasksAllJson = new JObject(AssessedGPPSchedTasksTaskProp, AssessedGPPSchedTasksImmediateTaskProp);
            return AssessedGPPSchedTasksAllJson;
            //Utility.DebugWrite("GPP is about SchedTasks");
            //Console.WriteLine(GPPSchedTasks["Task"]);
            //Console.WriteLine(GPPSchedTasks["ImmediateTaskV2"]);
        }

        public static JObject GetAssessedRegSettings(JObject GPPRegSettings)
        {
            JProperty GPPRegSettingsProp = new JProperty("RegSettings", GPPRegSettings["Registry"]);
            JObject AssessedGPPRegSettings = new JObject(GPPRegSettingsProp);
            return AssessedGPPRegSettings;
            //Utility.DebugWrite("GPP is about RegistrySettings");
            //Console.WriteLine(GPPRegSettings["Registry"]);
        }

        public static JObject GetAssessedNTServices(JObject GPPNTServices)
        {
            JProperty NTServiceProp = new JProperty("NTService", GPPNTServices["NTService"]);
            JObject AssessedGPPNTServices = new JObject(NTServiceProp);
            return AssessedGPPNTServices;
            //Utility.DebugWrite("GPP is about NTServices");
            //Console.WriteLine(GPPNTServices["NTService"]);
        }

        public static JObject GetAssessedNetworkOptions(JObject GPPNetworkOptions)
        {
            JProperty GPPNetworkOptionsProp = new JProperty("DUN", GPPNetworkOptions["DUN"]);
            JObject AssessedGPPNetworkOptions = new JObject(GPPNetworkOptionsProp);
            return AssessedGPPNetworkOptions;
            //Utility.DebugWrite("GPP is about Network Options");
            //Console.WriteLine(GPPNetworkOptions["DUN"]);
        }

        public static JObject GetAssessedFolders(JObject GPPFolders)
        {
            JProperty GPPFoldersProp = new JProperty("Folder", GPPFolders["Folder"]);
            JObject AssessedGPPFolders = new JObject(GPPFoldersProp);
            return AssessedGPPFolders;
            //Utility.DebugWrite("GPP is about Folders");
            //Console.WriteLine(GPPFolders["Folder"]);
        }

        public static JObject GetAssessedNetShares(JObject GPPNetShares)
        {
            JProperty GPPNetSharesProp = new JProperty("NetShare", GPPNetShares["NetShare"]);
            JObject AssessedGPPNetShares = new JObject(GPPNetSharesProp);
            return AssessedGPPNetShares;
            //Utility.DebugWrite("GPP is about Network Shares");
            //Console.WriteLine(GPPNetShares["NetShare"]);
        }


        public static JObject GetAssessedIniFiles(JObject GPPIniFiles)
        {
            //Utility.DebugWrite("GPP is about GPPIniFiles");
            JObject AssessedGPPIniFiles = (JObject)GPPIniFiles["Ini"];
            //Console.WriteLine(AssessedGPPIniFiles.ToString());
            return AssessedGPPIniFiles;
        }

        public static JObject GetAssessedFiles(JObject GPPFiles)
        {
            JProperty GPPFileProp = new JProperty("File", GPPFiles["File"]);
            JObject AssessedGPPFiles = new JObject(GPPFileProp);
            return AssessedGPPFiles;
            //Utility.DebugWrite("GPP is about Files");
            //Console.WriteLine(GPPFiles["File"]);
        }

        public static JObject GetAssessedGroups(JObject GPPGroups)
        {
            Dictionary<string, Dictionary<string, string>> AssessedGroupsDict = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> AssessedUsersDict = new Dictionary<string, Dictionary<string, string>>();

            foreach (JToken User in GPPGroups["User"])
            {
                // dictionary for results from this specific user.
                Dictionary<string, string> AssessedUserDict = new Dictionary<string, string>
                {
                    { "InterestLevel", "3" }
                };
                // check what the entry is doing to the user and turn it into real word
                string UserAction = User["Properties"]["@action"].ToString();
                switch (UserAction)
                {
                    case "U":
                        UserAction = "Update";
                        break;
                    case "A":
                        UserAction = "Add";
                        break;
                    case "D":
                        UserAction = "Delete";
                        break;
                    default:
                        Console.WriteLine("oh no this is new");
                        break;
                }
                // get the username and a bunch of other details:
                AssessedUserDict.Add("Name", User["@name"].ToString());
                AssessedUserDict.Add("User Name", User["Properties"]["@userName"].ToString());
                AssessedUserDict.Add("DateTime Changed", User["@changed"].ToString());
                AssessedUserDict.Add("Account Disabled", User["Properties"]["@acctDisabled"].ToString());
                AssessedUserDict.Add("Password Never Expires", User["Properties"]["@neverExpires"].ToString());
                AssessedUserDict.Add("Description", User["Properties"]["@description"].ToString());
                AssessedUserDict.Add("Full Name", User["Properties"]["@fullName"].ToString());
                AssessedUserDict.Add("New Name", User["Properties"]["@newName"].ToString());

                // check for cpasswords 
                string cpassword = User["Properties"]["@cpassword"].ToString();
                string DecryptedCpassword = "";
                if (cpassword.Length > 0)
                {
                    DecryptedCpassword = Utility.DecryptCpassword(cpassword);
                    // if we find one, that's super interesting.
                    AssessedUserDict.Add("Cpassword", DecryptedCpassword);
                    AssessedUserDict["InterestLevel"] = "10";
                }
                // add to the output dict with a uid to keep it unique.
                AssessedUsersDict.Add(User["@uid"].ToString(), AssessedUserDict);
            }

            // repeat the process for Groups
            foreach (JToken Group in GPPGroups["Group"])
            {
                //dictionary for results from this specific group
                Dictionary<string, string> AssessedGroupDict = new Dictionary<string, string>();
                string GroupAction = Group["Properties"]["@action"].ToString();
                switch (GroupAction)
                {
                    case "U":
                        break;
                    case "A":
                        break;
                    default:
                        Console.WriteLine("oh no this is new");
                        break;
                }
            }

            // cast our Dictionaries back into JObjects
            JProperty AssessedUsersJson = new JProperty("GPP User settings", JToken.FromObject(AssessedUsersDict));
            JProperty AssessedGroupsJson = new JProperty("GPP Group settings", JToken.FromObject(AssessedGroupsDict));
            // chuck the users and groups together in one JObject
            JObject AssessedGPPGroupsJson = new JObject();
            // only want to actually output these things if there's anything useful in them.
            if (AssessedUsersDict.Count > 0)
            {
                AssessedGPPGroupsJson.Add(AssessedUsersJson);
            }
            if (AssessedGroupsDict.Count > 0)
            {
                AssessedGPPGroupsJson.Add(AssessedGroupsJson);
            }
            return AssessedGPPGroupsJson;
        }

    }
}
