using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Grouper2.SddlParser;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class LDAPstuff
    {

        const int NO_ERROR = 0;
        const int ERROR_INSUFFICIENT_BUFFER = 122;

        enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool LookupAccountSid(
            string lpSystemName,
            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder referencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        public static string GetUserFromSid(string sidString)
        {
            // stolen wholesale from http://www.pinvoke.net/default.aspx/advapi32.LookupAccountSid

            StringBuilder name = new StringBuilder();
            uint cchName = (uint) name.Capacity;
            StringBuilder referencedDomainName = new StringBuilder();
            uint cchReferencedDomainName = (uint) referencedDomainName.Capacity;
            SID_NAME_USE sidUse;
            int err = 0;
            try
            {
                SecurityIdentifier sidObj = new SecurityIdentifier(sidString);
                byte[] sidBytes = new byte[sidObj.BinaryLength];
                sidObj.GetBinaryForm(sidBytes, 0);
                err = NO_ERROR;

                if (!LookupAccountSid(null, sidBytes, name, ref cchName, referencedDomainName,
                    ref cchReferencedDomainName,
                    out sidUse))
                {
                    err = Marshal.GetLastWin32Error();
                    if (err == ERROR_INSUFFICIENT_BUFFER)
                    {
                        name.EnsureCapacity((int) cchName);
                        referencedDomainName.EnsureCapacity((int) cchReferencedDomainName);
                        err = NO_ERROR;
                        if (!LookupAccountSid(null, sidBytes, name, ref cchName, referencedDomainName,
                            ref cchReferencedDomainName, out sidUse))
                            err = Marshal.GetLastWin32Error();
                    }
                }
            }
            catch (System.ArgumentException)
            {
                return "SID Lookup Failed";
            }

            string lookupResult = "";
            if (err != 0)
            {
                Utility.Output.DebugWrite(@"Error in SID Lookup : " + err + " resolving SID " + sidString + " handing off to well known sids list.");

                try
                {
                    lookupResult = Utility.Sid.GetWellKnownSid(sidString);
                }
                catch (Exception e)
                {
                    lookupResult = "SID Lookup Failed";
                    Utility.Output.DebugWrite(e.ToString());
                }

                return lookupResult;
            }

            if (referencedDomainName.ToString().Length > 0)
            {
                lookupResult = referencedDomainName.ToString() + "\\" + name.ToString();
            }
            else
            {
                lookupResult = name.ToString();
            } 

            return lookupResult;
        }

        public static JObject GetDomainGpos()
        {
            try
            {
                DirectoryEntry rootDse = new DirectoryEntry();
                DirectoryEntry rootDefNamingContext = new DirectoryEntry();
                DirectoryEntry rootExtRightsContext = new DirectoryEntry();
                
                rootDse = new DirectoryEntry("LDAP://rootDSE");
                rootDefNamingContext = new DirectoryEntry("GC://" + rootDse.Properties["defaultNamingContext"].Value);
                string schemaContextString = rootDse.Properties["schemaNamingContext"].Value.ToString();
                rootExtRightsContext =
                        new DirectoryEntry("LDAP://" + schemaContextString.Replace("Schema", "Extended-Rights"));
                
                // make a searcher to find GPOs
                DirectorySearcher gpoSearcher = new DirectorySearcher(rootDefNamingContext)
                {
                    Filter = "(objectClass=groupPolicyContainer)",
                    SecurityMasks = SecurityMasks.Dacl | SecurityMasks.Owner,
                    PageSize = 1000
                };
                SearchResultCollection gpoSearchResults = gpoSearcher.FindAll();

                


                // new dictionary for data from each GPO to go into
                JObject gposData = new JObject();

                foreach (SearchResult gpoSearchResult in gpoSearchResults)
                {
                    // object for all data for this one gpo
                    JObject gpoData = new JObject();
                    DirectoryEntry gpoDe = gpoSearchResult.GetDirectoryEntry();

                    // get some useful attributes of the gpo
                    string gpoDispName = gpoDe.Properties["displayName"].Value.ToString();
                    gpoData.Add("Display Name", gpoDispName);
                    string gpoUid = gpoDe.Properties["name"].Value.ToString();
                    // this is to catch duplicate UIDs caused by Default Domain Policy and Domain Controller Policy having 'well known guids'
                    if (gposData[gpoUid] != null)
                    {
                        Utility.Output.DebugWrite("\nI think you're in a multi-domain environment cos I just saw two GPOs with the same GUID. " +
                                           "\nYou should be careful not to miss stuff in the Default Domain Policy and Default Domain Controller Policy.");
                        continue;
                    }
                    gpoData.Add("UID", gpoUid);
                    string gpoDn = gpoDe.Properties["distinguishedName"].Value.ToString();
                    gpoData.Add("Distinguished Name", gpoDn);
                    string gpoCreated = gpoDe.Properties["whenCreated"].Value.ToString();
                    gpoData.Add("Created", gpoCreated);

                    // 3= all disabled
                    // 2= computer configuration settings disabled
                    // 1= user policy disabled
                    // 0 = all enabled
                    string gpoFlags = gpoDe.Properties["flags"].Value.ToString();
                    string gpoEnabledStatus = "";
                    switch (gpoFlags)
                    {
                        case "0":
                            gpoEnabledStatus = "Enabled";
                            break;
                        case "1":
                            gpoEnabledStatus = "User Policy Disabled";
                            break;
                        case "2":
                            gpoEnabledStatus = "Computer Policy Disabled";
                            break;
                        case "3":
                            gpoEnabledStatus = "Disabled";
                            break;
                        default:
                            gpoEnabledStatus = "Couldn't process GPO Enabled Status. Weird.";
                            break;
                    }
                    gpoData.Add("GPO Status", gpoEnabledStatus);
                    // get the acl
                    ActiveDirectorySecurity gpoAcl = gpoDe.ObjectSecurity;

                    JObject gpoAclJObject = new JObject();
                    AccessControlSections sections = AccessControlSections.All;
                    string sddlString = gpoAcl.GetSecurityDescriptorSddlForm(sections);
                    JObject parsedSDDL = ParseSddl.ParseSddlString(sddlString, SecurableObjectType.DirectoryServiceObject);
                
                    foreach (KeyValuePair<string, JToken> thing in parsedSDDL)
                    {
                        if ((thing.Key == "Owner") && (thing.Value.ToString() != "DOMAIN_ADMINISTRATORS"))
                        {
                            gpoAclJObject.Add("Owner", thing.Value.ToString());
                            continue;
                        }

                        if ((thing.Key == "Group") && (thing.Value.ToString() != "DOMAIN_ADMINISTRATORS"))
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

                                if (aceInterestLevel >= GlobalVar.IntLevelToShow)
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
                        gpoData.Add("ACLs", gpoAclJObject);
                    }
                    
                    gposData.Add(gpoUid, gpoData);
                }
        
        
                return gposData;
            }
            catch (Exception exception)
            {
                Utility.Output.DebugWrite(exception.ToString());
                Console.ReadKey();
                Environment.Exit(1);
            }

            return null;
        }

        public static JObject GetGpoPackages(string domain)
        {
            // this bit c/o @grouppolicyguy
            DirectorySearcher packageSearcher = new DirectorySearcher("LDAP://" + domain + "/System/Policies");
            packageSearcher.Filter = "(objectClass=packageRegistration)";
            packageSearcher.PropertiesToLoad.Add("displayName");
            packageSearcher.PropertiesToLoad.Add("distinguishedName");
            packageSearcher.PropertiesToLoad.Add("msiFileList");
            packageSearcher.PropertiesToLoad.Add("msiScriptName");
            packageSearcher.PropertiesToLoad.Add("productCode");
            packageSearcher.PropertiesToLoad.Add("whenCreated");
            packageSearcher.PropertiesToLoad.Add("whenChanged");
            packageSearcher.PropertiesToLoad.Add("upgradeProductCode");
            packageSearcher.PropertiesToLoad.Add("cn");

            JObject gpoPackages = new JObject();

            SearchResultCollection foundPackages = packageSearcher.FindAll();
            if (foundPackages.Count > 0)
            {
                //iterate through the apps found
                string displayName;
                foreach (SearchResult package in foundPackages)
                {
                    string[] lvItems = new string[8];
                    try
                    {
                        displayName = package.Properties["displayName"][0].ToString();
                        //check to see if there are transforms
                        if (package.Properties["msiFileList"].Count > 1)
                        {
                            for (int i = 0; i < package.Properties["msiFileList"].Count; i++)
                            {
                                string[] splitPath = package.Properties["msiFileList"][i].ToString()
                                    .Split(new Char[] { ':' });
                                if (splitPath[0] == "0")
                                    lvItems[2] = splitPath[1];
                                else
                                {
                                    // if there is more than one transform, need to concatenate them
                                    if (package.Properties["msiFileList"].Count > 2)
                                    {
                                        lvItems[3] = splitPath[1] + ";" + lvItems[3];
                                    }
                                    else
                                        lvItems[3] = splitPath[1];
                                }
                            }
                        }
                        else
                        {
                            lvItems[2] = package.Properties["msiFileList"][0].ToString()
                                .TrimStart(new char[] { '0', ':' });
                            lvItems[3] = "";
                        }

                        //the product code is a byte array, so we need to get the enum on it and iterate through the collection
                        ResultPropertyValueCollection colProductCode = package.Properties["productCode"];
                        byte[] productCodeBytes = (byte[])colProductCode[0];
                        Guid productCodeGuid = new Guid(productCodeBytes);
                        // and again for the upgradeCode
                        ResultPropertyValueCollection colUpgradeCode = package.Properties["upgradeProductCode"];
                        byte[] upgradeCodeBytes = (byte[])colUpgradeCode[0];
                        Guid upgradeCodeGuid = new Guid(upgradeCodeBytes);

                        lvItems[4] = productCodeGuid.ToString();
                        lvItems[7] = upgradeCodeGuid.ToString();

                        //now do the whenChanged and whenCreated stuff
                        lvItems[5] = ((DateTime)(package.Properties["whenCreated"][0])).ToString("G");
                        lvItems[6] = ((DateTime)(package.Properties["whenChanged"][0])).ToString("G");
                        //Next we need to find the GPO this app is in
                        string DN = package.Properties["adsPath"][0].ToString();
                        string[] arrFQDN = DN.Split(new Char[] { ',' });
                        string FQDN = "";
                        for (int i = 0; i != arrFQDN.Length; i++)
                        {
                            if (i > 3)
                            {
                                //if its the first one, don't put a comma in front of it
                                if (i == 4)
                                    FQDN = arrFQDN[i];
                                else
                                    FQDN = FQDN + "," + arrFQDN[i];
                            }
                        }

                        FQDN = "LDAP://" + FQDN;
                        DirectoryEntry GPOPath = new DirectoryEntry(FQDN);
                        lvItems[0] = GPOPath.Properties["Name"][0].ToString();
                        //now resolve whether the app is published or assigned
                        if (arrFQDN[3] == "CN=User")
                        {
                            if (package.Properties["msiScriptName"][0].ToString() == "A")
                                lvItems[1] = "User Assigned";
                            if (package.Properties["msiScriptName"][0].ToString() == "P")
                                lvItems[1] = "User Published";
                            if (package.Properties["msiScriptName"][0].ToString() == "R")
                                lvItems[1] = "Package Removed";
                        }
                        else if (package.Properties["msiScriptName"][0].ToString() == "R")
                            lvItems[1] = "Package Removed";

                        else
                            lvItems[1] = "Computer Assigned";

                        JObject gpoPackage = new JObject();
                        gpoPackage.Add("Display Name", displayName);
                        gpoPackage.Add("MSI Path", lvItems[2]);
                        //gpoPackage.Add("MsiPath2", lvItems[3]);
                        gpoPackage.Add("Changed", lvItems[5]);
                        gpoPackage.Add("Created", lvItems[6]);
                        gpoPackage.Add("Type", lvItems[1]);
                        gpoPackage.Add("ProductCode", productCodeGuid.ToString());
                        gpoPackage.Add("Upgrade Code", upgradeCodeGuid.ToString());
                        gpoPackage.Add("ParentGPO", lvItems[0]);
                        ResultPropertyValueCollection cnCol = package.Properties["cn"];
                        gpoPackages.Add(cnCol[0].ToString(), gpoPackage);

                    }
                    catch (Exception e)
                    {
                        Utility.Output.DebugWrite(e.ToString());
                    }
                }
            }

            return gpoPackages;
        }
    
        public static string GetDomainSid()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            string domainSid = id.User.AccountDomainSid.ToString();
            return domainSid;
        }
    }
}
