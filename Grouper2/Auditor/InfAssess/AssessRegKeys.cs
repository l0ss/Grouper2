using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Grouper2.Host.DcConnection;
using Grouper2.Host.DcConnection.Sddl;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// These are actually ACLs being set on reg keys using SDDL.
        ///
        /// The first value is inheritance rules:
        ///
        /// 2= replace existing permissions on all subkeys with inheritable permissions
        /// 1= Do not allow permissions on this key to be replace.
        /// 0= Propagate inheritable permissions to all subkeys.
        /// </summary>
        /// <param name="regKeys"></param>
        /// <param name="desiredInterestLevel"></param>
        /// <returns></returns>
        public Dictionary<string, AuditedRegistryKeys> AssessRegKeys(JToken regKeys)
        {
            if (regKeys == null) 
                throw new ArgumentNullException(nameof(regKeys));
            // These are actually ACLs being set on reg keys using SDDL.

            // The first value is inheritance rules:

            // 2= replace existing permissions on all subkeys with inheritable permissions
            // 1= Do not allow permissions on this key to be replace.
            // 0= Propagate inheritable permissions to all subkeys.
            Dictionary<string, AuditedRegistryKeys> AuditedKeys = new Dictionary<string, AuditedRegistryKeys>();
            
            JObject regKeysJObject = (JObject)regKeys;

            int inc = 0;

            foreach (KeyValuePair<string, JToken> regKey in regKeysJObject)
            {
                AuditedRegistryKeys keyUnderAudit = new AuditedRegistryKeys();
                inc++;
                int interestLevel = 1;
                JArray keyValues = (JArray)regKey.Value;
                string inheritance = keyValues[0].ToString().Trim('"');
                string sddl = keyValues[1].ToString().Trim('"');

                // turn the inheritance number into a nice string.
                if (inheritance == "0")
                    keyUnderAudit.Inheritance = "Propagate inheritable permissions to all subkeys.";
                else if (inheritance == "1")
                    keyUnderAudit.Inheritance = "Do not allow permissions on this key to be replaced.";
                else if (inheritance == "2")
                    keyUnderAudit.Inheritance = "Replace existing permissions on all subkeys with inheritable permissions.";
                else // otherwise null the value to prevent output during deserialisation
                    keyUnderAudit.Inheritance = null;

                // go parse the SDDL
                Sddl parsedSddl = ParseSddl.ParseSddlString(sddl, SecurableObjectType.WindowsService);
                if (parsedSddl == null)
                {
                    continue;
                }

                if (sddl.Length > 4)
                {
                    keyUnderAudit = new AuditedRegistryKeys()
                    {
                        KeyPath = regKey.Key.Trim('"')
                    };
                    if (parsedSddl.Owner != null 
                        && parsedSddl.Owner.Raw != null 
                        && !string.IsNullOrWhiteSpace(parsedSddl.Owner.Raw))
                    {
                        keyUnderAudit.Owner = parsedSddl.Owner;
                        interestLevel = 4;
                    }
                    else
                    {
                        keyUnderAudit.Owner = new SddlSid("ERROR");
                    }

                    if (parsedSddl.Group != null 
                        && parsedSddl.Group.Raw != null && !string.IsNullOrWhiteSpace(parsedSddl.Group.Raw))
                    {
                        keyUnderAudit.Group = parsedSddl.Group;
                        interestLevel = 4;
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

                        foreach (Ace ace in parsedSddl.Dacl.Aces)
                        {
                            int aceInterestLevel = 0;
                            string trusteeSid = ace.AceSid.Raw;

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

                            if (interestingUserPresent /* && interestingRightPresent*/)
                            {
                                aceInterestLevel = 10;
                            }
                            else if (boringUserPresent)
                            {
                                aceInterestLevel = 0;
                            }

                            if (aceInterestLevel >= this.InterestLevel)
                            {
                                // pass the whole thing on
                                keyUnderAudit.Aces.Add(ace);
                            }
                        }
                    }
                }

                if (interestLevel >= this.InterestLevel)
                {
                    if (keyUnderAudit.Aces.Count > 0)
                    {
                        AuditedKeys.Add(inc.ToString(), keyUnderAudit);
                    }
                }
            }

            // if there are objects to return, so do, otherwise null
            return AuditedKeys.Count > 0 
                ? AuditedKeys
                : null;
        }
    }
}