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
            JObject investigatedIconPath = Utility.InvestigatePath(gppShortcutProps["@iconPath"].ToString());
            if (investigatedIconPath["InterestLevel"] != null)
            {
                if ((int)investigatedIconPath["InterestLevel"] > interestLevel)
                {
                    interestLevel = (int)investigatedIconPath["InterestLevel"];
                }
            }
            
            assessedShortcut.Add("Icon Path", investigatedIconPath);
            assessedShortcut.Add("Icon Index", gppShortcutProps["@iconIndex"]);
            assessedShortcut.Add("Working Directory", gppShortcutProps["@startIn"]);
            assessedShortcut.Add("Comment", gppShortcutProps["@comment"]);

            JObject investigatedShortcutPath = Utility.InvestigatePath(gppShortcutProps["@shortcutPath"].ToString());
            if (investigatedShortcutPath["InterestLevel"] != null)
            {
                if ((int) investigatedShortcutPath["InterestLevel"] > interestLevel)
                {
                    interestLevel = (int) investigatedShortcutPath["InterestLevel"];
                }
            }

            JObject investigatedTargetPath = Utility.InvestigatePath(gppShortcutProps["@shortcutPath"].ToString());
            if (investigatedTargetPath["InterestLevel"] != null)
            {
                if ((int) investigatedTargetPath["InterestLevel"] > interestLevel)
                {
                    interestLevel = (int) investigatedTargetPath["InterestLevel"];
                }
            }

            assessedShortcut.Add("Shortcut Path", investigatedShortcutPath);
            assessedShortcut.Add("Target Path", investigatedTargetPath);

            // if it's too boring to be worth showing, return an empty jobj.
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
            }

            return assessedShortcut;
        }
    }
}