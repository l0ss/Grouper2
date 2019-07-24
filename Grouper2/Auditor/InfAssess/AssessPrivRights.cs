using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Grouper2.Host;
using Grouper2.Host.DcConnection;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {

        private int TransformTrusteeIntoInterestLevel(string trusteeSidString, int currentInterest, bool elevatedInterest)
        {
            string trusteeClean = trusteeSidString.Trim('*');
                                
            // try to resolve to a known sid
            var trusteeSid = Sid.CheckSid(trusteeClean);
            if (trusteeSid != null)
            {
                if (trusteeSid.CanonicalPrivLevel.Equals("Low") && elevatedInterest)
                {
                    return 10;
                }
                if (trusteeSid.CanonicalPrivLevel.Equals("High"))
                {
                    return 0;
                }
            }

            return currentInterest;
        }
        
        
        
        public List<AuditedPrivRight> AssessPrivRights(JToken privRights)
        {
            if (privRights == null) 
                throw new ArgumentNullException(nameof(privRights));
            // create an object to put the results in
            List<AuditedPrivRight> ret = new List<AuditedPrivRight>();

            //iterate over the entries
            foreach (JProperty privRight in privRights.Children<JProperty>())
            {
                foreach (PrivRight intPrivRight in JankyDb.Db.PrivRights)
                {
                    // if the priv is interesting
                    if ((string) intPrivRight.PrivRightPrivRight == privRight.Name)
                    {
                        bool privIsInteresting = false;
                        // build out a possible object to add to findings
                        AuditedPrivRight auditedRight = new AuditedPrivRight
                        {
                            Name = privRight.Name,
                            Description = GetOsPrivDescription(privRight.Name)
                        };
                        // adjust the name if it is interesting
                        if (auditedRight.Description.EndsWith("I"))
                        {
                            auditedRight.Description = auditedRight.Name.Trim('I');
                            privIsInteresting = true;
                        }

                        //then do an evaluation for each trustee it's granted to
                        List<TrusteeKvp> trustees = new List<TrusteeKvp>();
                        if (privRight.Value is JArray)
                        {
                            foreach (JToken trusteeJToken in privRight.Value)
                            {
                                string trustee = trusteeJToken.ToString();
                                
                                // get an interest level from them
                                int interestLevel = 
                                    TransformTrusteeIntoInterestLevel(trustee, 3, privIsInteresting);
                                // if the interest is high enough
                                if (interestLevel >= this.InterestLevel)
                                {
                                    // get data bout them
                                    TrusteeKvp trusteeData = this._netconn.GetTrusteeKvp(trustee);
                                    if (trusteeData != null)
                                    {
                                        auditedRight.TryBumpInterest(interestLevel);
                                        trustees.Add(trusteeData);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // extract the trustee
                            string trustee = privRight.Value.ToString().Trim('*');
                            // get an interest level from them
                            int interestLevel = 
                                TransformTrusteeIntoInterestLevel(trustee, 2, privIsInteresting);
                            // if the interest is high enough
                            if (interestLevel >= this.InterestLevel)
                            {
                                // get data bout them
                                TrusteeKvp trusteeData = this._netconn.GetTrusteeKvp(trustee);
                                if (trusteeData != null)
                                {
                                    auditedRight.TryBumpInterest(interestLevel);
                                    trustees.Add(trusteeData);
                                }
                            }
                        }

                        // add the results to our jobj of trustees if they are interesting enough.
                        if (trustees.Count > 0)
                        {
                            auditedRight.Trustees = trustees;
                            ret.Add(auditedRight);
                        }
                    }
                }
            }

            // only return an object if we found anything
            return ret.Count > 0 
                ? ret 
                : null;
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
    }
}