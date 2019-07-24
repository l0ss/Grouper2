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

            AuditedSystemAccess assessedSysAccess = new AuditedSystemAccess()
            {
                Interest = 0
            };
        
            if (this.InterestLevel <= 1 && sysAccess["MinimumPasswordAge"] != null)
            {
                if (sysAccess["MinimumPasswordAge"].ToString() != "1")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 1,
                        Name = "Minimum Password Age",
                        Result = sysAccess["MinimumPasswordAge"].ToString() + " days"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["MaximumPasswordAge"] != null)
            {
                if (sysAccess["MaximumPasswordAge"].ToString() != "42")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 1,
                        Name = "Maximum Password Age",
                        Result = sysAccess["MaximumPasswordAge"].ToString() + " days"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["MinimumPasswordLength"] != null)
            {
                if (sysAccess["MinimumPasswordLength"].ToString() != "7")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 1,
                        Name = "Minimum Password Length",
                        Result = sysAccess["MinimumPasswordLength"].ToString() + " characters"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["PasswordComplexity"] != null)
            {
                if (sysAccess["PasswordComplexity"].ToString() != "1")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 1,
                        Name = "Password complexity rules enforced",
                        Result = sysAccess["PasswordComplexity"].ToString()
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["PasswordHistorySize"] != null)
            {
                if (sysAccess["PasswordHistorySize"].ToString() != "24")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 1,
                        Name = "Password History Size",
                        Result = sysAccess["PasswordHistorySize"].ToString()
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["LockoutBadCount"] != null)
            {
                if (sysAccess["LockoutBadCount"].ToString() != "5")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 3,
                        Name = "Invalid password attempts before lockout",
                        Result = sysAccess["LockoutBadCount"].ToString()
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["ResetLockoutCount"] != null)
            {
                if (sysAccess["ResetLockoutCount"].ToString() != "30")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 3,
                        Name = "Invalid attempt counter resets after",
                        Result = sysAccess["ResetLockoutCount"].ToString() + " minutes"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["LockoutDuration"] != null)
            {
                if (sysAccess["LockoutDuration"].ToString() != "30")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 3,
                        Name = "Account unlocks after",
                        Result = sysAccess["LockoutDuration"].ToString() + " minutes"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["ForceLogoffWhenHourExpire"] != null)
            {
                if (sysAccess["ForceLogoffWhenHourExpire"].ToString() != "0")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 1,
                        Name = "Forcibly disconnect sessions outside logon hours",
                        Result = "True"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 4 && sysAccess["NewAdministratorName"] != null)
            {
                if (sysAccess["NewAdministratorName"] != null)
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 4,
                        Name = "New Administrator account name",
                        Result = sysAccess["NewAdministratorName"].ToString().Trim('"', '\\')
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 1 && sysAccess["NewGuestName"] != null)
            {
                if (sysAccess["NewGuestName"] != null)
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 1,
                        Name = "New Guest account name",
                        Result = sysAccess["NewGuestName"].ToString().Trim('"', '\\')
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 7 && sysAccess["ClearTextPassword"] != null)
            {
                if (sysAccess["ClearTextPassword"].ToString() != "0")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 7,
                        Name = "Store passwords using reversible encryption",
                        Result = "True"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
                }
            }
            if (this.InterestLevel <= 3 && sysAccess["LSAAnonymousNameLookup"] != null)
            {
                if (sysAccess["LSAAnonymousNameLookup"].ToString() != "0")
                {
                    var a = new AuditedSysAccessElement
                    {
                        Interest = 3,
                        Name = "Allow Anonymous access to local LSA policy",
                        Result = "True"
                    };
                    assessedSysAccess.TryBumpInterest(a);
                    assessedSysAccess.Findings.Add(a);
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