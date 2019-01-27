using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grouper2;
using Newtonsoft.Json.Linq;
using Sddl.Parser;

internal static partial class AssessInf
{
    public static JObject AssessServiceGenSetting(JToken svcGenSettings)
    {
        JObject svcGenSettingsJObject = (JObject)svcGenSettings;

        JObject assessedSvcGenSettings = new JObject();

        foreach (KeyValuePair<string, JToken> svcGenSetting in svcGenSettingsJObject)
        {
            int interestLevel = 3;
            string serviceName = svcGenSetting.Key.Trim('"','\\');
            JArray svcSettings = (JArray)svcGenSetting.Value;
            string startupType = svcSettings[0].ToString().Trim('"','\\');
            string sddl = svcSettings[1].ToString().Trim('"','\\');
            
            string startupString = "";
            switch (startupType)
            {
                case "2":
                    startupString = "Automatic";
                    break;
                case "3":
                    startupString = "Manual";
                    break;
                case "4":
                    startupString = "Disabled";
                    break;
            }

            // go parse the SDDL
            if (GlobalVar.OnlineChecks)
            {
                JObject parsedSddl = ParseSDDL.ParseSddlString(sddl, SecurableObjectType.WindowsService);


                // then assess the results based on interestLevel
                JObject assessedSddl = new JObject();

                string[] defaultSids = new string[]
                {
                    "CREATOR_OWNER",
                    "World Authority",
                    "LOCAL_SYSTEM",
                    "BUILTIN_ADMINISTRATORS",
                    "Flags"
                };

                if (parsedSddl["Owner"] != null)
                {
                    assessedSddl.Add("Owner", parsedSddl["Owner"].ToString());
                    interestLevel = 6;
                }

                if (parsedSddl["Group"] != null)
                {
                    assessedSddl.Add("Group", parsedSddl["Group"].ToString());
                    interestLevel = 6;
                }

                if (parsedSddl["DACL"] != null)
                {
                    JObject assessedDacl = new JObject();
                    foreach (KeyValuePair<string, JToken> ace in JObject.FromObject(parsedSddl["DACL"]))
                    {
                        // unless we are at interest level zero (show all defaults)
                        bool boringSidMatch = false;
                        foreach (string boringSid in defaultSids)
                        {
                            if (ace.Key.Contains(boringSid)) boringSidMatch = true;
                        }

                        if ((boringSidMatch) && (GlobalVar.IntLevelToShow > 0))
                        {
                            continue;
                        }

                        else
                        {
                            assessedDacl.Add(ace.Key, ace.Value);
                        }
                    }

                    if (assessedDacl.HasValues)
                    {
                        interestLevel = 6;
                        assessedSddl.Add("DACL", assessedDacl);
                    }

                    ;
                }

                if (interestLevel >= GlobalVar.IntLevelToShow)
                {
                    if (assessedSddl.HasValues)
                    {
                        assessedSvcGenSettings.Add(serviceName, new JObject(
                            new JProperty("Permissions", assessedSddl),
                            new JProperty("Startup Type", startupString)
                        ));
                    }
                }
            }
            else
            {
                assessedSvcGenSettings.Add(serviceName, new JObject(
                    new JProperty("SDDL", sddl),
                    new JProperty("Startup Type", startupString)
                    ));
            }
        }

        if (assessedSvcGenSettings.Count <= 0)
        {
            return null;
        }

        return assessedSvcGenSettings;
    }
}