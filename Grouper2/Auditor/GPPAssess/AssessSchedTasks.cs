using System;
using System.Collections.Generic;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(ScheduledTasks file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedScheduledTasks(file.JankyXmlStuff);
        }
        private AuditedGppXmlSchedTasks GetAssessedScheduledTasks(JObject gppCategory)
        {
            AuditedGppXmlSchedTasks ret = new AuditedGppXmlSchedTasks();

            //Console.WriteLine("");
            //Utility.Output.DebugWrite(gppCategory.ToString());
            //Console.WriteLine("");

            List<string> schedTaskTypes = new List<string> {"Task", "TaskV2", "ImmediateTask", "ImmediateTaskV2"};

            foreach (string schedTaskType in schedTaskTypes)
            {
                if (gppCategory["ScheduledTasks"][schedTaskType] is JArray)
                {
                    foreach (JToken taskJToken in gppCategory["ScheduledTasks"][schedTaskType])
                    {
                        try
                        {
                            AuditedGppXmlSchedTasksTask assessedGppSchedTask = GetAssessedScheduledTask(taskJToken, schedTaskType);
                            if (assessedGppSchedTask != null)
                            {
                                ret.Tasks.Add(assessedGppSchedTask.Uid,
                                    assessedGppSchedTask);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Degub($"unable to assess scheduled tasks in this json {gppCategory}", e, gppCategory);
                            continue;
                        }
                    }
                }
                else if (gppCategory["ScheduledTasks"][schedTaskType] is JObject)
                {
                    try
                    {
                        AuditedGppXmlSchedTasksTask assessedGppSchedTask = GetAssessedScheduledTask(gppCategory["ScheduledTasks"][schedTaskType], schedTaskType);
                        if (assessedGppSchedTask != null)
                        {
                            if (string.IsNullOrEmpty(assessedGppSchedTask.Uid))
                            {
                                Log.Degub($"argh");
                            }
                            ret.Tasks.Add(assessedGppSchedTask.Uid,
                                assessedGppSchedTask);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Degub($"unable to assess scheduled tasks in this json {gppCategory}", e, gppCategory);
                        return null;
                    }
                }
            }

            // return an object only if there is something in it
            return ret.Tasks.Count > 0 
                ? ret 
                : null;
        }

        private AuditedGppXmlSchedTasksCommand ExtractCommandFromScheduledTask(JToken scheduledTask, int initialInterestLevel, int number = 0)
        {
            AuditedGppXmlSchedTasksCommand ret = new AuditedGppXmlSchedTasksCommand()
            {
                Interest = initialInterestLevel,
                Command = FileSystem.InvestigatePath(JUtil.GetSafeString(scheduledTask, "Command")),
                Args = FileSystem.InvestigateString(JUtil.GetSafeString(scheduledTask, "Arguments"), this.InterestLevel)
            };
            if (ret.Args != null)
            {
                ret.Interest += ret.Args.Interest;
            }

            if (ret.Command != null)
            {
                ret.Interest += ret.Command.Interest;
            }

            string caption = "Exec";
            if (number > 0)
            {
                caption += " - " + number;
            }

            ret.Caption = caption;

            return ret;
        }

        private AuditedGppXmlSchedTasksTask GetAssessedScheduledTask(JToken scheduledTask, string schedTaskType)
        {
            int interestLevel = 4;
            string uid = scheduledTask["@uid"].ToString();

            JObject assessedScheduledTask = new JObject
            {
                {"Name", scheduledTask["@name"].ToString()},
                {"UID", scheduledTask["@uid"].ToString()},
                {"Type", schedTaskType},
                {"Changed", scheduledTask["@changed"].ToString()}
            };

            if (scheduledTask["Properties"]["@runAs"] != null)
            {
                assessedScheduledTask.Add("Run As", JUtil.GetSafeString(scheduledTask["Properties"], "@runAs"));
            }

            string cPassword = JUtil.GetSafeString(scheduledTask["Properties"], "@cpassword");
            if (cPassword.Length > 1)
            {
                assessedScheduledTask.Add("Encrypted Password",
                    JUtil.GetSafeString(scheduledTask["Properties"], "@cpassword"));
                assessedScheduledTask.Add("Decrypted Password", Util.DecryptCpassword(cPassword));
                interestLevel = 10;
            }

            if (scheduledTask["Properties"]["@logonType"] != null)
            {
                assessedScheduledTask.Add("Logon Type",
                    JUtil.GetSafeString(scheduledTask["Properties"], "@logonType"));
            }

            // handle the entries that are specific to some task types but not others
            // both taskv2 and immediatetaskv2 have the same rough structure
            if (schedTaskType.EndsWith("V2"))
            {
                assessedScheduledTask.Add("Action",
                    JUtil.GetActionString(scheduledTask["Properties"]["@action"].ToString()));
                assessedScheduledTask.Add("Description", JUtil.GetSafeString(scheduledTask, "@desc"));
                assessedScheduledTask.Add("Enabled",
                    JUtil.GetSafeString(scheduledTask["Properties"]["Task"]["Settings"], "Enabled"));
                // just adding the Triggers info raw, there are way too many options.
                assessedScheduledTask.Add("Triggers", scheduledTask["Properties"]["Task"]["Triggers"]);

                if (scheduledTask["Properties"]["Task"]["Actions"]["ShowMessage"] != null)
                {
                    assessedScheduledTask.Add(
                        new JProperty("Action - Show Message", new JObject(
                                new JProperty("Title",
                                    JUtil.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["ShowMessage"],
                                        "Title")),
                                new JProperty("Body",
                                    JUtil.GetSafeString(scheduledTask["Properties"]["Task"]["Actions"]["ShowMessage"],
                                        "Body"))
                            )
                        )
                    );
                }

                if (scheduledTask["Properties"]["Task"]["Actions"]["Exec"] != null)
                {
                    // do we have an array of Command?
                    if (scheduledTask["Properties"]["Task"]["Actions"]["Exec"].Type == JTokenType.Array)
                    {
                        int i = 1;
                        foreach (JToken item in scheduledTask["Properties"]["Task"]["Actions"]["Exec"])
                        {
                            AuditedGppXmlSchedTasksCommand task = ExtractCommandFromScheduledTask(item, interestLevel, i);
                            interestLevel = task.Interest;
                            assessedScheduledTask.Add("Exec task " + i.ToString(), JObject.FromObject(task));
                            i++;
                        }
                    }
                    else
                    {
                        // or just one?
                        AuditedGppXmlSchedTasksCommand task = ExtractCommandFromScheduledTask(
                            scheduledTask["Properties"]["Task"]["Actions"]["Exec"], interestLevel);
                        interestLevel = task.Interest;
                        assessedScheduledTask.Add("Exec task ", JObject.FromObject(task));
                    }
                }

                JObject schedTaskEmailAction;
                try
                {
                    schedTaskEmailAction = scheduledTask["Properties"]["Task"]["Actions"]["SendEmail"] as JObject;
                }
                catch (Exception)
                {
                    schedTaskEmailAction = null;
                }
                

                if (schedTaskEmailAction != null)
                {
                    string attachmentString =
                        JUtil.GetSafeString(
                            schedTaskEmailAction, "File");

                    AuditedString attachment = null;
                    var settings = JankyDb.Vars;
                    if (settings.OnlineMode)
                    {
                        attachment = FileSystem.InvestigateString(attachmentString, this.InterestLevel);
                        if (attachment != null)
                        {
                            interestLevel += attachment.Interest;
                        }
                    }
                    
                    var emailSend = new SendEmailAction();
                    emailSend.From = JUtil.GetSafeString(schedTaskEmailAction,"From");
                    emailSend.To = JUtil.GetSafeString(schedTaskEmailAction, "To");
                    emailSend.Subject = JUtil.GetSafeString(schedTaskEmailAction, "Subject");
                    emailSend.Body = JUtil.GetSafeString(schedTaskEmailAction,"Body");
                    emailSend.Headers = JUtil.GetSafeString(schedTaskEmailAction, "HeaderFields");
                    emailSend.Attachment = attachment;
                    emailSend.Server = JUtil.GetSafeString(schedTaskEmailAction,"Server");

                    assessedScheduledTask.Add(new 
                        JProperty("Action - Send Email", 
                            JObject.FromObject(emailSend)));
                }
            }

            if (schedTaskType == "Task")
            {
                string commandString = JUtil.GetSafeString(scheduledTask["Properties"], "@appname");
                string argumentsString = JUtil.GetSafeString(scheduledTask["Properties"], "@args");

                AuditedPath command = FileSystem.InvestigatePath(commandString);
                AuditedString arguments = FileSystem.InvestigateString(argumentsString, this.InterestLevel);

                if (arguments != null)
                {
                    interestLevel += arguments.Interest;
                }

                if (command != null)
                {
                    interestLevel += command.Interest;
                }
                

                assessedScheduledTask.Add("Action",
                    JUtil.GetActionString(scheduledTask["Properties"]["@action"].ToString()));
                assessedScheduledTask.Add("Command", JObject.FromObject(command));
                assessedScheduledTask.Add("Args", JObject.FromObject(arguments));
                AuditedPath assessedWorkingDir =
                    FileSystem.InvestigatePath(JUtil.GetSafeString(scheduledTask["Properties"], "@startIn"));
                if (assessedWorkingDir != null)
                {
                    assessedScheduledTask.Add("Working Dir", JObject.FromObject(assessedWorkingDir));
                }
                
                if (scheduledTask["Properties"]["Triggers"] != null)
                {
                    assessedScheduledTask.Add("Triggers", scheduledTask["Properties"]["Triggers"]);
                }
            }

            if (schedTaskType == "ImmediateTask")
            {
                string argumentsString = JUtil.GetSafeString(scheduledTask["Properties"], "@args");
                string commandString = JUtil.GetSafeString(scheduledTask["Properties"], "@appName");


                AuditedPath command = FileSystem.InvestigatePath(commandString);
                AuditedString arguments = FileSystem.InvestigateString(argumentsString, this.InterestLevel);

                if (arguments != null)
                {
                    interestLevel += arguments.Interest;
                }

                if (command != null)
                {
                    interestLevel += command.Interest;
                }
                

                assessedScheduledTask.Add("Command", JObject.FromObject(command));
                assessedScheduledTask.Add("Arguments", JObject.FromObject(arguments));

                AuditedPath assessedWorkingDir =
                    FileSystem.InvestigatePath(JUtil.GetSafeString(scheduledTask["Properties"], "@startIn"));
                if (assessedWorkingDir != null)
                {
                    assessedScheduledTask.Add("Working Dir", JObject.FromObject(assessedWorkingDir));
                }

                assessedScheduledTask.Add("Comment", JUtil.GetSafeString(scheduledTask["Properties"], "@comment"));
            }
            
            AuditedGppXmlSchedTasksTask ret = new AuditedGppXmlSchedTasksTask()
            {
                Uid = uid,
                Interest = interestLevel,
                SchedTaskJObject = assessedScheduledTask
            };

            // only return the object if interest is high enough, otherwise null
            return interestLevel < this.InterestLevel 
                ? null 
                : ret;
        }
    }
}
