using System;
using System.Collections.Generic;
using Grouper2.Host.DcConnection.Sddl;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        public Dictionary<string, AuditedServiceGenSetting> AssessServiceGenSetting(JToken svcGenSettings)
        {
            if (svcGenSettings == null) 
                throw new ArgumentNullException(nameof(svcGenSettings));
            JObject svcGenSettingsJObject = (JObject)svcGenSettings;

            // JObject assessedSvcGenSettings = new JObject();
            Dictionary<string, AuditedServiceGenSetting> auditedServiceGenSettings = new Dictionary<string, AuditedServiceGenSetting>();
            int inc = 0;

            foreach (KeyValuePair<string, JToken> svcGenSetting in svcGenSettingsJObject)
            {
                // basic setup
                inc++;
                int interestLevel = 3;
                JArray svcSettings = (JArray)svcGenSetting.Value;

                // make the return obj and init with the service name
                AuditedServiceGenSetting serviceGenSetting = new AuditedServiceGenSetting()
                {
                    Service = svcGenSetting.Key.Trim('"', '\\'),
                };

                // extract the service startup type enum value and process it into a string
                switch (svcSettings[0].ToString().Trim('"', '\\'))
                {
                    case "2":
                        serviceGenSetting.Startup = "Automatic";
                        break;
                    case "3":
                        serviceGenSetting.Startup = "Manual";
                        break;
                    case "4":
                        serviceGenSetting.Startup = "Disabled";
                        break;
                    default:
                        serviceGenSetting.Startup = null;
                        break;
                }

                //JObject assessedSddl = new JObject();

                // go parse the SDDL
                string sddl = svcSettings[1].ToString().Trim('"', '\\');
                if (sddl.Length > 4)
                {
                    Sddl parsedSddl = ParseSddl.ParseSddlString(sddl, SecurableObjectType.WindowsService);

                    // then assess the results based on interestLevel

                    if (parsedSddl.Owner != null)
                    {
                        serviceGenSetting.Owner = parsedSddl.Owner;
                        interestLevel = 2;
                    }

                    if (parsedSddl.Group != null)
                    {
                        serviceGenSetting.Group = parsedSddl.Group;
                        interestLevel = 2;
                    }

                    if (parsedSddl.Dacl != null)
                    {
                        string[] boringSidEndings = new string[]
                        {
                            "-3-0", "-5-9", "5-18", "-512", "-519", "SY", "BA", "DA", "CO", "ED", "PA", "CG", "DD",
                            "EA", "LA",
                        };
                        string[] interestingSidEndings = new string[]
                            {"DU", "WD", "IU", "BU", "AN", "AU", "BG", "DC", "DG", "LG"};
                        string[] interestingRights = new string[] {"WRITE_PROPERTY", "WRITE_DAC", "WRITE_OWNER"};

                        foreach (Ace ace in parsedSddl.Dacl.Aces)
                        {
                            int aceInterestLevel = 0;
                            string trusteeSid = ace.AceSid.Raw;

                            bool boringUserPresent = false;

                            bool interestingRightPresent = false;

                            foreach (string interestingRight in interestingRights)
                            {
                                foreach (string right in ace.Rights)
                                {
                                    if (string.Equals(interestingRight, right))
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

                            if (aceInterestLevel >= this.InterestLevel)
                            {
                                // pass the whole thing on
                                serviceGenSetting.Aces.Add(ace);
                            }
                        }

                        // null it out to prevent json output if no elements
                        if (serviceGenSetting.Aces.Count == 0)
                        {
                            serviceGenSetting.Aces = null;
                        }
                    }
                    
                }

                // check the current interest
                if (interestLevel >= this.InterestLevel)
                {
                    // ensure there were key values added
                    // if they are all missing, continue to the next one
                    if (serviceGenSetting.Owner == null 
                        && serviceGenSetting.Group == null 
                        && serviceGenSetting.Aces == null)
                        continue;

                    auditedServiceGenSettings.Add(inc.ToString(), serviceGenSetting);
                    
                }
            }

            // return the dict if we actually collected anything of note
            return auditedServiceGenSettings.Count <= 0 
                ? null 
                : auditedServiceGenSettings;
        }
    }
}