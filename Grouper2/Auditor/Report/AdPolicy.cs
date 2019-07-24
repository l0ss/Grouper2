using System.Collections.Generic;
using Grouper2.Host.DcConnection;
using Grouper2.Host.DcConnection.Sddl;
using Grouper2.Host.SysVol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AdPolicy
    {
        public AdPolicy()
        {
            this.GpoProperties = new Properties();
            this.GpoFindings = new GpoFindings();
            this.GpoPackages = new List<GpoPackage>();
        }
        [JsonProperty("File Path")] public string Path { get; set; }
        [JsonProperty("GPOProps")] public Properties GpoProperties { get; set; }
        [JsonProperty("Findings")] public GpoFindings GpoFindings { get; set; }
        [JsonProperty("Packages")] public List<GpoPackage> GpoPackages { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Properties
    {
        [JsonProperty("ACLs")] public JObject AclsReport { get; set; }
        [JsonProperty("Display Name")] public string Name { get; set; }
        [JsonProperty("UID")] public string Uid { get; set; }
        [JsonProperty("Distinguished Name")] public string DistinguishedName { get; set; }
        [JsonProperty("Created")] public string Created { get; set; }
        [JsonProperty("GPO Status")] public string EnabledStatus { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AclReportObject
    {
        public AclReportObject()
        {
            this.Acls = new List<ReportAcl>();
        }

        [JsonProperty("Owner")] public string Owner { get; set; }
        [JsonProperty("Group")] public string Group { get; set; }
        [JsonProperty("ACEs")] public List<ReportAcl> Acls { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ReportAcl
    {
        [JsonProperty("SID")] public string Sid { get; set; }
        [JsonProperty("Name")] public string Name { get; set; }
        [JsonProperty("Type")] public string Type { get; set; }
        [JsonProperty("Rights")] public string[] Rights { get; set; }
        [JsonProperty("Flags")] public string[] Flags { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GpoFindings
    {
        public GpoFindings()
        {
            this.MachineFindings = new List<Finding>();
            this.UserFindings = new List<Finding>();
        }
        [JsonProperty("User Policy")] public List<Finding> UserFindings { get; set; }
        [JsonProperty("Machine Policy")] public List<Finding> MachineFindings { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public abstract class Finding
    {
        [JsonProperty("Name")] public string Name { get; set; }
        [JsonIgnore] public string Path { get; set; }
        [JsonIgnore] public string ParentGpoUid { get; set; }
        [JsonIgnore] public SysvolObjectType Classification { get; set; } = SysvolObjectType.Unclassified;

        [JsonProperty("InterestLevel")] public int Interest { get; set; }

        /// <summary>
        /// Sets this finding interest level to that of another finding, if the other finding is a higher interest
        /// </summary>
        /// <param name="auditedSubFinding">finding whose interest might be higher</param>
        public void TryBumpInterest(Finding auditedSubFinding)
        {
            if (auditedSubFinding != null && auditedSubFinding.Interest >= this.Interest)
            {
                 this.Interest = auditedSubFinding.Interest;
            }
        }
        
        /// <summary>
        /// Sets this finding interest level to an arbitrary value, if the provided value is a higher interest
        /// </summary>
        /// <param name="newval">integer to try to set</param>
        public void TryBumpInterest(int newval)
        {
            if (newval >= this.Interest)
            {
                this.Interest = newval;
            }
        }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedDrives : Finding
    {
        [JsonProperty("Assessed Drives")] public List<AuditedDrive> Drives { get; set; } = new List<AuditedDrive>();
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedDrive : Finding
    {
        [JsonProperty("Drive UID")] public string Uid { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Drive Letter")] public string Letter { get; set; }
        [JsonProperty("Label")] public string Label { get; set; }
        [JsonProperty("Username")] public string Username { get; set; }
        [JsonProperty("cPassword")] public string CPass { get; set; }
        [JsonProperty("Decrypted Password")] public string CPassDecrypted { get; set; }
        [JsonProperty("Audited Path")] public AuditedPath AuditedPath { get; set; }
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlGroups : Finding
    {
        [JsonProperty("GPP Groups")] public Dictionary<string, AuditedGppXmlGroupsGroup> Groups { get; set; } = new Dictionary<string, AuditedGppXmlGroupsGroup>();
        [JsonProperty("GPP Users")] public Dictionary<string, AuditedGppXmlGroupsUser> Users { get; set; } = new Dictionary<string, AuditedGppXmlGroupsUser>();
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlGroupsUser : Finding
    {
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Username")] public string Username { get; set; }
        [JsonProperty("cPassword")] public string CPassword { get; set; }
        [JsonProperty("Decrypted Password")] public string CPasswordDecrypted { get; set; }
        [JsonProperty("Account Disabled")] public string AccountDisabled { get; set; }
        [JsonProperty("Password Never Expires")] public string PasswordNeverExpires { get; set; }
        [JsonProperty("Description")] public string Description { get; set; }
        [JsonProperty("Full Name")] public string FullName { get; set; }
        [JsonProperty("New Name")] public string NewName { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlGroupsGroup : Finding
    {
        [JsonProperty("Description")] public string Description { get; set; }
        [JsonProperty("New Name")] public string NewName { get; set; }
        [JsonProperty("Delete All Users")] public string DelUsers { get; set; }
        [JsonProperty("Delete All Groups")] public string DelGroups { get; set; }
        [JsonProperty("Remove Accounts")] public string DelAccounts { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Members")] public List<AuditedGppXmlGroupsGroupMember> Members { get; set; } = new List<AuditedGppXmlGroupsGroupMember>();
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlGroupsGroupMember : Finding
    {
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("SID")] public string Sid { get; set; }
        [JsonProperty("Display Name From SID")] public string DisplayName { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SendEmailAction : Finding
    {
        [JsonProperty("From")] public string From { get; set; }
        [JsonProperty("To")] public string To { get; set; }
        [JsonProperty("Subject")] public string Subject { get; set; }
        [JsonProperty("Body")] public string Body { get; set; }
        [JsonProperty("Header Fields")] public string Headers { get; set; }
        [JsonProperty("Attachment")] public AuditedString Attachment { get; set; }
        [JsonProperty("Server")] public string Server { get; set; }
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlSchedTasks : Finding
    {
        [JsonProperty("Tasks")] public Dictionary<string, AuditedGppXmlSchedTasksTask> Tasks { get; set; } = new Dictionary<string, AuditedGppXmlSchedTasksTask>();
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlSchedTasksCommand : Finding
    {
        [JsonIgnore] public string Caption { get; set; }
        [JsonProperty("Command")] public AuditedPath Command { get; set; }
        [JsonProperty("Args")] public AuditedString Args { get; set; }
        
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlSchedTasksTask : Finding
    {
        [JsonIgnore] public string Uid { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("SID")] public string Sid { get; set; }
        [JsonProperty("Display Name From SID")] public string DisplayName { get; set; }
        [JsonProperty("Quick Workaround")] public JObject WORKAROUND { get; set; }
    }
    
    
    
    
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlPrinters : Finding
    {
        [JsonProperty("Printers")] public Dictionary<string, AuditedGppXmlPrintersPrinter> Printers { get; set; } = new Dictionary<string, AuditedGppXmlPrintersPrinter>();
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlPrintersPrinter : Finding
    {
        [JsonIgnore] public string Uid { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Username")] public string Username { get; set; }
        [JsonProperty("cPassword")] public string CPassword { get; set; }
        [JsonProperty("Decrypted Password")] public string CPasswordDecrypted { get; set; }
        [JsonProperty("Local Name")] public string LocalName { get; set; }
        [JsonProperty("Address")] public string Address { get; set; }
        [JsonProperty("SNMP Community String")] public string SnmpString { get; set; }
        [JsonProperty("Path")] public new AuditedPath Path { get; set; }
        [JsonProperty("Comment")] public AuditedString Comment { get; set; }
        [JsonProperty("Location")] public AuditedString Location { get; set; }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlNtServices : Finding
    {
        [JsonProperty("Services")] public Dictionary<string, AuditedGppXmlNtService> Services { get; set; } = new Dictionary<string, AuditedGppXmlNtService>();
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlNtService : Finding
    {
        [JsonIgnore] public string Uid { get; set; }
        [JsonProperty("Service Name")] public string ServiceName { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        
        [JsonProperty("Username")] public string Username { get; set; }
        [JsonProperty("Timeout")] public string Timeout { get; set; }
        [JsonProperty("Startup Type")] public string StartupType { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Action on first failure")] public string ActionFailure { get; set; }
        [JsonProperty("Action on second failure")] public string ActionFailure2 { get; set; }
        [JsonProperty("Action on third failure")] public string ActionFailure3 { get; set; }
        [JsonProperty("cPassword")] public string CPass { get; set; }
        [JsonProperty("Decrypted Password")] public string CPassDecrypted { get; set; }
        [JsonProperty("Program")] public AuditedPath Program { get; set; }
        [JsonProperty("Args")] public AuditedString Args { get; set; }
    }
    
    
    
    
    
    
    
    
    
    
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedScriptsDotIni : Finding
    {
        public AuditedScriptsDotIni()
        {
            Scripts = new List<AuditedScriptsDotIniType>();
        }

        [JsonProperty("Scripts")] public List<AuditedScriptsDotIniType> Scripts { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedScriptsDotIniType : Finding
    {
        [JsonProperty("Type")] public string Type { get; set; }
        [JsonProperty("Interesting Findings")] public List<AuditedScriptsDotIniScript> Findings { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedScriptsDotIniScript : Finding
    {
        [JsonProperty("Script Number")] public string Num { get; set; }
        [JsonProperty("Command Line")] public AuditedPath Commandline { get; set; }
        [JsonProperty("Parameters")] public AuditedString Params { get; set; }
        
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedIni : Finding
    {
        [JsonProperty("File Path")] public new string Path { get; set; }
        [JsonProperty("Assessed Shortcuts")] public Dictionary<string, AuditedShortcut> Contents { get; set; } = new Dictionary<string, AuditedShortcut>();
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedShortcuts : Finding
    {
        [JsonProperty("File Path")] public new string Path { get; set; }
        [JsonProperty("Assessed Shortcuts")] public Dictionary<string, AuditedShortcut> Contents { get; set; } = new Dictionary<string, AuditedShortcut>();
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedShortcut : Finding
    {
        [JsonProperty("@uid")] public string Uid { get; set; }
        [JsonProperty("Status")] public string Status { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }

        [JsonProperty("Target Type")] public string TargetType { get; set; }
        [JsonProperty("Arguments")] public string Arguments { get; set; }
        [JsonProperty("Icon Path")] public AuditedPath IconPath { get; set; }
        [JsonProperty("Icon Index")] public string IconIndex { get; set; }
        [JsonProperty("Working Directory")] public AuditedPath WorkingDir { get; set; }
        [JsonProperty("Comment")] public string Comment { get; set; }
        [JsonProperty("Shortcut Path")] public AuditedPath ShortcutPath { get; set; }
        [JsonProperty("Targert Path")] public AuditedPath TargetPath { get; set; }

    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedDataSource : Finding
    {
        public AuditedDataSource()
        {
            Contents = new List<AuditedDataSourceEntry>();
        }

        [JsonProperty("File Path")] public new string Path { get; set; }
        [JsonProperty("Assessed DataSources")] public List<AuditedDataSourceEntry> Contents { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedDataSourceEntry : Finding
    {
        [JsonProperty("uid")] public string Uid { get; set; }
        // NAME property is inherited from Finding
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Username")] public string Username { get; set; }
        [JsonProperty("cPassword")] public string CPassword { get; set; }
        [JsonProperty("Decrypted Password")] public string CPasswordDecrypted { get; set; }
        [JsonProperty("DSN")] public string Dsn { get; set; }
        [JsonProperty("Driver")] public string Driver { get; set; }
        [JsonProperty("Description")] public string Description { get; set; }
        [JsonProperty("Attributes")] public JToken Attributes { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGptmpl : Finding
    {
        [JsonProperty("Privilege Rights")] public List<AuditedPrivRight> AuditedPrivRight { get; set; } = new List<AuditedPrivRight>();
        [JsonProperty("Registry Values")] public List<AuditedRegistryValues> RegistryValues { get; set; } = new List<AuditedRegistryValues>();
        [JsonProperty("System Access")] public AuditedSystemAccess SystemAccess { get; set; }
        [JsonProperty("Kerberos Policy")] public AuditedKerbPolicy KerbPolicy { get; set; }
        [JsonProperty("Registry Keys")] public Dictionary<string, AuditedRegistryKeys> RegistryKeys { get; set; } = new Dictionary<string, AuditedRegistryKeys>();
        [JsonProperty("Group Membership")] public JObject GroupMemberships { get; set; }
        [JsonProperty("Service General Setting")] public Dictionary<string, AuditedServiceGenSetting> ServiceGenSettings { get; set; } = new Dictionary<string, AuditedServiceGenSetting>();

    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedRegistryValues : Finding
    {
        [JsonProperty("Values")] public List<string> KeyValues { get; set; }

    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedSystemAccess : Finding
    {
        public AuditedSystemAccess()
        {
            this.Findings = new List<AuditedSysAccessElement>();
        }
        [JsonProperty("Findings")] public List<AuditedSysAccessElement> Findings { get; set; }
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]

    public class AuditedSysAccessElement : Finding
    {
        [JsonProperty("Value")] public string Result { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedKerbPolicy : Finding
    {
        [JsonProperty("Maximum Ticket Age")] public string TicketAge { get; set; }
        [JsonProperty("Maximum lifetime for user ticket renewal")] public string MaxRenewAge { get; set; }
        [JsonProperty("Maximum lifetime for service ticket")] public string MaxServiceAge { get; set; }
        [JsonProperty("Maximum clock skew")] public string MaxClockSkew { get; set; }
        [JsonProperty("Enforce user logon restrictions")] public string TicketValidateClient { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedRegistryKeys : Finding
    {
        [JsonProperty("RegKey")] public string KeyPath { get; set; }
        [JsonProperty("Inheritance")] public string Inheritance { get; set; }
        [JsonProperty("Owner")] public SddlSid Owner { get; set; }
        [JsonProperty("Group")] public SddlSid Group { get; set; }
        [JsonProperty("Key ACLs")] public List<Ace> Aces { get; set; } = new List<Ace>();

    }



    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGroupMembership : Finding
    {
        [JsonIgnore] public string Key { get; set; }
        [JsonProperty("Sid")] public string Sid { get; set; }
        [JsonProperty("Members")] public AuditedGroupMember AuditedString { get; set; } = new AuditedGroupMember();
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGroupMember : Finding
    {
        [JsonProperty("Sid")] public string Sid { get; set; }
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlNetworkOptions : Finding
    {
        [JsonProperty("DUN")] public JObject Dun { get; set; }
    }
    
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlRegSettings : Finding
    {
        public AuditedGppXmlRegSettings()
        {
            this.RegSetting = new Dictionary<string, AuditedGppXmlRegSetting>();
            this.RegCollections = new Dictionary<string, AuditedGppXmlRegCollection>();
        }
        [JsonProperty("Registry Setting Collections")] public Dictionary<string, AuditedGppXmlRegCollection> RegCollections { get; set; }
        [JsonProperty("Registry Settings")] public Dictionary<string, AuditedGppXmlRegSetting> RegSetting { get; set; }
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlRegCollection : Finding
    {
        public AuditedGppXmlRegCollection()
        {
            Settings = new Dictionary<string, AuditedGppXmlRegSetting>();
        }

        [JsonProperty("Description")] public string Description { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Registry Settings in Collection")] public Dictionary<string, AuditedGppXmlRegSetting> Settings { get; set; }

    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlRegSetting : Finding
    {
        [JsonProperty("Display Name")] public string DisplayName { get; set; }
        [JsonProperty("Status")] public string Status { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Default")] public string Default { get; set; }
        [JsonProperty("Hive")] public string Hive { get; set; }
        [JsonProperty("Key")] public AuditedString Key { get; set; }
        [JsonProperty("Name")] public new AuditedString Name { get; set; }
        [JsonProperty("Type")] public string Type { get; set; }
        [JsonProperty("Value")] public AuditedString Value { get; set; }
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlNetShares : Finding
    {
        public AuditedGppXmlNetShares()
        {
            this.Shares = new Dictionary<string, AuditedGppXmlNetShare>();
        }
        [JsonProperty("Shares")] public Dictionary<string, AuditedGppXmlNetShare> Shares { get; set; }
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlNetShare : Finding
    {
        [JsonIgnore] public string Uid { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Comment")] public string Comment { get; set; }
        
    }
    

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlIniFiles : Finding
    {
        public AuditedGppXmlIniFiles()
        {
            Inis = new Dictionary<string, AuditedGppXmlIniFilesFile>();
        }

        [JsonProperty("INIs")] public Dictionary<string, AuditedGppXmlIniFilesFile> Inis { get; set; }
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlIniFilesFile : Finding
    {
        [JsonIgnore] public string Uid { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Path Info")] public AuditedPath PathInfo { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Status")] public string Status { get; set; }
        [JsonProperty("Section")] public AuditedString Section { get; set; }
        [JsonProperty("Value")] public AuditedString Value { get; set; }
        [JsonProperty("Property")] public AuditedString Property { get; set; }
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlFiles : Finding
    {
        [JsonProperty("Files")] public Dictionary<string, AuditedGppXmlFilesFile> Files { get; set; } = new Dictionary<string, AuditedGppXmlFilesFile>();
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlFilesFile : Finding
    {
        [JsonProperty("Status")] public string Status { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
        [JsonProperty("Target Path")] public string TargetPath { get; set; }
        [JsonProperty("From Path")] public AuditedPath FromPath { get; set; }
    }


    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedServiceGenSetting : Finding
    {
        [JsonProperty("Service", Order = -2)] public string Service { get; set; }
        [JsonProperty("Owner")] public SddlSid Owner { get; set; }
        [JsonProperty("Group")] public SddlSid Group { get; set; }
        [JsonProperty("Startup Type")] public string Startup { get; set; }
        [JsonProperty("DACL")] public List<Ace> Aces { get; set; } = new List<Ace>();
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedDotPolFile : Finding
    {
        // TODO: AuditedDotPolFile needs stuff in it!
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlEnvVars : Finding
    {
        /// <inheritdoc />
        public AuditedGppXmlEnvVars()
        {
            Vars = new Dictionary<string, AuditedGppXmlEnvVarsVar>();
        }

        /// <summary>
        /// Collection for fiindings
        /// </summary>
        [JsonProperty("Findings")] public Dictionary<string, AuditedGppXmlEnvVarsVar> Vars { get; set; }
    }
    
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedGppXmlEnvVarsVar : Finding
    {
        [JsonIgnore] public string Uid { get; set; }
        
        [JsonProperty("Status")] public string Status { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Action")] public string Action { get; set; }
    }
    
    


    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedPrivRight : Finding
    {

        [JsonProperty("Description")] public string Description { get; set; }
        [JsonProperty("Trustees")] public List<TrusteeKvp> Trustees { get; set; } = new List<TrusteeKvp>();
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedFileContents : Finding
    {
        [JsonProperty("Contents")] public AuditedString AuditedString { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedScript : Finding
    {
        [JsonProperty("File Path")] public new string Path { get; set; }
        // TODO: figure out the json naming
        public AuditedFileContents Contents { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedString : Finding
    {
        public AuditedString()
        {
            this.InterestingPaths = new List<AuditedPath>();
            this.InterestingWords = new List<string>();
        }
        [JsonProperty("Value")] public string Value { get; set; }
        [JsonProperty("Interesting Paths")] public List<AuditedPath> InterestingPaths { get; set; }
        [JsonProperty("Interesting Words")] public List<string> InterestingWords { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AuditedPath : Finding
    {
        [JsonProperty("Not A Path")] public string NotAPath { get; set; }
        [JsonProperty("HTTP/S URL?")] public string NotAPathHttps { get; set; }
        [JsonProperty("URI?")] public string NotAPathUri { get; set; }
        [JsonProperty("Env var found in path")] public string NotAPathEnv { get; set; }
        [JsonProperty("Local Drive?")] public string NotAPathDrive { get; set; }
        [JsonProperty("No path separators, file in SYSVOL?")] public string NoSep { get; set; }
        [JsonProperty("Path assessed")] public string PathAssessed { get; set; }
        [JsonProperty("File Info")] public AuditedPathFile FileData { get; set; }
        [JsonProperty("Directory Info")] public AuditedPathDir DirData { get; set; }
    }

    public class AuditedPathFile : Finding
    {
        [JsonProperty("File exists")] public bool FileExists { get; set; }
        [JsonProperty("File extension interesting")] public bool ExtIsInteresting { get; set; }
        [JsonProperty("File readable")] public bool Readable { get; set; }
        [JsonProperty("File contents interesting")] public bool ContentsInteresting { get; set; }
        [JsonProperty("Interesting strings found")] public List<string> ContentsStringsOfInterest { get; set; } = new List<string>();
        [JsonProperty("File writable")] public bool Writable { get; set; }
        [JsonProperty("File DACLs")] public JObject FileDacls { get; set; }
    }
    
    public class AuditedPathDir : Finding
    {
        [JsonProperty("Extant parent dir")] public string ExtantParentDir { get; set; }
        [JsonProperty("Dir exists")] public bool DirExists { get; set; }
        [JsonProperty("Directory is writable")] public bool Writable { get; set; }
        [JsonProperty("Parent Dir")] public AuditedPathDir Parent { get; set; }
        [JsonProperty("Directory DACL")] public JObject Dacls { get; set; }

    }
}