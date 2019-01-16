using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
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
                JObject gppShortcuts = (JObject) JToken.FromObject(gppCategory["Shortcut"]);
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
                assessedShortcut.Add("Target Path",
                    Utility.InvestigatePath(gppShortcutProps["@targetPath"].ToString()));
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
    }
}