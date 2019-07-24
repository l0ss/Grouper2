using System.Collections.Generic;
using System.Linq;
using Grouper2.Host.DcConnection.Sddl;
using Grouper2.Host.SysVol.Files;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        private JObject JObjectifySddl(Sddl sddl)
        {
            JObject parsedSddl = new JObject();

            if (sddl.Owner != null)
            {
                parsedSddl.Add("Owner", sddl.Owner.Alias);
            }

            if (sddl.Group != null)
            {
                parsedSddl.Add("Group", sddl.Group.Alias);
            }

            if (sddl.Dacl != null)
            {
                parsedSddl.Add("DACL", JobjectifyAcl(sddl.Dacl));
            }
            /*
            if (Sacl != null)
            {
                parsedSDDL.Add("SACL", Sacl.ToString());
            }*/

            return parsedSddl;
        }
        private JObject JobjectifyAcl(Acl acl)
        {
            bool anyFlags = acl.Flags != null && acl.Flags.Any();
            bool anyAces = acl.Aces != null && acl.Aces.Any();

            JObject parsedAcl = new JObject();

            if (anyFlags)
            {
                //parsedAcl.Add("Flags", JArray.FromObject(Flags));
            }

            if (anyAces)
            {
                int inc = 0;
                foreach (Ace ace in acl.Aces)
                {
                    JObject parsedAce = new JObject();

                    JArray aceFlagsJArray = new JArray();
                    if (ace.AceFlags != null)
                    {
                        aceFlagsJArray = JArray.FromObject(ace.AceFlags);
                    }

                    string aceSidAlias = ace.AceSid.Alias;
                    string aceSidRaw = ace.AceSid.Raw;
                    string aceType;
                    if (ace.AceType == "ACCESS_ALLOWED" || ace.AceType == "OBJECT_ACCESS_ALLOWED")
                    {
                        aceType = "Allow";
                    }
                    else if (ace.AceType == "ACCESS_DENIED" || ace.AceType == "OBJECT_ACCESS_DENIED")
                    {
                        aceType = "Deny";
                    }
                    else
                    {
                        aceType = ace.AceType;
                    }

                    Dictionary<string, string> boringRights = new Dictionary<string, string>()
                    {
                        { "READ_CONTROL", "Read ACL"},
                        { "SYNCHRONIZE", "Synchronize"},
                        { "GENERIC_EXECUTE", "Execute"},
                        { "GENERIC_READ", "Read"},
                        { "READ_PROPERTY", "Read Property"},
                        { "LIST_CHILDREN", "List Children"},
                        { "LIST_OBJECT", "List Object"}
                    };

                    Dictionary<string, string> interestingRights = new Dictionary<string, string>()
                    {
                        {"KEY_ALL","Full Control"},
                        {"DELETE_TREE", "Delete Tree"},
                        {"STANDARD_DELETE", "Delete"},
                        {"CREATE_CHILD", "Create Child"},
                        {"DELETE_CHILD", "Delete Child"},
                        {"WRITE_PROPERTY", "Write Property"},
                        {"GENERIC_ALL", "Full Control"},
                        {"GENERIC_WRITE", "Write"},
                        {"WRITE_DAC", "Write ACL"},
                        {"WRITE_OWNER", "Write Owner"},
                        {"STANDARD_RIGHTS_ALL", "Full Control"},
                        {"STANDARD_RIGHTS_REQUIRED", "Delete, Write DACL, Write Owner"},
                        {"CONTROL_ACCESS", "Extended Rights"},
                        {"SELF_WRITE", "Self Write"}
                    };

                    JArray aceRightsJArray = new JArray();
                    foreach (string right in ace.Rights)
                    {
                        // if the right is interesting, we'll take it
                        if (interestingRights.ContainsKey(right))
                        {
                            aceRightsJArray.Add(interestingRights[right]);
                            continue;
                        }
                        // if it's boring and we're not showing defaults, we'll skip it.
                        else if (boringRights.ContainsKey(right) && InterestLevel > 0)
                        {
                            continue;
                        }
                        else if (boringRights.ContainsKey(right) && InterestLevel == 0)
                        {
                            aceRightsJArray.Add(boringRights[right]);
                            continue;
                        }
                        else
                        {
                            // KEY_READ will land here. not sure what to do with it cos reading the right registry key is of course very interesting, but those cases are going to be pretty rare.
                            // Might have to handle further downstream?
                            Utility.Output.DebugWrite(right + " was not defined as either boring or interesting. Consider adding it to the dicts in Acl.cs?");
                            aceRightsJArray.Add(right);
                        }
                    }

                    string displayName = _netconn.GetUserFromSid(aceSidRaw);
                    parsedAce.Add("SID", aceSidRaw);
                    parsedAce.Add("Name", displayName);
                    parsedAce.Add("Type", aceType);
                    if (aceRightsJArray.Count > 1)
                    {
                        parsedAce.Add("Rights", aceRightsJArray);
                    }
                    else if (aceRightsJArray.Count == 1)
                    {
                        parsedAce.Add("Rights", aceRightsJArray[0].ToString());
                    }
                    if (aceFlagsJArray.Count > 1)
                    {
                        parsedAce.Add("Flags", aceFlagsJArray);
                    }
                    else if (aceFlagsJArray.Count == 1)
                    {
                        parsedAce.Add("Flags", aceFlagsJArray[0].ToString());
                    }

                    parsedAcl.Add(new JProperty(inc.ToString(), parsedAce));
                    inc++;
                }
            }
            return parsedAcl;
        }
    }
}