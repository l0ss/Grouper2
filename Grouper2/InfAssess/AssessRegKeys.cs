using System.Collections.Generic;
using Grouper2.SddlParser;
using Newtonsoft.Json.Linq;

namespace Grouper2.InfAssess
{
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

            JObject assessedRegKeys = new JObject();

            int inc = 0;

            foreach (KeyValuePair<string, JToken> regKey in regKeysJObject)
            {
                inc++;
                int interestLevel = 1;
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

                // then assess the results based on interestLevel
                JObject assessedSddl = new JObject();

                // go parse the SDDL
                if (GlobalVar.OnlineChecks)
                {
                    JObject parsedSddl = ParseSddl.ParseSddlString(sddl, SecurableObjectType.WindowsService);
                

                    if (parsedSddl["Owner"] != null)
                    {
                        assessedSddl.Add("Owner", parsedSddl["Owner"].ToString());
                        interestLevel = 4;
                    }

                    if (parsedSddl["Group"] != null)
                    {
                        assessedSddl.Add("Group", parsedSddl["Group"].ToString());
                        interestLevel = 4;
                    }

                    JObject assessedDacl = new JObject();
                    if (parsedSddl["DACL"] != null)
                    {
                        string[] boringSidEndings = new string[]
                            {"-3-0", "-5-9", "5-18", "-512", "-519", "SY", "BA", "DA", "CO", "ED", "PA", "CG", "DD", "EA", "LA",};
                        string[] interestingSidEndings = new string[]
                            {"DU", "WD", "IU", "BU", "AN", "AU", "BG", "DC", "DG", "LG"};

                        foreach (JProperty ace in parsedSddl["DACL"].Children())
                        {
                            int aceInterestLevel = 0;
                            string trusteeSid = ace.Value["SID"].ToString();

                            bool boringUserPresent = false;
                            foreach (string boringSidEnding in boringSidEndings)
                            {
                                if (trusteeSid.EndsWith(boringSidEnding))
                                {
                                    boringUserPresent = true;
                                    break;
                                }
                            }

                            bool interestingUserPresent = false;
                            foreach (string interestingSidEnding in interestingSidEndings)
                            {
                                if (trusteeSid.EndsWith(interestingSidEnding))
                                {
                                    interestingUserPresent = true;
                                    break;
                                }
                            }

                            if (interestingUserPresent/* && interestingRightPresent*/)
                            {
                                aceInterestLevel = 10;
                            }
                            else if (boringUserPresent)
                            {
                                aceInterestLevel = 0;
                            }

                            if (aceInterestLevel >= GlobalVar.IntLevelToShow)
                            {
                                // pass the whole thing on
                                assessedSddl.Add(ace);
                            }
                        }

                        if ((assessedDacl != null) && assessedDacl.HasValues)
                        {
                            assessedSddl.Add("DACL", assessedDacl);
                        }
                    }
                
                }

                if (interestLevel >= GlobalVar.IntLevelToShow)
                {
                    if (assessedSddl.HasValues)
                    {
                        assessedSddl.AddFirst(new JProperty("RegKey", keyPath));
                        assessedSddl.Add("Inheritance", inheritanceString);
                        assessedRegKeys.Add(inc.ToString(), assessedSddl);
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
}