using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public class AssessGpp
    {

        public JObject GetAssessed(string assessName, JObject gpp)
        {
            //construct the method name based on the assessName and get it using reflection
            MethodInfo mi = this.GetType().GetMethod("GetAssessed" + assessName);
            //TODO check if mi exists, error out if not implemented
            //invoke the found method
            try
            {
                JObject assessedThing = (JObject)mi.Invoke(this, parameters: new object[] { gpp });
                if (assessedThing != null)
                {
                    return assessedThing;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Utility.DebugWrite(e.ToString());
                return null;
            }

        }

        // none of these assess functions do anything but return the values from the gpp yet.
        public static JObject GetAssessedShortcuts(JObject gppShortcuts)
        {
            JProperty gppShortcutProp = new JProperty("Shortcut", gppShortcuts["Shortcut"]);
            JObject assessedGppShortcuts = new JObject(gppShortcutProp);
            return assessedGppShortcuts;
            //Utility.DebugWrite("gpp is about gppShortcuts");
            //Console.WriteLine(gppShortcuts["Shortcut"]);
        }

        public static JObject GetAssessedSchedTasks(JObject gppSchedTasks)
        {
            JProperty assessedGppSchedTasksTaskProp = new JProperty("Task", gppSchedTasks["Task"]);
            JProperty assessedGppSchedTasksImmediateTaskProp = new JProperty("ImmediateTaskV2", gppSchedTasks["ImmediateTaskV2"]);
            JObject assessedGppSchedTasksAllJson = new JObject(assessedGppSchedTasksTaskProp, assessedGppSchedTasksImmediateTaskProp);
            return assessedGppSchedTasksAllJson;
            //Utility.DebugWrite("gpp is about SchedTasks");
            //Console.WriteLine(gppSchedTasks["Task"]);
            //Console.WriteLine(gppSchedTasks["ImmediateTaskV2"]);
        }

        public static JObject GetAssessedRegSettings(JObject gppRegSettings)
        {
            JProperty gppRegSettingsProp = new JProperty("RegSettings", gppRegSettings["Registry"]);
            JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
            return assessedGppRegSettings;
            //Utility.DebugWrite("gpp is about RegistrySettings");
            //Console.WriteLine(gppRegSettings["Registry"]);
        }

        public static JObject GetAssessedNtServices(JObject gppNtServices)
        {
            JProperty ntServiceProp = new JProperty("NTService", gppNtServices["NTService"]);
            JObject assessedNtServices = new JObject(ntServiceProp);
            return assessedNtServices;
            //Utility.DebugWrite("gpp is about NTServices");
            //Console.WriteLine(gppNtServices["NTService"]);
        }

        public static JObject GetAssessedNetworkOptions(JObject gppNetworkOptions)
        {
            JProperty gppNetworkOptionsProp = new JProperty("DUN", gppNetworkOptions["DUN"]);
            JObject assessedGppNetworkOptions = new JObject(gppNetworkOptionsProp);
            return assessedGppNetworkOptions;
            //Utility.DebugWrite("gpp is about Network Options");
            //Console.WriteLine(gppNetworkOptions["DUN"]);
        }

        public static JObject GetAssessedFolders(JObject gppFolders)
        {
            JProperty gppFoldersProp = new JProperty("Folder", gppFolders["Folder"]);
            JObject assessedGppFolders = new JObject(gppFoldersProp);
            return assessedGppFolders;
            //Utility.DebugWrite("gpp is about Folders");
            //Console.WriteLine(gppFolders["Folder"]);
        }

        public static JObject GetAssessedNetShares(JObject gppNetShares)
        {
            JProperty gppNetSharesProp = new JProperty("NetShare", gppNetShares["NetShare"]);
            JObject assessedGppNetShares = new JObject(gppNetSharesProp);
            return assessedGppNetShares;
            //Utility.DebugWrite("gpp is about Network Shares");
            //Console.WriteLine(gppNetShares["NetShare"]);
        }


        public static JObject GetAssessedIniFiles(JObject gppIniFiles)
        {
            //Utility.DebugWrite("gpp is about gppIniFiles");
            JObject assessedGppIniFiles = (JObject)gppIniFiles["Ini"];
            //Console.WriteLine(AssessedGPPIniFiles.ToString());
            return assessedGppIniFiles;
        }

        public static JObject GetAssessedFiles(JObject gppFiles)
        {
            JProperty gppFileProp = new JProperty("File", gppFiles["File"]);
            JObject assessedGppFiles = new JObject(gppFileProp);
            return assessedGppFiles;
            //Utility.DebugWrite("gpp is about Files");
            //Console.WriteLine(gppFiles["File"]);
        }

        public static JObject GetAssessedGroups(JObject gppGroups)
        {
            Dictionary<string, Dictionary<string, string>> assessedGroupsDict = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> assessedUsersDict = new Dictionary<string, Dictionary<string, string>>();

            foreach (JToken gppUser in gppGroups["User"])
            {
                // dictionary for results from this specific user.
                Dictionary<string, string> assessedUserDict = new Dictionary<string, string>
                {
                    { "InterestLevel", "3" }
                };
                // check what the entry is doing to the user and turn it into real word
                string userAction = gppUser["Properties"]["@action"].ToString();
                userAction = GetActionString(userAction);

                // get the username and a bunch of other details:
                assessedUserDict.Add("Name", gppUser["@name"].ToString());
                assessedUserDict.Add("User Name", gppUser["Properties"]["@userName"].ToString());
                assessedUserDict.Add("DateTime Changed", gppUser["@changed"].ToString());
                assessedUserDict.Add("Account Disabled", gppUser["Properties"]["@acctDisabled"].ToString());
                assessedUserDict.Add("Password Never Expires", gppUser["Properties"]["@neverExpires"].ToString());
                assessedUserDict.Add("Description", gppUser["Properties"]["@description"].ToString());
                assessedUserDict.Add("Full Name", gppUser["Properties"]["@fullName"].ToString());
                assessedUserDict.Add("New Name", gppUser["Properties"]["@newName"].ToString());
                assessedUserDict.Add("Action", userAction);

                // check for cpasswords
                string cpassword = gppUser["Properties"]["@cpassword"].ToString();
                if (cpassword.Length > 0)
                {
                    string decryptedCpassword = "";
                    decryptedCpassword = Utility.DecryptCpassword(cpassword);
                    // if we find one, that's super interesting.
                    assessedUserDict.Add("Cpassword", decryptedCpassword);
                    assessedUserDict["InterestLevel"] = "10";
                }
                // add to the output dict with a uid to keep it unique.
                assessedUsersDict.Add(gppUser["@uid"].ToString(), assessedUserDict);
            }

            // repeat the process for Groups
            foreach (JToken gppGroup in gppGroups["Group"])
            {
                //dictionary for results from this specific group
                Dictionary<string, string> assessedGroupDict = new Dictionary<string, string>
                {
                    { "InterestLevel", "3" }
                };
                // check what the entry is doing to the group and turn it into real word
                string groupAction = gppGroup["Properties"]["@action"].ToString();
                groupAction = GetActionString(groupAction);

                // get the group name and a bunch of other details:
                assessedGroupDict.Add("Name", gppGroup["@name"].ToString());
                //assessedGroupDict.Add("User Name", gppGroup["Properties"]["@userName"].ToString());
                assessedGroupDict.Add("DateTime Changed", gppGroup["@changed"].ToString());
                //assessedGroupDict.Add("Account Disabled", gppGroup["Properties"]["@acctDisabled"].ToString());
                //assessedGroupDict.Add("Password Never Expires", gppGroup["Properties"]["@neverExpires"].ToString());
                //assessedGroupDict.Add("Description", gppGroup["Properties"]["@description"].ToString());
                //assessedGroupDict.Add("Full Name", gppGroup["Properties"]["@fullName"].ToString());
                //assessedGroupDict.Add("New Name", gppGroup["Properties"]["@newName"].ToString());
                //assessedGroupDict.Add("Delete All Users", gppGroup["Properties"]["@deleteAllUsers"].ToString());
                //assessedGroupDict.Add("Delete All Groups", gppGroup["Properties"]["@deleteAllGroups"].ToString());
                //assessedGroupDict.Add("Remove Accounts", gppGroup["Properties"]["@removeAccounts"].ToString());
                assessedGroupDict.Add("Action", groupAction);
                //Utility.DebugWrite(gppGroup["Properties"]["Members"].ToString());


                // add to the output dict with a uid to keep it unique.
                assessedGroupsDict.Add(gppGroup["@uid"].ToString(), assessedGroupDict);

            }

            // cast our Dictionaries back into JObjects
            JProperty assessedUsersJson = new JProperty("gpp User settings", JToken.FromObject(assessedUsersDict));
            JProperty assessedGroupsJson = new JProperty("gpp Group settings", JToken.FromObject(assessedGroupsDict));
            // chuck the users and groups together in one JObject
            JObject assessedGppGroupsJson = new JObject();
            // only want to actually output these things if there's anything useful in them.
            if (assessedUsersDict.Count > 0)
            {
                assessedGppGroupsJson.Add(assessedUsersJson);
            }
            if (assessedGroupsDict.Count > 0)
            {
                assessedGppGroupsJson.Add(assessedGroupsJson);
            }
            return assessedGppGroupsJson;
        }

        public static string GetActionString(string actionChar)
            // shut up, i know it's not really a char.
        {
            string actionString = "";

            switch (actionChar)
            {
                case "U":
                    actionString = "Update";
                    break;
                case "A":
                    actionString = "Add";
                    break;
                case "D":
                    actionString = "Delete";
                    break;
                default:
                    Utility.DebugWrite("oh no this is new");
                    actionString = "Broken";
                    break;
            }

            return actionString;
        }
    }
}
