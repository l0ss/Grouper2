using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        public AuditedSystemAccess AssessSysAccess(JToken sysAccess)
        {
            if (sysAccess == null) 
                throw new ArgumentNullException(nameof(sysAccess));

            AuditedSystemAccess assessedSysAccess = new AuditedSystemAccess();
        
            if (this.InterestLevel <= 1 && sysAccess["MinimumPasswordAge"] != null)
            {
                if (sysAccess["MinimumPasswordAge"].ToString() != "1")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Minimum Password Age",
                        Result = sysAccess["MinimumPasswordAge"].ToString() + " days"
                    });
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["MaximumPasswordAge"] != null)
            {
                if (sysAccess["MaximumPasswordAge"].ToString() != "42")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Maximum Password Age",
                        Result = sysAccess["MaximumPasswordAge"].ToString() + " days"
                    });
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["MinimumPasswordLength"] != null)
            {
                if (sysAccess["MinimumPasswordLength"].ToString() != "7")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Minimum Password Length",
                        Result = sysAccess["MinimumPasswordLength"].ToString() + " characters"
                    });
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["PasswordComplexity"] != null)
            {
                if (sysAccess["PasswordComplexity"].ToString() != "1")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Password complexity rules enforced",
                        Result = sysAccess["PasswordComplexity"].ToString()
                    });
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["PasswordHistorySize"] != null)
            {
                if (sysAccess["PasswordHistorySize"].ToString() != "24")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Password History Size",
                        Result = sysAccess["PasswordHistorySize"].ToString()
                    });
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["LockoutBadCount"] != null)
            {
                if (sysAccess["LockoutBadCount"].ToString() != "5")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Invalid password attempts before lockout",
                        Result = sysAccess["LockoutBadCount"].ToString()
                    });
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["ResetLockoutCount"] != null)
            {
                if (sysAccess["ResetLockoutCount"].ToString() != "30")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Invalid attempt counter resets after",
                        Result = sysAccess["ResetLockoutCount"].ToString() + " minutes"
                    });
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["LockoutDuration"] != null)
            {
                if (sysAccess["LockoutDuration"].ToString() != "30")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Account unlocks after",
                        Result = sysAccess["LockoutDuration"].ToString() + " minutes"
                    });
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["ForceLogoffWhenHourExpire"] != null)
            {
                if (sysAccess["ForceLogoffWhenHourExpire"].ToString() != "0")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Forcibly disconnect sessions outside logon hours",
                        Result = "True"
                    });
                }
            }
            if (this.InterestLevel <= 4 && sysAccess["NewAdministratorName"] != null)
            {
                if (sysAccess["NewAdministratorName"] != null)
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "New Administrator account name",
                        Result = sysAccess["NewAdministratorName"].ToString().Trim('"', '\\')
                    });
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["NewGuestName"] != null)
            {
                if (sysAccess["NewGuestName"] != null)
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "New Guest account name",
                        Result = sysAccess["NewGuestName"].ToString().Trim('"', '\\')
                    });
                }
            }
            if (this.InterestLevel <= 7 && sysAccess["ClearTextPassword"] != null)
            {
                if (sysAccess["ClearTextPassword"].ToString() != "0")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Store passwords using reversible encryption",
                        Result = "True"
                    });
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["LSAAnonymousNameLookup"] != null)
            {
                if (sysAccess["LSAAnonymousNameLookup"].ToString() != "0")
                {
                    assessedSysAccess.Findings.Add(new AuditedSysAccessElement
                    {
                        Name = "Allow Anonymous access to local LSA policy",
                        Result = "True"
                    });
                }
            }

            if (!(assessedSysAccess.Findings.Count > 0))
            {
                assessedSysAccess = null;
            }
            return assessedSysAccess;
        }
    }
}