using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Grouper2.Host.DcConnection;
using Grouper2.Host.DcConnection.Sddl;
using Grouper2.Host.SysVol;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Gpo = Grouper2.Host.SysVol.Gpo;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        private Ldap _netconn;
        private AuditReport AuditReport { get; set; }
        public int InterestLevel { get; }
        public bool DebugMode { get; }
        
        
        // bag in which to put the audited files
        public ConcurrentBag<AuditedScript> Scripts { get; }
        public ConcurrentBag<AuditFileResult> ConcurrentFindings { get; }

        

        public GrouperAuditor()
        {
            this._netconn = Ldap.Use();
            AuditReport = new AuditReport();
            InterestLevel = JankyDb.Vars.Interest;
            this.DebugMode = JankyDb.Vars.DebugMode;
            this.Scripts = new ConcurrentBag<AuditedScript>();
            this.ConcurrentFindings = new ConcurrentBag<AuditFileResult>();
        }

        public AuditReport GetReport(string runtime)
        {
            // add the findings
            foreach (AuditFileResult finding in ConcurrentFindings)
            {
                foreach (AdPolicy policy in this.AuditReport.CurrentPolicies.Values)
                {
                    if (string.Equals(finding.ParentGpoUid, policy.GpoProperties.Uid))
                    {
                        switch (finding.MachineOrUser)
                        {
                            case SysvolObjectType.UserDirectory:
                                policy.GpoFindings.UserFindings.Add(finding.FileFinding);
                                break;
                            case SysvolObjectType.MachineDirectory:
                                policy.GpoFindings.MachineFindings.Add(finding.FileFinding);
                                break;
                        }
                    }
                }
            }

            // add the scripts
            foreach (AuditedScript script in Scripts) this.AuditReport.Scripts.Add(script);

            return this.AuditReport;
        }

        internal void AuditGpo(Gpo gpo)
        {
            // exit early if it is policy definitions
            if (gpo.Path.Contains("PolicyDefinitions")) return;

            // create a JObject to put the stuff we find for this GPO into.
            AdPolicy gpoReport = new AdPolicy
            {
                Path = gpo.Path,
                GpoProperties = new Properties {Uid = gpo.Uid}
            };

            // If we're online and talking to the domain, just use that data for the props
            if (_netconn.CanSendTraffic)
                try
                {
                    // select the GPO's details from the gpo data we got
                    if (_netconn.DomainGpos.Count(gpo1 => gpo1.Uid.Equals(gpo.Uid)) == 1)
                    {
                        Gpo dcGpo = _netconn.DomainGpos.Where(g => g.Uid.Equals(gpo.Uid)).First();

                        // fill in props we got from the domain
                        gpoReport.GpoProperties.Created = dcGpo.Created;
                        gpoReport.GpoProperties.DistinguishedName = dcGpo.DistinguishedName;
                        gpoReport.GpoProperties.EnabledStatus = dcGpo.GpoStatus;
                        gpoReport.GpoProperties.Name = dcGpo.DisplayName;
                        gpoReport.GpoProperties.AclsReport = AuditSddl(gpo.GpoAcls);
                        gpoReport.GpoPackages = dcGpo.GpoPackages.Where(p => p.ParentUid.Equals(gpo.Uid)).ToList();
                    }
                    else
                    {
                        Output.DebugWrite("Couldn't get GPO Properties from the domain for the following GPO: " +
                                          gpo.Uid);
                    }
                }
                catch (ArgumentNullException e)
                {
                    Output.DebugWrite(e.ToString());
                }

            // add this gpo to the final report
            AuditReport.AddPolicyReport(gpo.Path, gpoReport);
        }


        internal JObject AuditSddl(Sddl sddl)
        {
            JObject parsedSddl = sddl.ToJObject();
            JObject gpoAclJObject = new JObject();

            foreach (KeyValuePair<string, JToken> thing in parsedSddl)
            {
                if (thing.Key == "Owner" && thing.Value.ToString() != "DOMAIN_ADMINISTRATORS")
                {
                    gpoAclJObject.Add("Owner", thing.Value.ToString());
                    continue;
                }

                if (thing.Key == "Group" && thing.Value.ToString() != "DOMAIN_ADMINISTRATORS")
                {
                    gpoAclJObject.Add("Group", thing.Value);
                    continue;
                }

                if (thing.Key == "DACL")
                {
                    foreach (JProperty ace in thing.Value.Children())
                    {
                        int aceInterestLevel = 1;
                        bool interestingRightPresent = false;
                        if (ace.Value["Rights"] != null)
                        {
                            string[] intRightsArray0 = new string[]
                            {
                                        "WRITE_OWNER", "CREATE_CHILD", "WRITE_PROPERTY", "WRITE_DAC", "SELF_WRITE", "CONTROL_ACCESS"
                            };

                            foreach (string right in intRightsArray0)
                            {
                                if (ace.Value["Rights"].Contains(right))
                                {
                                    interestingRightPresent = true;
                                }
                            }
                        }

                        string trusteeSid = ace.Value["SID"].ToString();
                        string[] boringSidEndings = new string[]
                            {"-3-0", "-5-9", "5-18", "-512", "-519", "SY", "BA", "DA", "CO", "ED", "PA", "CG", "DD", "EA", "LA",};
                        string[] interestingSidEndings = new string[]
                            {"DU", "WD", "IU", "BU", "AN", "AU", "BG", "DC", "DG", "LG"};

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

                        if (interestingUserPresent && interestingRightPresent)
                        {
                            aceInterestLevel = 10;
                        }
                        else if (boringUserPresent)
                        {
                            aceInterestLevel = 0;
                        }

                        if (aceInterestLevel >= InterestLevel)
                        {
                            // pass the whole thing on
                            gpoAclJObject.Add(ace);
                        }
                    }
                }

            }

            //add the JObject to our blob of data about the gpo
            if (gpoAclJObject.HasValues)
            {
                return gpoAclJObject;
            }

            return null;
        }
    }
}