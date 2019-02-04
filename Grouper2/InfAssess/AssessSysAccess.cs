using Newtonsoft.Json.Linq;

namespace Grouper2.InfAssess
{
    internal static partial class AssessInf
    {
        public static JObject AssessSysAccess(JToken sysAccess)
        {

            JObject assessedSysAccess = new JObject();
        
            if ((GlobalVar.IntLevelToShow <= 1) && (sysAccess["MinimumPasswordAge"] != null))
            {
                if (sysAccess["MinimumPasswordAge"].ToString() != "1")
                {
                    assessedSysAccess.Add(new JProperty("Minimum Password Age", sysAccess["MinimumPasswordAge"].ToString() + " days"));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 1) && (sysAccess["MaximumPasswordAge"] != null))
            {
                if (sysAccess["MaximumPasswordAge"].ToString() != "42")
                {
                    assessedSysAccess.Add(new JProperty("Maximum Password Age", sysAccess["MaximumPasswordAge"].ToString() + " days"));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 1) && (sysAccess["MinimumPasswordLength"] != null))
            {
                if (sysAccess["MinimumPasswordLength"].ToString() != "7")
                {
                    assessedSysAccess.Add(new JProperty("Minimum Password Length", sysAccess["MinimumPasswordLength"].ToString() + " characters"));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 1) && (sysAccess["PasswordComplexity"] != null))
            {
                if (sysAccess["PasswordComplexity"].ToString() != "1")
                {
                    assessedSysAccess.Add(new JProperty("Password complexity rules enforced", sysAccess["PasswordComplexity"].ToString()));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 1) && (sysAccess["PasswordHistorySize"] != null))
            {
                if (sysAccess["PasswordHistorySize"].ToString() != "24")
                {
                    assessedSysAccess.Add(new JProperty("Password History Size", sysAccess["PasswordHistorySize"].ToString()));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 3) && (sysAccess["LockoutBadCount"] != null))
            {
                if (sysAccess["LockoutBadCount"].ToString() != "5")
                {
                    assessedSysAccess.Add(new JProperty("Invalid password attempts before lockout", sysAccess["LockoutBadCount"].ToString()));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 3) && (sysAccess["ResetLockoutCount"] != null))
            {
                if (sysAccess["ResetLockoutCount"].ToString() != "30")
                {
                    assessedSysAccess.Add(new JProperty("Invalid attempt counter resets after", sysAccess["ResetLockoutCount"].ToString() + " minutes"));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 3) && (sysAccess["LockoutDuration"] != null))
            {
                if (sysAccess["LockoutDuration"].ToString() != "30")
                {
                    assessedSysAccess.Add(new JProperty("Account unlocks after", sysAccess["LockoutDuration"].ToString() + " minutes"));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 1) && (sysAccess["ForceLogoffWhenHourExpire"] != null))
            {
                if (sysAccess["ForceLogoffWhenHourExpire"].ToString() != "0")
                {
                    assessedSysAccess.Add(new JProperty("Forcibly disconnect sessions outside logon hours", "True"));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 4) && (sysAccess["NewAdministratorName"] != null))
            {
                if (sysAccess["NewAdministratorName"] != null)
                {
                    assessedSysAccess.Add(new JProperty("New Administrator account name", sysAccess["NewAdministratorName"].ToString().Trim('"', '\\')));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 1) && (sysAccess["NewGuestName"] != null))
            {
                if (sysAccess["NewGuestName"] != null)
                {
                    assessedSysAccess.Add(new JProperty("New Guest account name", sysAccess["NewGuestName"].ToString().Trim('"', '\\')));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 7) && (sysAccess["ClearTextPassword"] != null))
            {
                if (sysAccess["ClearTextPassword"].ToString() != "0")
                {
                    assessedSysAccess.Add(new JProperty("Store passwords using reversible encryption", "True"));
                }
            }
            if ((GlobalVar.IntLevelToShow <= 3) && (sysAccess["LSAAnonymousNameLookup"] != null))
            {
                if (sysAccess["LSAAnonymousNameLookup"].ToString() != "0")
                {
                    assessedSysAccess.Add(new JProperty("Allow Anonymous access to local LSA policy", "True"));
                }
            }

            if (!(assessedSysAccess.HasValues))
            {
                assessedSysAccess = null;
            }

            return assessedSysAccess;
        }
    }
}