using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.InfAssess
{
    internal static partial class AssessInf
    {
        public static JObject AssessPrivRights(JToken privRights)
        {
            JObject jankyDb = JankyDb.Instance;
            JArray intPrivRights = (JArray) jankyDb["privRights"];

            // create an object to put the results in
            JObject assessedPrivRights = new JObject();

            //set an intentionally non-matchy domainSid value unless we doing online checks.
            string domainSid = "X";
            if (GlobalVar.OnlineChecks)
            {
                domainSid = LDAPstuff.GetDomainSid();
            }

            //iterate over the entries
            foreach (JProperty privRight in privRights.Children<JProperty>())
            {
                foreach (JToken intPrivRight in intPrivRights)
                {
                    // if the priv is interesting
                    if ((string) intPrivRight["privRight"] == privRight.Name)
                    {
                        bool privIsInteresting = false;
                        string privRightDesc = GetOsPrivDescription(privRight.Name);
                        if (privRightDesc.EndsWith("I"))
                        {
                            privRightDesc = privRightDesc.Trim('I');
                            privIsInteresting = true;
                        }
                        //create a jobj to put the trustees into
                        JObject trustees = new JObject();
                        //then for each trustee it's granted to
                        if (privRight.Value is JArray)
                        {
                            foreach (JToken trusteeJToken in privRight.Value)
                            {
                                int interestLevel = 3;
                                string trustee = trusteeJToken.ToString();
                                string trusteeClean = trustee.Trim('*');
                                string trusteeHighOrLow = Sid.GetWKSidHighOrLow(trusteeClean);
                                if ((trusteeHighOrLow == "Low") && privIsInteresting)
                                {
                                    interestLevel = 10;
                                }
                                if (trusteeHighOrLow == "High")
                                {
                                    interestLevel = 0;
                                }
                                if (interestLevel >= GlobalVar.IntLevelToShow)
                                {
                                    trustees.Add(GetTrustee(trusteeClean));
                                }
                            }
                        }
                        else
                        {
                            int interestLevel = 2;
                            string trusteeClean = privRight.Value.ToString().Trim('*');
                            string trusteeHighOrLow = Sid.GetWKSidHighOrLow(trusteeClean);
                            if ((trusteeHighOrLow == "Low") && privIsInteresting)
                            {
                                interestLevel = 10;
                            }
                            if (trusteeHighOrLow == "High")
                            {
                                interestLevel = 0;
                            }
                            if (interestLevel >= GlobalVar.IntLevelToShow)
                            {
                                trustees.Add(GetTrustee(trusteeClean));
                            }
                        }

                        // add the results to our jobj of trustees if they are interesting enough.
                        if (trustees.HasValues)
                        {
                            trustees.Add("Description", privRightDesc);
                            assessedPrivRights.Add(new JProperty(privRight.Name, trustees));
                        }
                    }
                }
            }

            return assessedPrivRights;
        }

        static string GetOsPrivDescription(string privilege)
        {
            Dictionary<string, string> osPrivDescriptions = new Dictionary<string, string>
            {
                {"SeAssignPrimaryTokenPrivilege", "Required to assign the primary token of a process. I" },
                {"SeBackupPrivilege", "This privilege causes the system to grant all read access control to any file. I"},
                {"SeCreateTokenPrivilege", "Required to create a primary token. I"},
                {"SeDebugPrivilege", "Required to debug and adjust the memory of a process owned by another account. I" },
                {"SeLoadDriverPrivilege", "Required to load or unload a device driver. I"},
                {"SeRestorePrivilege", "This privilege causes the system to grant all write access control to any file. I" },
                {"SeTakeOwnershipPrivilege", "Required to take ownership of an object without being granted discretionary access. I" },
                {"SeTcbPrivilege", "This privilege identifies its holder as part of the trusted computer base. I" },
                {"SeSyncAgentPrivilege", "Synchronize directory service data. I"},
                {"SeTrustedCredManAccessPrivilege", "Access Credential Manager as a trusted caller. I" },
                {"SeCreatePermanentPrivilege", "Create permanent shared objects. I"},
                {"SeDelegateSessionUserImpersonatePrivilege","Required to obtain an impersonation token for another user in the same session. I" },
                {"SeEnableDelegationPrivilege","Required to mark user and computer accounts as trusted for delegation. I" },
                {"SeMachineAccountPrivilege", "Required to create computer accounts in the domain. I" },
                {"SeManageVolumePrivilege", "Required to enable volume management privileges." },
                {"SeRelabelPrivilege", "Modify the mandatory integrity level of an object." },
                {"SeBatchLogonRight","Required for an account to log on using the batch logon type." },
                {"SeDenyInteractiveLogonRight", "Explicitly denies an account the right to log on using the interactive logon type." },
                {"SeDenyRemoteInteractiveLogonRight", "Explicitly denies an account the right to log on remotely using the interactive logon type." },
                {"SeDenyServiceLogonRight", "Explicitly denies an account the right to log on using the service logon type." },
                {"SeInteractiveLogonRight", "Required for an account to log on using the interactive logon type."},
                {"SeNetworkLogonRight", "Required for an account to log on using the network logon type."},
                {"SeRemoteInteractiveLogonRight", "Required for an account to log on remotely using the interactive logon type. I" },
                {"SeServiceLogonRight", "Required for an account to log on using the service logon type." }
            };

            if (osPrivDescriptions.ContainsKey(privilege))
            {
                return osPrivDescriptions[privilege];
            }
            
            return "";
        }

        static JProperty GetTrustee(string trustee)
        {
            string displayName = "";
            // clean up the trustee SID

           
           string resolvedSid = LDAPstuff.GetUserFromSid(trustee);
           displayName = resolvedSid;
           

            return new JProperty(trustee, displayName);
        }
    }
}