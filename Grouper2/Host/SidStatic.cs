using System.Collections.Concurrent;
using System.Linq;

namespace Grouper2.Host
{
    public partial class Sid
    {
        private static ConcurrentBag<Sid> _wellKnownSids;

        // ensure we only collect the list once due to overhead in string parsing
        internal static ConcurrentBag<Sid> WellKnownSids => _wellKnownSids ?? (_wellKnownSids = GetAllWellKnownSids());

        /// <summary>
        ///     Determine whether a given SID string is in the form of a SID issued by a domain
        /// </summary>
        /// <param name="sid">The SID to test</param>
        /// <returns>true if is in a domain form, false otherwise</returns>
        internal static bool IsDomainSid(string sid)
        {
            return sid.StartsWith("S-1-5-21");
        }

        public static string GetWellKnownSidAlias(string sid)
        {
            if (!IsShoddilyValidatedSid(sid))
                return null;
            
            bool isDomainSid = IsDomainSid(sid);


            foreach (Sid trustee in WellKnownSids)
            {
                if (isDomainSid)
                {
                    string splitSid = sid.Split('-').Last();
                    if (string.Equals(splitSid, trustee.RelativeId))
                    {
                        return trustee.Name;
                    }
                }

                if (trustee.ComparisonString == sid)
                {
                    return trustee.Name;
                }
            }

            // none found!
            return null;
        }

        public static bool IsShoddilyValidatedSid(string sid)
        {
            // all Sids should start with an "S-1-"
            if (!sid.StartsWith("S-1-"))
                return false;
            
            // other than the leading 'S' and dashes, all sids are only numeric
            string hopefullyNumeric = sid.Replace("S", "").Replace("-", "");
            int whateverDudeIDontWantThisValue = 0;
            foreach (char c in hopefullyNumeric)
            {
                if (!int.TryParse(c.ToString(), out whateverDudeIDontWantThisValue))
                {
                    return false;
                }
            }

            // must be okay?
            return true;
        }

        public static Sid CheckSid(string sid)
        {
            if (!IsShoddilyValidatedSid(sid))
                return null;

            try
            {
                return WellKnownSids.First(
                    s => string.Equals(s.Raw, sid) || // this is an exact match
                         // OR
                         s.DomainSid // the known sid is a domain sid
                         && sid.Length >= 14 // AND the length of the string seems pretty long
                         && // AND the last element of the string matches the relativeID of the known SID
                         string.Equals(sid.Split('-').Last(), s.RelativeId));
            }
            // the errors propagated by the linq above don't matter much, only the fact nothing was found
            catch
            {
                return null;
            }
        }

        // method to build out the well known list
        private static ConcurrentBag<Sid> GetAllWellKnownSids()
        {
            return new ConcurrentBag<Sid>
            {
                new Sid("S-1-0", "Null Authority", "", ""),
                new Sid("S-1-0-0", "Nobody", "", ""),
                new Sid("S-1-1", "World Authority", "", ""),
                new Sid("S-1-1-0", "Everyone", "", "Low"),
                new Sid("S-1-2", "Local Authority", "", ""),
                new Sid("S-1-2-0", "Local", "", ""),
                new Sid("S-1-2-1", "Console Logon", "", "Low"),
                new Sid("S-1-3", "Creator Authority", "", ""),
                new Sid("S-1-3-0", "Creator Owner", "", "High"),
                new Sid("S-1-3-1", "Creator Group", "", ""),
                new Sid("S-1-3-2", "Creator Owner Server", "", ""),
                new Sid("S-1-3-3", "Creator Group Server", "", ""),
                new Sid("S-1-3-4", "Owner Rights", "", ""),
                new Sid("S-1-4", "Non-unique Authority", "", ""),
                new Sid("S-1-5", "NT Authority", "", ""),
                new Sid("S-1-5-1", "Dialup", "", ""),
                new Sid("S-1-5-2", "Network", "", "Low"),
                new Sid("S-1-5-3", "Batch", "", "Low"),
                new Sid("S-1-5-4", "Interactive", "", "Low"),
                new Sid("S-1-5-6", "Service", "", ""),
                new Sid("S-1-5-7", "Anonymous", "", "Low"),
                new Sid("S-1-5-8", "Proxy", "", ""),
                new Sid("S-1-5-9", "Enterprise Domain Controllers", "", "High"),
                new Sid("S-1-5-10", "Principal Self", "", ""),
                new Sid("S-1-5-11", "Authenticated Users", "", "Low"),
                new Sid("S-1-5-12", "Restricted Code", "", ""),
                new Sid("S-1-5-13", "Terminal Server Users", "", "Low"),
                new Sid("S-1-5-14", "Remote Interactive Logon", "", "Low"),
                new Sid("S-1-5-15", "This Organization", "", ""),
                new Sid("S-1-5-17", "This Organization", "", ""),
                new Sid("S-1-5-18", "Local System", "", "High"),
                new Sid("S-1-5-19", "NT Authority\\Local Service", "", ""),
                new Sid("S-1-5-20", "NT Authority\\Network Service", "", ""),
                new Sid("S-1-5-21-<DOMAIN>-498", "Enterprise Read-only Domain Controllers", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-500", "Administrator", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-501", "Guest", "", "Low"),
                new Sid("S-1-5-21-<DOMAIN>-502", "KRBTGT", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-512", "Domain Admins", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-513", "Domain Users", "", "Low"),
                new Sid("S-1-5-21-<DOMAIN>-514", "Domain Guests", "", "Low"),
                new Sid("S-1-5-21-<DOMAIN>-515", "Domain Computers", "", "Low"),
                new Sid("S-1-5-21-<DOMAIN>-516", "Domain Controllers", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-517", "Cert Publishers", "", ""),
                new Sid("S-1-5-21-<DOMAIN>-518", "Schema Admins", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-519", "Enterprise Admins", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-520", "Group Policy Creator Owners", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-522", "Cloneable Domain Controllers", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-526", "Key Admins", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-527", "Enterprise Key Admins", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-553", "RAS and IAS Servers", "", ""),
                new Sid("S-1-5-21-<DOMAIN>-521", "Read-only Domain Controllers", "", "High"),
                new Sid("S-1-5-21-<DOMAIN>-571", "Allowed RODC Password Replication Group", "", ""),
                new Sid("S-1-5-21-<DOMAIN>-572", "Denied RODC Password Replication Group", "", ""),
                new Sid("S-1-5-32-544", "Administrators", "", "High"),
                new Sid("S-1-5-32-545", "Users", "", "Low"),
                new Sid("S-1-5-32-546", "Guests", "", "Low"),
                new Sid("S-1-5-32-547", "Power Users", "", ""),
                new Sid("S-1-5-32-548", "Account Operators", "", ""),
                new Sid("S-1-5-32-549", "Server Operators", "", ""),
                new Sid("S-1-5-32-550", "Print Operators", "", ""),
                new Sid("S-1-5-32-551", "Backup Operators", "", ""),
                new Sid("S-1-5-32-552", "Replicators", "", ""),
                new Sid("S-1-5-64-10", "NTLM Authentication", "", ""),
                new Sid("S-1-5-64-14", "SChannel Authentication", "", ""),
                new Sid("S-1-5-64-21", "Digest Authentication", "", ""),
                new Sid("S-1-5-80", "NT Service", "", ""),
                new Sid("S-1-5-80-0", "All Services", "", ""),
                new Sid("S-1-5-83-0", "NT VIRTUAL MACHINE\\Virtual Machines", "", ""),
                new Sid("S-1-16-0", "Untrusted Mandatory Level", "", ""),
                new Sid("S-1-16-4096", "Low Mandatory Level", "", ""),
                new Sid("S-1-16-8192", "Medium Mandatory Level", "", ""),
                new Sid("S-1-16-8448", "Medium Plus Mandatory Level", "", ""),
                new Sid("S-1-16-12288", "High Mandatory Level", "", ""),
                new Sid("S-1-16-16384", "System Mandatory Level", "", ""),
                new Sid("S-1-16-20480", "Protected Process Mandatory Level", "", ""),
                new Sid("S-1-16-28672", "Secure Process Mandatory Level", "", ""),
                new Sid("S-1-5-32-554", "BUILTIN\\Pre-Windows 2000 Compatible Access", "", "Low"),
                new Sid("S-1-5-32-555", "BUILTIN\\Remote Desktop Users", "", ""),
                new Sid("S-1-5-32-556", "BUILTIN\\Network Configuration Operators", "", ""),
                new Sid("S-1-5-32-557", "BUILTIN\\Incoming Forest Trust Builders", "", ""),
                new Sid("S-1-5-32-558", "BUILTIN\\Performance Monitor Users", "", ""),
                new Sid("S-1-5-32-559", "BUILTIN\\Performance Log Users", "", ""),
                new Sid("S-1-5-32-560", "BUILTIN\\Windows Authorization Access Group", "", ""),
                new Sid("S-1-5-32-561", "BUILTIN\\Terminal Server License Servers", "", ""),
                new Sid("S-1-5-32-562", "BUILTIN\\Distributed COM Users", "", ""),
                new Sid("S-1-5-32-573", "BUILTIN\\Event Log Readers", "", ""),
                new Sid("S-1-5-32-574", "BUILTIN\\Certificate Service DCOM Access", "", ""),
                new Sid("S-1-5-32-569", "BUILTIN\\Cryptographic Operators", "", ""),
                new Sid("S-1-5-32-575", "BUILTIN\\RDS Remote Access Servers", "", ""),
                new Sid("S-1-5-32-576", "BUILTIN\\RDS Endpoint Servers", "", ""),
                new Sid("S-1-5-32-577", "BUILTIN\\RDS Management Servers", "", ""),
                new Sid("S-1-5-32-578", "BUILTIN\\Hyper-V Administrators", "", "High"),
                new Sid("S-1-5-32-579", "BUILTIN\\Access Control Assistance Operators", "", ""),
                new Sid("S-1-5-32-580", "BUILTIN\\Remote Management Users", "", "")
            };
        }
    }
}