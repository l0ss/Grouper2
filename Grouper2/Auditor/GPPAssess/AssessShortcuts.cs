using System;
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
        public Finding Audit(Shortcuts file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));

            return GetAssessedShortcuts(file.JankyXmlStuff);
        }
        private AuditedShortcuts GetAssessedShortcuts(JObject gppCategory)
        {
            AuditedShortcuts shortcuts = new AuditedShortcuts();

            if (gppCategory["Shortcuts"] != null)
            {
                JToken gppShortcutsBlob = gppCategory["Shortcuts"];

                if (gppShortcutsBlob["Shortcut"] is JArray)
                {
                    foreach (JToken gppShortcuts in gppShortcutsBlob["Shortcut"])
                    {
                        try
                        {
                            AuditedShortcut assessedShortcut = GetAssessedShortcut(gppShortcuts as JObject);
                            if (assessedShortcut != null)
                            {
                                shortcuts.Contents.Add(gppShortcuts["@uid"].ToString(), assessedShortcut);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Degub($"unable to assess or add to the dict {shortcuts.Contents.ToString()}", e, this);
                            continue;
                        }
                    }
                }
                else
                {
                    if (gppShortcutsBlob["Shortcut"] != null)
                    {
                        JObject gppShortcuts = (JObject) JToken.FromObject(gppShortcutsBlob["Shortcut"]);
                        try
                        {
                            AuditedShortcut assessedShortcut = GetAssessedShortcut(gppShortcuts as JObject);
                            if (assessedShortcut != null)
                            {
                                shortcuts.Contents.Add(gppShortcuts["@uid"].ToString(), assessedShortcut);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Degub($"unable to assess or add to the dict {shortcuts.Contents.ToString()}", e, this);
                            return null;
                        }
                    }
                }

                if (shortcuts.Contents != null && shortcuts.Contents.Count > 0)
                {
                    return shortcuts;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        private AuditedShortcut GetAssessedShortcut(JObject gppShortcut)
        {
            // setup
            // int interestLevel = 3;
            JToken gppShortcutProps = gppShortcut["Properties"];

            // build the initial return object
            AuditedShortcut auditedShortcut = new AuditedShortcut
            {
                //Uid = gppShortcut["@uid"].ToString(),
                Name = JUtil.GetSafeString(gppShortcut, "@name"),
                Interest = 3,
                Status = JUtil.GetSafeString(gppShortcut, "@status"),
                Changed = JUtil.GetSafeString(gppShortcut, "@changed"),
                Action = JUtil.GetActionString(gppShortcutProps["@action"].ToString()),
                TargetType = JUtil.GetSafeString(gppShortcutProps, "@targetType"),
                Arguments = JUtil.GetSafeString(gppShortcutProps, "@arguments"),
                IconIndex = JUtil.GetSafeString(gppShortcutProps, "@iconIndex"),
                Comment = JUtil.GetSafeString(gppShortcutProps, "@comment")
            };

            // set the current interest level based on any argument that were found
            if (auditedShortcut.Arguments != null)
            {
                AuditedString investigatedArguments = FileSystem.InvestigateString(auditedShortcut.Arguments, this.InterestLevel);
                auditedShortcut.TryBumpInterest(investigatedArguments);
            }

            // audit the icon path
            auditedShortcut.IconPath = 
                FileSystem.InvestigatePath(JUtil.GetSafeString(gppShortcutProps, "@iconPath"));
            // determine whether to adjust the interest level
            auditedShortcut.TryBumpInterest(auditedShortcut.IconPath);

            // determine whether to assess the working directory
            string workingDir = JUtil.GetSafeString(gppShortcutProps, "@startIn");
            if (workingDir != null)
            {
                // attempt to audit the working dir path
                auditedShortcut.WorkingDir = FileSystem.InvestigatePath(workingDir);
            }

            // attempt to audit the shortcut and target paths
            auditedShortcut.ShortcutPath = FileSystem.InvestigatePath(JUtil.GetSafeString(gppShortcutProps, "@shortcutPath"));
            auditedShortcut.TargetPath = FileSystem.InvestigatePath(JUtil.GetSafeString(gppShortcutProps, "@targetPath"));

            // determine whether to adjust the interest level for the above audits
            auditedShortcut.TryBumpInterest(auditedShortcut.ShortcutPath);
            auditedShortcut.TryBumpInterest(auditedShortcut.TargetPath);

            // if it's too boring to be worth showing, return anull.
            return auditedShortcut.Interest < this.InterestLevel 
                ? null 
                : auditedShortcut;
        }
    }
}