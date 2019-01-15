using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
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
                    if (GlobalVar.DebugMode)
                    {
                        Utility.DebugWrite("Failed to find method: GetAssessed" + assessName);
                    }
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
                    JObject assessedFile = GetAssessedFile(gppFile);
                    if (assessedFile != null)
                    {
                        assessedFiles.Add(gppFile["@uid"].ToString(), assessedFile);
                    }
                }
            }
            else
            {
                JObject gppFile = (JObject)JToken.FromObject(gppCategory["File"]);
                JObject assessedFile = GetAssessedFile(gppFile);
                if (assessedFile != null)
                {
                    assessedFiles.Add(gppFile["@uid"].ToString(), assessedFile);
                }
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
            JToken targetPathJToken = gppFileProps["@targetPath"];
            if (targetPathJToken != null)
            {
                assessedFile.Add("Target Path", gppFileProps["@targetPath"].ToString());
            }

            JToken fromPathJToken = gppFileProps["@fromPath"];
            if (fromPathJToken != null)
            {
                string fromPath = gppFileProps["@fromPath"].ToString();
                
                if (GlobalVar.OnlineChecks && (fromPath.Length > 0))
                {
                    JObject assessedPath = Utility.InvestigatePath(gppFileProps["@fromPath"].ToString());
                    assessedFile.Add("From Path", assessedPath);
                    if (assessedPath["InterestLevel"] != null)
                    {
                        int pathInterest = (int)assessedPath["InterestLevel"];
                        interestLevel = interestLevel + pathInterest;
                    }
                }
                else
                {
                    assessedFile.Add("From Path", fromPath);
                }
            }

            // if it's too boring to be worth showing, return an empty jobj.
            if (interestLevel <= GlobalVar.IntLevelToShow)
            {
                return null;
            }
            return assessedFile;
        }

        private JObject GetAssessedGroups(JObject gppCategory)
        {
            JObject assessedGroups = new JObject();
            JObject assessedUsers = new JObject();

            if (gppCategory["Group"] != null)
            {
                if (gppCategory["Group"] is JArray)
                {
                    foreach (JObject gppGroup in gppCategory["Group"])
                    {
                        JObject assessedGroup = GetAssessedGroup(gppGroup);
                        if (assessedGroup != null)
                        {
                            assessedGroups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                        }
                    }
                }
                else
                {
                    JObject gppGroup = (JObject) JToken.FromObject(gppCategory["Group"]);
                    JObject assessedGroup = GetAssessedGroup(gppGroup);
                    if (assessedGroup != null)
                    {
                        assessedGroups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                    }
                }
            }

            JObject assessedGppGroups = (JObject)JToken.FromObject(assessedGroups);

            if (gppCategory["User"] != null)
            {
                if (gppCategory["User"] is JArray)
                {
                    foreach (JObject gppUser in gppCategory["User"])
                    {
                        JObject assessedUser = GetAssessedUser(gppUser);
                        if (assessedUser != null)
                        {
                            assessedUsers.Add(gppUser["@uid"].ToString(), assessedUser);
                        }
                    }
                }
                else
                {
                    JObject gppUser = (JObject) JToken.FromObject(gppCategory["User"]);
                    JObject assessedUser = GetAssessedUser(gppUser);
                    if (assessedUser != null)
                    {
                        assessedUsers.Add(gppUser["@uid"].ToString(), assessedUser);
                    }
                }
            }

            JObject assessedGppUsers = (JObject)JToken.FromObject(assessedUsers);
            
            JProperty assessedUsersJson = new JProperty("GPP Users", assessedGppUsers);
            JProperty assessedGroupsJson = new JProperty("GPP Groups", assessedGppGroups);
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
                return null;
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
            if (gppGroupProps["Members"] != null)
            {
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
                    if (GlobalVar.DebugMode)
                    {
                        Utility.DebugWrite("Something went squirrely with Group Memberships");
                        Utility.DebugWrite(members.Type.ToString());
                        Utility.DebugWrite(" " + membersType + " ");
                        Utility.DebugWrite(members.ToString());
                    }
                }
            }

            assessedGroup.Add("Members", gppGroupMemberArray);

            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
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
                if (GlobalVar.OnlineChecks)
                {
                    string resolvedSID = LDAPstuff.GetUserFromSid(memberSid);
                    assessedMember.Add("Display Name From SID", resolvedSID);
                }
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
           string arguments = gppShortcutProps["@arguments"].ToString();
           assessedShortcut.Add("Arguments", arguments);
           assessedShortcut.Add("Icon Path", Utility.InvestigatePath(gppShortcutProps["@iconPath"].ToString()));
           assessedShortcut.Add("Icon Index", gppShortcutProps["@iconIndex"]);
           assessedShortcut.Add("Working Directory", gppShortcutProps["@startIn"]);
           assessedShortcut.Add("Comment", gppShortcutProps["@comment"]);

           if (GlobalVar.OnlineChecks)
           {
               assessedShortcut.Add("Shortcut Path",
                   Utility.InvestigatePath(gppShortcutProps["@shortcutPath"].ToString()));
               assessedShortcut.Add("Target Path", Utility.InvestigatePath(gppShortcutProps["@targetPath"].ToString()));
           }
           else
           {
               assessedShortcut.Add("Shortcut Path", gppShortcutProps["@shortcutPath"].ToString());
               assessedShortcut.Add("Target Path", gppShortcutProps["@targetPath"].ToString());
            }

           // TODO get the interest levels from the results of InvestigatePath

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

            //Console.WriteLine("");
            //Utility.DebugWrite(gppCategory.ToString());
            //Console.WriteLine("");

            List<string> schedTaskTypes = new List<string>();
            schedTaskTypes.Add("Task");
            schedTaskTypes.Add("TaskV2");
            schedTaskTypes.Add("ImmediateTask");
            schedTaskTypes.Add("ImmediateTaskV2");

            foreach (string schedTaskType in schedTaskTypes)
            { 
                if (gppCategory[schedTaskType] is JArray)
                {
                    foreach (JToken taskJToken in gppCategory[schedTaskType])
                    {
                        JObject assessedGppSchedTask = GetAssessedScheduledTask(taskJToken, schedTaskType);
                        if (assessedGppSchedTask != null)
                        {
                            assessedGppSchedTasksAllJson.Add(assessedGppSchedTask["UID"].ToString(),
                                assessedGppSchedTask);
                        }
                    }
                }
                else if (gppCategory[schedTaskType] is JObject)
                {
                    JObject assessedGppSchedTask = GetAssessedScheduledTask(gppCategory[schedTaskType], schedTaskType);
                    if (assessedGppSchedTask != null)
                    {
                        assessedGppSchedTasksAllJson.Add(assessedGppSchedTask["UID"].ToString(), assessedGppSchedTask);
                    }
                }
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

        private JObject GetAssessedScheduledTask(JToken scheduledTask, string schedTaskType)
        {
            int interestLevel = 4;
            
            JObject assessedScheduledTask = new JObject();

            assessedScheduledTask.Add("Name", scheduledTask["@name"].ToString());
            assessedScheduledTask.Add("UID", scheduledTask["@uid"].ToString());
            assessedScheduledTask.Add("Type", schedTaskType);
            assessedScheduledTask.Add("Changed", scheduledTask["@changed"].ToString());
            if (scheduledTask["Properties"]["@runAs"] != null)
            {
                assessedScheduledTask.Add("Run As", Utility.GetSafeString(scheduledTask["Properties"], "@runAs"));
            }
            string cPassword = Utility.GetSafeString(scheduledTask["Properties"], "@cpassword");
            if (cPassword.Length > 1)
            {
                assessedScheduledTask.Add("Encrypted Password", Utility.GetSafeString(scheduledTask["Properties"], "@cpassword"));
                assessedScheduledTask.Add("Decrypted Password", Utility.DecryptCpassword(cPassword));
                interestLevel = 10;
            }

            if (scheduledTask["Properties"]["@logonType"] != null)
            {
                assessedScheduledTask.Add("Logon Type",
                    Utility.GetSafeString(scheduledTask["Properties"], "@logonType"));
            }
            // handle the entries that are specific to some task types but not others
            // both taskv2 and immediatetaskv2 have the same rough structure
            if (schedTaskType.EndsWith("V2"))
            {
                assessedScheduledTask.Add("Action", Utility.GetActionString(scheduledTask["Properties"]["@action"].ToString()));
                assessedScheduledTask.Add("Description", Utility.GetSafeString(scheduledTask, "@desc"));
                assessedScheduledTask.Add("Enabled", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Settings"], "Enabled"));
                // just adding the Triggers info raw, there are way too many options.
                assessedScheduledTask.Add("Triggers", scheduledTask["Properties"]["Task"]["Triggers"]);

                if (scheduledTask["Properties"]["Task"]["Actions"]["ShowMessage"] != null)
                {
                    assessedScheduledTask.Add(
                        new JProperty("Action - Show Message", new JObject(
                            new JProperty("Title", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["ShowMessage"], "Title")),
                            new JProperty("Body", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["ShowMessage"], "Body"))
                            )
                        )
                    );
                }

                if (scheduledTask["Properties"]["Task"]["Actions"]["Exec"] != null)
                {
                    string commandString = Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["Exec"],
                        "Command");
                    string argumentsString =
                        Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["Exec"], "Arguments");

                    JObject command = new JObject(new JProperty("Command", commandString));
                    JObject arguments = new JObject(new JProperty("Arguments", argumentsString));

                    if (GlobalVar.OnlineChecks)
                    {
                        command = Utility.InvestigatePath(commandString);
                        arguments = Utility.InvestigateString(argumentsString);
                        if (arguments["InterestLevel"] != null)
                        {
                            int argumentInterest = (int) arguments["InterestLevel"];
                            interestLevel = interestLevel + argumentInterest;
                        }

                        if (command["InterestLevel"] != null)
                        {
                            int commandInterest = (int) command["InterestLevel"];
                            interestLevel = interestLevel + commandInterest;
                        }
                    }
                    
                assessedScheduledTask.Add(
                        new JProperty("Action - Execute Command", new JObject(
                            new JProperty("Command", command),
                            new JProperty("Arguments", arguments)
                            )
                        )
                    );
                }

                if (scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"] != null)
                {
                    string attachmentString = Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"]["Attachments"], "File");

                    JObject attachment = new JObject(new JProperty("Attachment", attachmentString));

                    if (GlobalVar.OnlineChecks)
                    {
                        attachment = Utility.InvestigatePath(attachmentString);
                        if (attachment["InterestLevel"] != null)
                        {
                            int attachmentInterest = (int) attachment["InterestLevel"];
                            interestLevel = interestLevel + attachmentInterest;
                        }
                    }

                    assessedScheduledTask.Add(
                        new JProperty("Action - Send Email", new JObject(
                            new JProperty("From", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"], "From")),
                            new JProperty("To", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"], "To")),
                            new JProperty("Subject", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"], "Subject")),
                            new JProperty("Body", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"], "Body")),
                            new JProperty("Header Fields", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"], "HeaderFields")),
                            new JProperty("Attachment", attachment),
                            new JProperty("Server", Utility.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"], "Server"))
                            )
                        )
                    );
                }
            }
            
            if (schedTaskType == "Task")
            {
                string commandString = Utility.GetSafeString(scheduledTask["Properties"], "@appname");
                string argumentsString = Utility.GetSafeString(scheduledTask["Properties"], "@args");
                JObject command = new JObject(new JProperty("Command", commandString));
                JObject arguments = new JObject(new JProperty("Arguments", argumentsString));

                if (GlobalVar.OnlineChecks)
                {
                    command = Utility.InvestigatePath(commandString);
                    arguments = Utility.InvestigateString(argumentsString);

                    if (arguments["InterestLevel"] != null)
                    {
                        int argumentInterest = (int) arguments["InterestLevel"];
                        interestLevel = interestLevel + argumentInterest;
                    }

                    if (command["InterestLevel"] != null)
                    {
                        int commandInterest = (int) command["InterestLevel"];
                        interestLevel = interestLevel + commandInterest;
                    }
                }

                assessedScheduledTask.Add("Action", Utility.GetActionString(scheduledTask["Properties"]["@action"].ToString()));
                assessedScheduledTask.Add("Command", command);
                assessedScheduledTask.Add("Args", arguments);
                assessedScheduledTask.Add("Start In", Utility.GetSafeString(scheduledTask["Properties"], "@startIn"));

                if (scheduledTask["Properties"]["Triggers"] != null)
                {
                    assessedScheduledTask.Add("Triggers", scheduledTask["Properties"]["Triggers"]);
                }
                
            }

            if (schedTaskType == "ImmediateTask")
            {
                string argumentsString = Utility.GetSafeString(scheduledTask["Properties"], "@args");
                string commandString = Utility.GetSafeString(scheduledTask["Properties"], "@appName");
                JObject command = new JObject(new JProperty("Command", commandString));
                JObject arguments = new JObject(new JProperty("Arguments", argumentsString));

                if (GlobalVar.OnlineChecks)
                {
                    command = Utility.InvestigatePath(commandString);
                    arguments = Utility.InvestigateString(argumentsString);

                    if (arguments["InterestLevel"] != null)
                    {
                        int argumentInterest = (int) arguments["InterestLevel"];
                        interestLevel = interestLevel + argumentInterest;
                    }

                    if (command["InterestLevel"] != null)
                    {
                        int commandInterest = (int) command["InterestLevel"];
                        interestLevel = interestLevel + commandInterest;
                    }
                }

                assessedScheduledTask.Add("Command", command);
                assessedScheduledTask.Add("Arguments", arguments);
                assessedScheduledTask.Add("Start In", Utility.GetSafeString(scheduledTask["Properties"], "@startIn"));
                assessedScheduledTask.Add("Comment", Utility.GetSafeString(scheduledTask["Properties"], "@comment"));
            }

            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
            }

            return assessedScheduledTask;
        }


       private JObject GetAssessedRegistrySettings(JObject gppCategory)
       {
           int interestLevel = 0;
           if (gppCategory["Collection"] != null)
           {
               JProperty gppRegSettingsProp = new JProperty("RegSettingsColl", gppCategory["Collection"]);
               JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
               if (interestLevel < GlobalVar.IntLevelToShow)
               {
                   assessedGppRegSettings = new JObject();
               }
               return assessedGppRegSettings;
            }
           if (gppCategory["Registry"] != null)
           {
               JProperty gppRegSettingsProp = new JProperty("RegSettingsReg", gppCategory["Registry"]);
               JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
               if (interestLevel < GlobalVar.IntLevelToShow)
               {
                   assessedGppRegSettings = new JObject();
               }
               return assessedGppRegSettings;
           }
           if (gppCategory["RegistrySettings"] != null)
           {
               JProperty gppRegSettingsProp = new JProperty("RegSettingsRegSet", gppCategory["RegistrySettings"]);
               JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
               if (interestLevel < GlobalVar.IntLevelToShow)
               {
                   assessedGppRegSettings = new JObject();
               }
               return assessedGppRegSettings;
            }
           else
           {
               Utility.DebugWrite("Something fucked up.");
               Utility.DebugWrite(gppCategory.ToString());
               return null;
           }
       }


       private JObject GetAssessedDrives(JObject gppCategory)
       {
           // dont forget cpasswords
           int interestLevel = 0;
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
           int interestLevel = 0;
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
           // dont forget cpasswords

            int interestLevel = 0;
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
           int interestLevel = 0;
           JProperty gppNetworkOptionsProp = new JProperty("DUN", gppCategory["DUN"]);
           JObject assessedGppNetworkOptions = new JObject(gppNetworkOptionsProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppNetworkOptions = new JObject();
           }
            return assessedGppNetworkOptions;
       }

       private JObject GetAssessedPrinters(JObject gppCategory)
       {
           // dont forget cpasswords
           int interestLevel = 0;
           JProperty gppSharedPrintersProp = new JProperty("SharedPrinter", gppCategory["SharedPrinter"]);
           JObject assessedGppSharedPrinters = new JObject(gppSharedPrintersProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppSharedPrinters = new JObject();
           }
           return assessedGppSharedPrinters;
       }

       private JObject GetAssessedDataSources(JObject gppCategory)
       {
           // dont forget cpasswords
           int interestLevel = 0;
           JProperty gppDataSourcesProp = new JProperty("DataSource", gppCategory["DataSource"]);
           JObject assessedGppDataSources = new JObject(gppDataSourcesProp);
           if (interestLevel < GlobalVar.IntLevelToShow)
           {
               assessedGppDataSources = new JObject();
           }
           return assessedGppDataSources;
       }

        private JObject GetAssessedFolders(JObject gppCategory)
       {
           int interestLevel = 0;
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
           int interestLevel = 0;
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
           int interestLevel = 0;
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
