using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public class AssessGpp
    {
        private readonly JObject _GPP;

        public AssessGpp(JObject GPP)
        {
            _GPP = GPP;
        }

        public JObject GetAssessed(string assessName)
        {
            //construct the method name based on the assessName and get it using reflection
            MethodInfo mi = this.GetType().GetMethod("GetAssessed" + assessName, BindingFlags.NonPublic | BindingFlags.Instance);
            //invoke the found method
            try
            {
                JObject gppToAssess = (JObject)_GPP[assessName];
                if (mi != null)
                {
                    JObject assessedThing = (JObject)mi.Invoke(this, parameters: new object[] { gppToAssess });
                    if (assessedThing != null)
                    {
                        return assessedThing;
                    }
                    else
                    {
                        Utility.DebugWrite(assessName);
                        return null;
                    }
                }
                else
                {
                    Utility.DebugWrite("Failed to find method: GetAssessed" + assessName);
                    return null;
                }
            }
            catch (Exception e)
            {
                Utility.DebugWrite(e.ToString());
                return null;
            }
        }

        private JObject GetAssessedFiles(JObject gppCategory)
        {
            JObject assessedFiles = new JObject();

            if (gppCategory["File"] is JArray)
            {
                foreach (JObject gppFile in gppCategory["File"])
                {
                    assessedFiles.Add(gppFile["@uid"].ToString(), GetAssessedFile(gppFile));
                }
            }
            else
            {
                JObject gppFile = (JObject)JToken.FromObject(gppCategory["File"]);
                assessedFiles.Add(gppFile["@uid"].ToString(), GetAssessedFile(gppFile));
            }
            
            return assessedFiles;
        }

        private JObject GetAssessedFile(JObject gppFile)
        {
            int interestLevel = 3;
            JObject assessedFile = new JObject();
            JToken gppFileProps = gppFile["Properties"];
            assessedFile.Add("Name", gppFile["@name"].ToString());
            assessedFile.Add("Status", gppFile["@status"].ToString());
            assessedFile.Add("Changed", gppFile["@changed"].ToString());
            string gppFileAction = Utility.GetActionString(gppFileProps["@action"].ToString());
            assessedFile.Add("Action", gppFileAction);
            string fromPath = gppFileProps["@fromPath"].ToString();
            assessedFile.Add("From Path", fromPath);
            assessedFile.Add("Target Path", gppFileProps["@targetPath"].ToString());
            //TODO some logic to enumerate file ACLS
            if (GlobalVar.OnlineChecks && (fromPath.Length > 0))
            {
                if (Utility.DoesFileExist(fromPath))
                {
                    assessedFile.Add("Source file exists", "True");
                    bool writable = false;
                    writable = Utility.CanIWrite(fromPath);
                    if (writable)
                    {
                        interestLevel = 10;
                        assessedFile.Add("Source file writable", "True");
                    }
                    else
                    {
                        assessedFile.Add("Source file writable", "False");
                    }
                }
                else
                {
                    assessedFile.Add("Source file exists", "False");
                    interestLevel = 7;
                }
            }

            // if it's too boring to be worth showing, return an empty jobj.
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedFile = new JObject();
            }
            return assessedFile;
        }

        private JObject GetAssessedGroups(JObject gppCategory)
        {
            JObject assessedGroups = new JObject();
            JObject assessedUsers = new JObject();

            if (gppCategory["Group"] is JArray)
            {
                foreach (JObject gppGroup in gppCategory["Group"])
                {
                    JObject assessedGroup = GetAssessedGroup(gppGroup);
                    if (assessedGroup.Count > 0)
                    {
                        assessedGroups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                    }
                }
            }
            else
            {
                JObject gppGroup = (JObject)JToken.FromObject(gppCategory["Group"]);
                JObject assessedGroup = GetAssessedGroup(gppGroup);
                if (assessedGroup.Count > 0)
                {
                    assessedGroups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                }
            }
            JObject assessedGppGroups = (JObject)JToken.FromObject(assessedGroups);

            if (gppCategory["User"] is JArray)
            {
                foreach (JObject gppUser in gppCategory["User"])
                {
                    JObject assessedUser = GetAssessedUser(gppUser);
                    if (assessedUser.Count > 0)
                    {
                        assessedUsers.Add(gppUser["@uid"].ToString(), assessedUser);
                    }
                }
            }
            else
            {
                JObject gppUser = (JObject)JToken.FromObject(gppCategory["User"]);
                JObject assessedUser = GetAssessedUser(gppUser);
                if (assessedUser.Count > 0)
                {
                    assessedUsers.Add(gppUser["@uid"].ToString(), assessedUser);
                }
            }
            JObject assessedGppUsers = (JObject)JToken.FromObject(assessedUsers);
            
            JProperty assessedUsersJson = new JProperty("GPPUserSettings", assessedGppUsers);
            JProperty assessedGroupsJson = new JProperty("GPPGroupSettings", assessedGppGroups);
            // chuck the users and groups together in one JObject
            JObject assessedGppGroupsJson = new JObject();
            // only want to actually output these things if there's anything useful in them.
            if (assessedUsers.Count > 0)
            {
                assessedGppGroupsJson.Add(assessedUsersJson);
            }
            if (assessedGroups.Count > 0)
            {
                assessedGppGroupsJson.Add(assessedGroupsJson);
            }
            return assessedGppGroupsJson;
        }

        private JObject GetAssessedUser(JObject gppUser)
        {
            //foreach (JToken gppUser in gppUsers) {
            // jobj for results from this specific user.
            JObject assessedUser = new JObject();

            //set base interest level
            int interestLevel = 3;

            JToken gppUserProps = gppUser["Properties"];

            // check what the entry is doing to the user and turn it into real word
            string userAction = gppUserProps["@action"].ToString();
            userAction = Utility.GetActionString(userAction);

            // get the username and a bunch of other details:
            assessedUser.Add("Name", gppUser["@name"].ToString());
            assessedUser.Add("User Name", gppUserProps["@userName"].ToString());
            assessedUser.Add("DateTime Changed", gppUser["@changed"].ToString());
            assessedUser.Add("Account Disabled", gppUserProps["@acctDisabled"].ToString());
            assessedUser.Add("Password Never Expires", gppUserProps["@neverExpires"].ToString());
            assessedUser.Add("Description", gppUserProps["@description"].ToString());
            assessedUser.Add("Full Name", gppUserProps["@fullName"].ToString());
            assessedUser.Add("New Name", gppUserProps["@newName"].ToString());
            assessedUser.Add("Action", userAction);

            // check for cpasswords
            string cpassword = gppUserProps["@cpassword"].ToString();
            if (cpassword.Length > 0)
            {
                string decryptedCpassword = "";
                decryptedCpassword = Utility.DecryptCpassword(cpassword);
                // if we find one, that's super interesting.
                assessedUser.Add("Cpassword", decryptedCpassword);
                interestLevel = 10;
            }
            // if it's too boring to be worth showing, return an empty jobj.
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedUser = new JObject();
            }
            return assessedUser;
        }

        private JObject GetAssessedGroup(JObject gppGroup)
        {
            //jobj for results from this specific group
            JObject assessedGroup = new JObject();
            int interestLevel = 3;

            JToken gppGroupProps = gppGroup["Properties"];

            // check what the entry is doing to the group and turn it into real word
            string groupAction = gppGroupProps["@action"].ToString();
            groupAction = Utility.GetActionString(groupAction);

            // get the group name and a bunch of other details:
            assessedGroup.Add("Name", Utility.GetSafeString(gppGroup, "@name"));
            //TODO if the name is an interesting group, make the finding more interesting.
            assessedGroup.Add("DateTime Changed", Utility.GetSafeString(gppGroup,"@changed"));
            assessedGroup.Add("Description", Utility.GetSafeString(gppGroupProps, "@description"));
            assessedGroup.Add("New Name", Utility.GetSafeString(gppGroupProps, "@newName"));
            assessedGroup.Add("Delete All Users", Utility.GetSafeString(gppGroupProps,"@deleteAllUsers"));
            assessedGroup.Add("Delete All Groups", Utility.GetSafeString(gppGroupProps,"@deleteAllGroups"));
            assessedGroup.Add("Remove Accounts", Utility.GetSafeString(gppGroupProps,"@removeAccounts"));
            assessedGroup.Add("Action", groupAction);

            JArray gppGroupMemberArray = new JArray();
            JToken members = gppGroupProps["Members"]["Member"];
            string membersType = members.Type.ToString();
            if (membersType == "Array")
            {
                foreach (JToken member in members.Children())
                {
                    gppGroupMemberArray.Add(GetAssessedGroupMember(member));
                }
            }
            else if (membersType == "Object")
            {
                gppGroupMemberArray.Add(GetAssessedGroupMember(members));
            }
            else
            {
                Utility.DebugWrite("Something went squirrely with Group Memberships");
                Utility.DebugWrite(members.Type.ToString());
                Utility.DebugWrite(" " + membersType + " ");
                Utility.DebugWrite(members.ToString());
            }
            assessedGroup.Add("Members", gppGroupMemberArray);

            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGroup = new JObject();
            }
            return assessedGroup;
        }

        private JObject GetAssessedGroupMember(JToken member)
        {
            JObject assessedMember = new JObject();
            assessedMember.Add("Name", Utility.GetSafeString(member, "@name"));
            assessedMember.Add("Action", Utility.GetSafeString(member, "@action"));
            string memberSid = Utility.GetSafeString(member, "@sid");
            if (memberSid.Length > 0)
            {
                assessedMember.Add("SID", memberSid);
                string resolvedSID = LDAPstuff.GetUserFromSid(memberSid);
                assessedMember.Add("Display Name From SID", resolvedSID);
            }
            return assessedMember;
        }
        
       private JObject GetAssessedShortcuts(JObject gppCategory)
       {
           JObject assessedShortcuts = new JObject();

           if (gppCategory["Shortcut"] is JArray)
           {
               foreach (JObject gppShortcuts in gppCategory["Shortcut"])
               {
                   JObject assessedShortcut = GetAssessedShortcut(gppShortcuts);
                   if (assessedShortcut.HasValues)
                   {
                       assessedShortcuts.Add(gppShortcuts["@uid"].ToString(), assessedShortcut);
                   }
               }
           }
           else
           {
               JObject gppShortcuts = (JObject)JToken.FromObject(gppCategory["Shortcut"]);
               JObject assessedShortcut = GetAssessedShortcut(gppShortcuts);
               if (assessedShortcut.HasValues)
               {
                   assessedShortcuts.Add(gppShortcuts["@uid"].ToString(), assessedShortcut);
               }
            }

           return assessedShortcuts;
       }

        private JObject GetAssessedShortcut(JObject gppShortcut)
       {
           int interestLevel = 3;
           JObject assessedShortcut = new JObject();
           JToken gppShortcutProps = gppShortcut["Properties"];
           assessedShortcut.Add("Name", gppShortcut["@name"].ToString());
           assessedShortcut.Add("Status", gppShortcut["@status"].ToString());
           assessedShortcut.Add("Changed", gppShortcut["@changed"].ToString());
           string gppShortcutAction = Utility.GetActionString(gppShortcutProps["@action"].ToString());
           assessedShortcut.Add("Action", gppShortcutAction);
           assessedShortcut.Add("Target Type", gppShortcutProps["@targetType"]);
           assessedShortcut.Add("Arguments", gppShortcutProps["@arguments"]);
           assessedShortcut.Add("Icon Path", gppShortcutProps["@iconPath"]);
           assessedShortcut.Add("Icon Index", gppShortcutProps["@iconIndex"]);
           assessedShortcut.Add("Working Directory", gppShortcutProps["@startIn"]);
           assessedShortcut.Add("Shortcut Path", gppShortcutProps["@shortcutPath"]);
           assessedShortcut.Add("Comment", gppShortcutProps["@comment"]);

            string targetPath = gppShortcutProps["@targetPath"].ToString();
           assessedShortcut.Add("Target Path", targetPath);
           //TODO some logic to check target path file perms and icon Path file perms
           if (GlobalVar.OnlineChecks && (targetPath.Length > 0))
           {
               bool writable = false;
               writable = Utility.CanIWrite(targetPath);
               if (writable)
               {
                   interestLevel = 10;
                   assessedShortcut.Add("From Path Writable", "True");
               }
           }

           // if it's too boring to be worth showing, return an empty jobj.
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedShortcut = new JObject();
           }
           return assessedShortcut;
       }

        private JObject GetAssessedScheduledTasks(JObject gppCategory)
        {
           JObject assessedGppSchedTasksAllJson = new JObject();

           JObject assessedScheduledTasksTasks = GetAssessedScheduledTasksTasks((JObject)gppCategory["Task"]);
           if (assessedScheduledTasksTasks != null)
           {
               JProperty assessedGppSchedTasksTaskProp = new JProperty("Scheduled Tasks", assessedScheduledTasksTasks);
               assessedGppSchedTasksAllJson.Add(assessedGppSchedTasksTaskProp);
           }
           
           JObject assessedScheduledTasksImmediateTasks = GetAssessedScheduledTasksImmediateTasks((JObject)gppCategory["ImmediateTaskV2"]);
           if (assessedScheduledTasksImmediateTasks != null)
           {
               JProperty assessedGppSchedTasksImmediateTasksProp = new JProperty("Scheduled Tasks (Immediate Tasks)", assessedScheduledTasksImmediateTasks);
               assessedGppSchedTasksAllJson.Add(assessedGppSchedTasksImmediateTasksProp);
           }
            if (assessedGppSchedTasksAllJson.HasValues)
           {
               return assessedGppSchedTasksAllJson;
           }
           else
           {
               return null;
           }
       }

        private JObject GetAssessedScheduledTasksTasks(JObject scheduledTasks)
        {
            JObject assessedScheduledTasksTasks = new JObject();
            Utility.DebugWrite(scheduledTasks.ToString());
            return assessedScheduledTasksTasks;
        }

        private JObject GetAssessedScheduledTasksImmediateTasks(JObject scheduledTasksImmediateTask)
        {
            JObject assessedScheduledTasksImmediateTasks = new JObject();
            Utility.DebugWrite(scheduledTasksImmediateTask.ToString());
            return assessedScheduledTasksImmediateTasks;
        }

       private JObject GetAssessedRegistrySettings(JObject gppCategory)
       {
           int interestLevel = 2;
           JProperty gppRegSettingsProp = new JProperty("RegSettings", gppCategory["Registry"]);
           JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppRegSettings = new JObject();
           }
            return assessedGppRegSettings;
       }


       private JObject GetAssessedDrives(JObject gppCategory)
       {
           int interestLevel = 2;
           JProperty gppDriveProp = new JProperty("Drive", gppCategory["Drive"]);
           JObject assessedGppDrives = new JObject(gppDriveProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppDrives = new JObject();
           }
           return assessedGppDrives;
       }


       private JObject GetAssessedEnvironmentVariables(JObject gppCategory)
       {
           int interestLevel = 1;
           JProperty gppEVProp = new JProperty("EnvironmentVariable", gppCategory["EnvironmentVariable"]);
           JObject assessedGppEVs = new JObject(gppEVProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppEVs = new JObject();
           }
           return assessedGppEVs;
       }

        private JObject GetAssessedNTServices(JObject gppCategory)
       {
           int interestLevel = 3;
           JProperty ntServiceProp = new JProperty("NTService", gppCategory["NTService"]);
           JObject assessedNtServices = new JObject(ntServiceProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedNtServices = new JObject();
           }
            return assessedNtServices;
       }

       private JObject GetAssessedNetworkOptions(JObject gppCategory)
       {
           int interestLevel = 1;
           JProperty gppNetworkOptionsProp = new JProperty("DUN", gppCategory["DUN"]);
           JObject assessedGppNetworkOptions = new JObject(gppNetworkOptionsProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppNetworkOptions = new JObject();
           }
            return assessedGppNetworkOptions;
       }

       private JObject GetAssessedFolders(JObject gppCategory)
       {
           int interestLevel = 1;
           JProperty gppFoldersProp = new JProperty("Folder", gppCategory["Folder"]);
           JObject assessedGppFolders = new JObject(gppFoldersProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppFolders = new JObject();
           }
            return assessedGppFolders;
       }

       private JObject GetAssessedNetworkShareSettings(JObject gppCategory)
       {
           int interestLevel = 1;
           JProperty gppNetSharesProp = new JProperty("NetShare", gppCategory["NetShare"]);
           JObject assessedGppNetShares = new JObject(gppNetSharesProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppNetShares = new JObject();
           }
            return assessedGppNetShares;
       }


       private JObject GetAssessedIniFiles(JObject gppCategory)
       {
           int interestLevel = 2;
           JProperty gppIniFilesProp = new JProperty("Ini", gppCategory["Ini"]);
           JObject assessedGppIniFiles = new JObject(gppIniFilesProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppIniFiles = new JObject();
           }
            return assessedGppIniFiles;
       }
    }
}
