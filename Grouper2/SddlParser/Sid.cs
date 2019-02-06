using System.Collections.Generic;

namespace Grouper2.SddlParser
{
    public class Sid
    {
        public string Raw { get; }

        public string Alias { get; }
        
        public Sid(string sid)
        {
            Raw = sid;

            string alias =
                Match.OneByRegex(sid, KnownSidsDict) ??
                Match.OneByPrefix(sid, KnownAliasesDict, out _);

            if (alias == null)
            {
                // ERROR Unknown SID.
                if (GlobalVar.OnlineChecks)
                {
                    alias = LDAPstuff.GetUserFromSid(sid);
                }
                else
                {
                    alias = Format.Unknown(sid);
                }
            }
            
            Alias = alias;
        }

        internal static readonly Dictionary<string, string> KnownAliasesDict = new Dictionary<string, string>
        {
            { "DA", "DOMAIN_ADMINISTRATORS" },
            { "DG", "DOMAIN_GUESTS" },
            { "DU", "DOMAIN_USERS" },
            { "ED", "ENTERPRISE_DOMAIN_CONTROLLERS" },
            { "DD", "DOMAIN_DOMAIN_CONTROLLERS" },
            { "DC", "DOMAIN_COMPUTERS" },
            { "BA", "BUILTIN_ADMINISTRATORS" },
            { "BG", "BUILTIN_GUESTS" },
            { "BU", "BUILTIN_USERS" },
            { "LA", "LOCAL_ADMIN" },
            { "LG", "LOCAL_GUEST" },
            { "AO", "ACCOUNT_OPERATORS" },
            { "BO", "BACKUP_OPERATORS" },
            { "PO", "PRINTER_OPERATORS" },
            { "SO", "SERVER_OPERATORS" },
            { "AU", "AUTHENTICATED_USERS" },
            { "PS", "PERSONAL_SELF" },
            { "CO", "CREATOR_OWNER" },
            { "CG", "CREATOR_GROUP" },
            { "SY", "LOCAL_SYSTEM" },
            { "PU", "POWER_USERS" },
            { "WD", "EVERYONE" },
            { "RE", "REPLICATOR" },
            { "IU", "INTERACTIVE" },
            { "NU", "NETWORK" },
            { "SU", "SERVICE" },
            { "RC", "RESTRICTED_CODE" },
            { "WR", "WRITE_RESTRICTED_CODE" },
            { "AN", "ANONYMOUS" },
            { "SA", "SCHEMA_ADMINISTRATORS" },
            { "CA", "CERT_SERV_ADMINISTRATORS" },
            { "RS", "RAS_SERVERS" },
            { "EA", "ENTERPRISE_ADMINS" },
            { "PA", "GROUP_POLICY_ADMINS" },
            { "RU", "ALIAS_PREW2KCOMPACC" },
            { "LS", "LOCAL_SERVICE" },
            { "NS", "NETWORK_SERVICE" },
            { "RD", "REMOTE_DESKTOP" },
            { "NO", "NETWORK_CONFIGURATION_OPS" },
            { "MU", "PERFMON_USERS" },
            { "LU", "PERFLOG_USERS" },
            { "IS", "IIS_USERS" },
            { "CY", "CRYPTO_OPERATORS" },
            { "OW", "OWNER_RIGHTS" },
            { "ER", "EVENT_LOG_READERS" },
            { "RO", "ENTERPRISE_RO_DCs" },
            { "CD", "CERTSVC_DCOM_ACCESS" },
            { "AC", "ALL_APP_PACKAGES" },
            { "RA", "RDS_REMOTE_ACCESS_SERVERS" },
            { "ES", "RDS_ENDPOINT_SERVERS" },
            { "MS", "RDS_MANAGEMENT_SERVERS" },
            { "UD", "USER_MODE_DRIVERS" },
            { "HA", "HYPER_V_ADMINS" },
            { "CN", "CLONEABLE_CONTROLLERS" },
            { "AA", "ACCESS_CONTROL_ASSISTANCE_OPS" },
            { "RM", "REMOTE_MANAGEMENT_USERS" },
            { "AS", "AUTHORITY_ASSERTED" },
            { "SS", "SERVICE_ASSERTED" },
            { "AP", "PROTECTED_USERS" },
            { "KA", "KEY_ADMINS" },
            { "EK", "ENTERPRISE_KEY_ADMINS" },
        };

        /// <summary>
        /// A dictionary of well known SIDs in format S-* as defined in https://support.microsoft.com/en-us/help/243330/well-known-security-identifiers-in-windows-operating-systems.
        /// </summaty>
        internal static readonly Dictionary<string, string> KnownSidsDict = new Dictionary<string, string>
        {
            { "S-1-0", @"Null Authority" },
            { "S-1-0-0", @"Nobody" },
            { "S-1-1", @"World Authority" },
            { "S-1-1-0", @"Everyone" },
            { "S-1-2", @"Local Authority" },
            { "S-1-2-0", @"Local" },
            { "S-1-2-1", @"Console Logon" },
            { "S-1-3", @"Creator Authority" },
            { "S-1-3-0", @"Creator Owner" },
            { "S-1-3-1", @"Creator Group" },
            { "S-1-3-2", @"Creator Owner Server" },
            { "S-1-3-3", @"Creator Group Server" },
            { "S-1-3-4", @"Owner Rights" },
            { "S-1-4", @"Non-unique Authority" },
            { "S-1-5", @"NT Authority" },
            { "S-1-5-1", @"Dialup" },
            { "S-1-5-2", @"Network" },
            { "S-1-5-3", @"Batch" },
            { "S-1-5-4", @"Interactive" },
            { "S-1-5-5-(.+)-(.+)", @"Logon Session" },
            { "S-1-5-6", @"Service" },
            { "S-1-5-7", @"Anonymous" },
            { "S-1-5-8", @"Proxy" },
            { "S-1-5-9", @"Enterprise Domain Controllers" },
            { "S-1-5-10", @"Principal Self" },
            { "S-1-5-11", @"Authenticated Users" },
            { "S-1-5-12", @"Restricted Code" },
            { "S-1-5-13", @"Terminal Server Users" },
            { "S-1-5-14", @"Remote Interactive Logon" },
            { "S-1-5-15", @"This Organization" },
            { "S-1-5-17", @"This Organization" },
            { "S-1-5-18", @"Local System" },
            { "S-1-5-19", @"NT Authority" },
            { "S-1-5-20", @"NT Authority" },
            { "S-1-5-21(.*)-500", @"Administrator" },
            { "S-1-5-21(.*)-501", @"Guest" },
            { "S-1-5-21(.*)-502", @"KRBTGT" },
            { "S-1-5-21(.*)-512", @"Domain Admins" },
            { "S-1-5-21(.*)-513", @"Domain Users" },
            { "S-1-5-21(.*)-514", @"Domain Guests" },
            { "S-1-5-21(.*)-515", @"Domain Computers" },
            { "S-1-5-21(.*)-516", @"Domain Controllers" },
            { "S-1-5-21(.*)-517", @"Cert Publishers" },
            { "S-1-5-21(.*)-518", @"Schema Admins" },
            { "S-1-5-21(.*)-519", @"Enterprise Admins" },
            { "S-1-5-21(.*)-520", @"Group Policy Creator Owners" },
            { "S-1-5-21(.*)-553", @"RAS and IAS Servers" },
            { "S-1-5-32-544", @"Administrators" },
            { "S-1-5-32-545", @"Users" },
            { "S-1-5-32-546", @"Guests" },
            { "S-1-5-32-547", @"Power Users" },
            { "S-1-5-32-548", @"Account Operators" },
            { "S-1-5-32-549", @"Server Operators" },
            { "S-1-5-32-550", @"Print Operators" },
            { "S-1-5-32-551", @"Backup Operators" },
            { "S-1-5-32-552", @"Replicators" },
            { "S-1-5-64-10", @"NTLM Authentication" },
            { "S-1-5-64-14", @"SChannel Authentication" },
            { "S-1-5-64-21", @"Digest Authentication" },
            { "S-1-5-80", @"NT Service" },
            { "S-1-5-80-0", @"All Services" },
            { "S-1-5-83-0", @"NT VIRTUAL MACHINE\Virtual Machines" },
            { "S-1-16-0", @"Untrusted Mandatory Level" },
            { "S-1-16-4096", @"Low Mandatory Level" },
            { "S-1-16-8192", @"Medium Mandatory Level" },
            { "S-1-16-8448", @"Medium Plus Mandatory Level" },
            { "S-1-16-12288", @"High Mandatory Level" },
            { "S-1-16-16384", @"System Mandatory Level" },
            { "S-1-16-20480", @"Protected Process Mandatory Level" },
            { "S-1-16-28672", @"Secure Process Mandatory Level" },
            { "S-1-5-32-554", @"BUILTIN\Pre-Windows 2000 Compatible Access" },
            { "S-1-5-32-555", @"BUILTIN\Remote Desktop Users" },
            { "S-1-5-32-556", @"BUILTIN\Network Configuration Operators" },
            { "S-1-5-32-557", @"BUILTIN\Incoming Forest Trust Builders" },
            { "S-1-5-32-558", @"BUILTIN\Performance Monitor Users" },
            { "S-1-5-32-559", @"BUILTIN\Performance Log Users" },
            { "S-1-5-32-560", @"BUILTIN\Windows Authorization Access Group" },
            { "S-1-5-32-561", @"BUILTIN\Terminal Server License Servers" },
            { "S-1-5-32-562", @"BUILTIN\Distributed COM Users" },
            { "S-1-5-21(.*)-498", @"Enterprise Read-only Domain Controllers" },
            { "S-1-5-21(.*)-521", @"Read-only Domain Controllers" },
            { "S-1-5-32-569", @"BUILTIN\Cryptographic Operators" },
            { "S-1-5-21(.*)-571", @"Allowed RODC Password Replication Group" },
            { "S-1-5-21(.*)-572", @"Denied RODC Password Replication Group" },
            { "S-1-5-32-573", @"BUILTIN\Event Log Readers" },
            { "S-1-5-32-574", @"BUILTIN\Certificate Service DCOM Access" },
            { "S-1-5-21(.*)-522", @"Cloneable Domain Controllers" },
            { "S-1-5-32-575", @"BUILTIN\RDS Remote Access Servers" },
            { "S-1-5-32-576", @"BUILTIN\RDS Endpoint Servers" },
            { "S-1-5-32-577", @"BUILTIN\RDS Management Servers" },
            { "S-1-5-32-578", @"BUILTIN\Hyper-V Administrators" },
            { "S-1-5-32-579", @"BUILTIN\Access Control Assistance Operators" },
            { "S-1-5-32-580", @"BUILTIN\Remote Management Users" },
        };

        public override string ToString()
        {
            return Alias;
        }
    }
}