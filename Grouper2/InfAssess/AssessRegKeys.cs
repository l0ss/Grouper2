using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using Grouper2;
using Newtonsoft.Json.Linq;
using Sddl.Parser;
using System.Security.AccessControl;
using Microsoft.Win32;

internal static partial class AssessInf
{
    public static JObject AssessRegKeys(JToken regKeys)
    {
        // These are actually ACLs being set on reg keys using SDDL.

        // The first value is inheritance rules:

        // 2= replace existing permissions on all subkeys with inheritable permissions
        // 1= Do not allow permissions on this key to be replace.
        // 0= Propagate inheritable permissions to all subkeys.

        JObject regKeysJObject = (JObject) regKeys;
        int interestLevel = 1;

        JObject assessedRegKeys = new JObject();

        foreach (KeyValuePair<string, JToken> regKey in regKeysJObject)
        {
            interestLevel = 1;
            string keyPath = regKey.Key.Trim('"');
            JArray keyValues = (JArray) regKey.Value;
            string inheritance = keyValues[0].ToString().Trim('"');
            string sddl = keyValues[1].ToString().Trim('"');

            // turn the inheritance number into a nice string.
            string inheritanceString = "";
            switch (inheritance)
            {
                case "0":
                    inheritanceString = "Propagate inheritable permissions to all subkeys.";
                    break;
                case "1":
                    inheritanceString = "Do not allow permissions on this key to be replaced.";
                    break;
                case "2":
                    inheritanceString = "Replace existing permissions on all subkeys with inheritable permissions.";
                    break;
            }

            // go parse the SDDL
            JObject parsedSddl = ParseSDDL.ParseSddlString(sddl, SecurableObjectType.RegistryKey);

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
            }

            if (parsedSddl["Group"] != null)
            {
                assessedSddl.Add("Group", parsedSddl["Group"].ToString());
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
                    assessedSddl.Add("DACL", assessedDacl);
                };
            }

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                if (assessedSddl.HasValues)
                {
                    assessedRegKeys.Add(keyPath, new JObject(
                        new JProperty("Permissions", assessedSddl),
                        new JProperty("Inheritance", inheritanceString)
                    ));
                }
            }
        }

        if (assessedRegKeys.Count <= 0)
        {
            return null;
        }
        
        return assessedRegKeys;
    }
}