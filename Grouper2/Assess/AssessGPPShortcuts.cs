using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    public class AssessGPPShortcuts
    {
            public static JObject GetAssessedShortcuts(JObject GPPShortcuts)
            {
                JProperty GPPShortcutProp = new JProperty("Shortcut", GPPShortcuts["Shortcut"]);
                JObject AssessedGPPShortcuts = new JObject(GPPShortcutProp);
                return AssessedGPPShortcuts;
                //Utility.DebugWrite("GPP is about GPPShortcuts");
                //Console.WriteLine(GPPShortcuts["Shortcut"]);
            }
    }
}
