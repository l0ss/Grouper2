using System.Collections.Generic;
using Grouper2.Host;
using Grouper2.Properties;

namespace Grouper2

{
    public partial class RegKey
    { 
        public string MsDesc { get; set; }
        public object FriendlyDesc { get; set; }
        public int IntLevel { get; set; }
        public string Key { get; set; }
    }
    
    // Create several singletons that contain our big GPO data blob so we can access it without reparsing it.
    public static class JankyDb
    {
        private static Jankydb _instance;

        private static readonly RegKey[] _regkeys;
        public static SingletonVars Vars { get; set; }
        public static bool DebugMode { get; set; } = false;

        public static RegKey[] RegKeys => _regkeys;
        public static Jankydb Db => _instance ?? (_instance = Jankydb.FromJson(Resources.PolData));

        static JankyDb()
        {
            _regkeys = GetKeys();
        }

        private static RegKey[] GetKeys()
        {
            return new[]
            {
                new RegKey()
                {
                    MsDesc = @"User Account Control: Run all administrators in Admin Approval Mode",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Microsoft\\Windows\\CurrentVersion\\Policies\\System\\EnableLUA",
                },
                new RegKey()
                {
                    MsDesc = @"Local Account Token Filter Policy",
                    FriendlyDesc = null,
                    IntLevel = 3,
                    Key = @"Microsoft\\Windows\\CurrentVersion\\Policies\\System\\LocalAccountTokenFilterPolicy",
                },
                new RegKey()
                {
                    MsDesc = @"Include command line in process creation events",
                    FriendlyDesc = null,
                    IntLevel = 3,
                    Key =
                        @"Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit\\ProcessCreationIncludeCmdLine_Enabled",
                },
                new RegKey()
                {
                    MsDesc = @"Turn off downloading of print drivers over HTTP",
                    FriendlyDesc = null,
                    IntLevel = 3,
                    Key = @"Policies\\Microsoft\\Windows NT\\Printers\\DisableWebPnPDownload",
                },
                new RegKey()
                {
                    MsDesc = @"Enable insecure guest logons",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Policies\\Microsoft\\Windows\\LanmanWorkstation\\AllowInsecureGuestAuth",
                },
                new RegKey()
                {
                    MsDesc = @"Turn on PowerShell Script Block Logging",
                    FriendlyDesc = null,
                    IntLevel = 3,
                    Key = @"Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging\\EnableScriptBlockLogging",
                },
                new RegKey()
                {
                    MsDesc = @"Allow Basic authentication (Client)",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Policies\\Microsoft\\Windows\\WinRM\\Client\\AllowBasic",
                },
                new RegKey()
                {
                    MsDesc = @"Disallow Digest authentication",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Policies\\Microsoft\\Windows\\WinRM\\Client\\AllowDigest",
                },
                new RegKey()
                {
                    MsDesc = @"Allow unencrypted traffic (Client)",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Policies\\Microsoft\\Windows\\WinRM\\Client\\AllowUnencryptedTraffic",
                },
                new RegKey()
                {
                    MsDesc = @"Allow Basic authentication (Server)",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Policies\\Microsoft\\Windows\\WinRM\\Service\\AllowBasic",
                },
                new RegKey()
                {
                    MsDesc = @"Allow unencrypted traffic (Server)",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Policies\\Microsoft\\Windows\\WinRM\\Service\\AllowUnencryptedTraffic",
                },
                new RegKey()
                {
                    MsDesc = @"Disallow WinRM from storing RunAs credentials",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"Policies\\Microsoft\\Windows\\WinRM\\Service\\DisableRunAs",
                },
                new RegKey()
                {
                    MsDesc = @"Network access: Let Everyone permissions apply to anonymous users",
                    FriendlyDesc = null,
                    IntLevel = 1,
                    Key = @"CurrentControlSet\\Control\\Lsa\\everyoneincludesanonymous",
                },
                new RegKey()
                {
                    MsDesc = @"Network security: Do not store LAN Manager hash value on next password change",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Control\\Lsa\\NoLmHash",
                },
                new RegKey()
                {
                    MsDesc = @"Network access: Do not allow anonymous enumeration of SAM accounts",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Control\\Lsa\\restrictanonymoussam",
                },
                new RegKey()
                {
                    MsDesc = @"Network access: Restrict clients allowed to make remote calls to SAM",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Control\\Lsa\\RestrictRemoteSAM",
                },
                new RegKey()
                {
                    MsDesc = @"WDigest Authentication Disabled",
                    FriendlyDesc = null,
                    IntLevel = 5,
                    Key = @"CurrentControlSet\\Control\\SecurityProviders\\WDigest\\UseLogonCredentials",
                },
                new RegKey()
                {
                    MsDesc = @"Microsoft network server: Digitally sign communications (always)",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\LanmanServer\\Parameters\\RequireSecuritySignature",
                },
                new RegKey()
                {
                    MsDesc = @"Network access: Restrict anonymous access to Named Pipes and Shares",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\LanmanServer\\Parameters\\RestrictNullSessAccess",
                },
                new RegKey()
                {
                    MsDesc = @"Microsoft network client: Send unencrypted password to third-party SMB servers",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\LanmanWorkstation\\Parameters\\EnablePlainTextPassword",
                },
                new RegKey()
                {
                    MsDesc = @"Microsoft network client: Digitally sign communications (always)",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\LanmanWorkstation\\Parameters\\RequireSecuritySignature",
                },
                new RegKey()
                {
                    MsDesc = @"Network security: LDAP client signing requirements",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\ldap\\ldapclientintegrity",
                },
                new RegKey()
                {
                    MsDesc = @"Domain member: Digitally encrypt or sign secure channel data (always)",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\Netlogon\\Parameters\\RequireSignOrSeal",
                },
                new RegKey()
                {
                    MsDesc = @"WPAD Disabled",
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\WinHttpAutoProxySvc\\Start",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Control\\Lsa\\DisableDomainCreds",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Control\\Lsa\\LimitBlankPasswordUse",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 4,
                    Key = @"CurrentControlSet\\Control\\Lsa\\SubmitControl",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 3,
                    Key = @"CurrentControlSet\\Control\\Lsa\\UseMachineId",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 5,
                    Key =
                        @"CurrentControlSet\\Control\\Print\\Providers\\LanMan Print Services\\Servers\\AddPrinterDrivers",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 5,
                    Key = @"CurrentControlSet\\Control\\SecurePipeServers\\Winreg\\AllowedExactPaths\\Machine",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 5,
                    Key = @"CurrentControlSet\\Control\\SecurePipeServers\\Winreg\\AllowedPaths\\Machine",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 5,
                    Key = @"CurrentControlSet\\Services\\LanManServer\\Parameters\\NullSessionPipes",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 2,
                    Key = @"CurrentControlSet\\Services\\LanManServer\\Parameters\\NullSessionShares",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"Network Associates\\ePolicy Orchestrator",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"FileZilla Server",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"Wow6432Node\\McAfee\\DesktopProtection - McAfee VSE",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"McAfee\\DesktopProtection - McAfee VSE",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"ORL\\WinVNC3",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"ORL\\WinVNC3\\Default",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"ORL\\WinVNC\\Default",
                },
                new RegKey()
                {
                    MsDesc = null,
                    FriendlyDesc = null,
                    IntLevel = 8,
                    Key = @"RealVNC\\WinVNC4",
                },
                new RegKey()
                {
                    IntLevel = 8,
                    Key = @"RealVNC\\Default",
                },
                new RegKey()
                {
                    IntLevel = 8,
                    Key = @"TightVNC\\Server",
                },
                new RegKey()
                {
                    IntLevel = 8,
                    Key = @"Microsoft\\Windows NT\\CurrentVersion\\Winlogon\\DefaultUserName",
                },
                new RegKey()
                {
                    IntLevel = 10,
                    Key = @"Microsoft\\Windows NT\\CurrentVersion\\Winlogon\\DefaultPassword",
                },
                new RegKey()
                {
                    IntLevel = 8,
                    Key = @"Microsoft\\Windows NT\\CurrentVersion\\Winlogon\\AutoAdminLogon",
                },
            };
        }
        
    }

    public class SingletonVars
    {
        public SingletonVars(string sysvolDir, int interest, bool onlineMode, bool noNtfrs, bool noGrepScripts, bool debugMode, string domain)
        {
            this.SysvolDir = sysvolDir;
            this.Interest = interest;
            this.OnlineMode = onlineMode;
            this.NoNtfrs = noNtfrs;
            this.NoGrepScripts = noGrepScripts;
            this.DebugMode = debugMode;
            this.Domain = domain;
        }
        public string SysvolDir { get; private set; }
        public string Domain { get; private set; }
        public int Interest { get; private set; }
        public bool OnlineMode { get; private set; }
        public bool NoNtfrs { get; private set; }
        public bool NoGrepScripts { get; private set; }
        public bool DebugMode { get; private set; }
    }
}
