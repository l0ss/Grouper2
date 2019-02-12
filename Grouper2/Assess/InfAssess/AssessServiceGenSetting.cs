using System.Collections.Generic;
using System.DirectoryServices;
using Grouper2.SddlParser;
using Newtonsoft.Json.Linq;

namespace Grouper2.InfAssess
{
    internal static partial class AssessInf
    {
        public static JObject AssessServiceGenSetting(JToken svcGenSettings)
        {
            JObject svcGenSettingsJObject = (JObject)svcGenSettings;

            JObject assessedSvcGenSettings = new JObject();

            int inc = 0;

            foreach (KeyValuePair<string, JToken> svcGenSetting in svcGenSettingsJObject)
            {
                inc++;
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

                JObject assessedSddl = new JObject();
                // go parse the SDDL
                if (sddl.Length > 4)
                {
                    JObject parsedSddl = ParseSddl.ParseSddlString(sddl, SecurableObjectType.WindowsService);

                    // then assess the results based on interestLevel
                    

                    if (parsedSddl["Owner"] != null)
                    {
                        assessedSddl.Add("Owner", parsedSddl["Owner"].ToString());
                        interestLevel = 2;
                    }

                    if (parsedSddl["Group"] != null)
                    {
                        assessedSddl.Add("Group", parsedSddl["Group"].ToString());
                        interestLevel = 2;
                    }

                    if (parsedSddl["DACL"] != null)
                    {
                        JObject assessedDacl = new JObject();

                        string[] boringSidEndings = new string[]
                        {
                            "-3-0", "-5-9", "5-18", "-512", "-519", "SY", "BA", "DA", "CO", "ED", "PA", "CG", "DD",
                            "EA", "LA",
                        };
                        string[] interestingSidEndings = new string[]
                            {"DU", "WD", "IU", "BU", "AN", "AU", "BG", "DC", "DG", "LG"};
                        string[] interestingRights = new string[] {"WRITE_PROPERTY", "WRITE_DAC", "WRITE_OWNER"};

                        foreach (JProperty ace in parsedSddl["DACL"].Children())
                        {
                            int aceInterestLevel = 0;
                            string trusteeSid = ace.Value["SID"].ToString();

                            bool boringUserPresent = false;

                            bool interestingRightPresent = false;

                            foreach (string interestingRight in interestingRights)
                            {
                                foreach (JToken right in ace.Value["Rights"])
                                {
                                    if (interestingRight == right.ToString())
                                    {
                                        interestingRightPresent = true;
                                        break;
                                    }

                                    if (interestingRightPresent) break;
                                }
                            }

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

                            // first look if both match
                            if (interestingUserPresent && interestingRightPresent)
                            {
                                aceInterestLevel = 10;
                            }
                            // then skip if they're dumb defaults
                            else if (interestingRightPresent && boringUserPresent)
                            {
                                aceInterestLevel = 0;
                            }
                            // then catch all the non-default but high-privs
                            else if (interestingRightPresent && !interestingUserPresent)
                            {
                                aceInterestLevel = 7;
                            }
                            // then give them a nudge if they're non-default
                            else if (interestingUserPresent && !interestingRightPresent)
                            {
                                aceInterestLevel = 1;
                            }

                            if (aceInterestLevel >= GlobalVar.IntLevelToShow)
                            {
                                // pass the whole thing on
                                assessedSddl.Add(ace);
                            }
                        }

                        if ((assessedDacl != null) && (assessedDacl.HasValues))
                        {
                            assessedSddl.Add("DACL", assessedDacl);
                        }
                    }
                    
                }

                if (interestLevel >= GlobalVar.IntLevelToShow)
                {
                    if (assessedSddl.HasValues)
                    {
                        assessedSddl.AddFirst(new JProperty("Service", serviceName));
                        assessedSddl.Add("Startup Type", startupString);
                        assessedSvcGenSettings.Add(inc.ToString(), assessedSddl);
                    }
                }
            }

            if (assessedSvcGenSettings.Count <= 0)
            {
                return null;
            }

            return assessedSvcGenSettings;
        }
    }
}