using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Grouper2;

class LDAPstuff
    {
        public static JObject GetDomainGpos()
        {
            DirectoryEntry rootDse = new DirectoryEntry("LDAP://rootDSE");
            DirectoryEntry root = new DirectoryEntry("GC://" + rootDse.Properties["defaultNamingContext"].Value);
            //DirectoryEntry cfg = new DirectoryEntry("LDAP://" + rootDse.Properties["configurationnamingcontext"].Value);
            //DirectoryEntry exRights = new DirectoryEntry("LDAP://cn=Extended-rights," + rootDse.Properties["configurationnamingcontext"].Value);
            // create and populate a hashtable with extended rights from the domain
            //Hashtable exRighthash = new Hashtable();
            //foreach (DirectoryEntry chent in exRights.Children)
            //{
            //    if (exRighthash.ContainsKey(chent.Properties["rightsGuid"].Value) == false)
            //    {
            //        exRighthash.Add(chent.Properties["rightsGuid"].Value, chent.Properties["DisplayName"].Value);
            //    }
            //}
            //
            //DirectorySearcher cfgsearch = new DirectorySearcher(cfg);
            //cfgsearch.Filter = "(objectCategory=msExchPrivateMDB)";
            //cfgsearch.PropertiesToLoad.Add("distinguishedName");
            //cfgsearch.SearchScope = SearchScope.Subtree;
            //SearchResultCollection res = cfgsearch.FindAll();
            //foreach (SearchResult se in res)
            //{
            //    DirectoryEntry ssStoreObj = se.GetDirectoryEntry();
            //    ActiveDirectorySecurity StoreobjSec = ssStoreObj.ObjectSecurity;
            //    AuthorizationRuleCollection Storeacls =
            //        StoreobjSec.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            //    foreach (ActiveDirectoryAccessRule ace in Storeacls)
            //    {
            //        if (ace.IdentityReference.Value != "S-1-5-7" & ace.IdentityReference.Value != "S-1-1-0" &
            //            ace.IsInherited != true)
            //        {
            //            DirectoryEntry sidUser = new DirectoryEntry("LDAP://");
            //            Console.WriteLine(sidUser.Properties["DisplayName"].Value.ToString());
            //            Console.WriteLine(exRighthash[ace.ObjectType.ToString()].ToString());
            //        }
            //
            //
            //    }
            //}


        // make a searcher to find GPOs
        DirectorySearcher searcher = new DirectorySearcher(root)
            {
                Filter = "(objectClass=groupPolicyContainer)",
                SecurityMasks = SecurityMasks.Dacl | SecurityMasks.Owner
            };

            SearchResultCollection gpos = searcher.FindAll();


        
        

        

        // new dictionary for data from each GPO to go into
            JObject gposData = new JObject();

            foreach (SearchResult gpo in gpos)
            {
                // object for all data for this one gpo
                JObject gpoData = new JObject();
                DirectoryEntry gpoDe = gpo.GetDirectoryEntry();
                // get some useful attributes of the gpo
                string gpoDispName = gpoDe.Properties["displayName"].Value.ToString();
                gpoData.Add("Display Name", gpoDispName);
                string gpoUid = gpoDe.Properties["name"].Value.ToString();
                gpoData.Add("UID", gpoUid);
                string gpoDn = gpoDe.Properties["distinguishedName"].Value.ToString();
                gpoData.Add("Distinguished Name", gpoDn);

            // get the acl
            ActiveDirectorySecurity gpoAcl = gpoDe.ObjectSecurity;
                // make a JObject to put the acl in
                JObject gpoAclJObject = new JObject();
                //iterate over the aces in the acl
                foreach (ActiveDirectoryAccessRule gpoAce in gpoAcl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)))
                {
                    int aceInterestLevel = 1;
                    ActiveDirectoryRights adRightsObj = gpoAce.ActiveDirectoryRights;
                    if ((adRightsObj & ActiveDirectoryRights.ExtendedRight) != 0)
                    {
                        Utility.DebugWrite("Fuck, I still have to deal with Extended Rights.");
                    }
                    // get the rights quick and dirty
                    string adRights = gpoAce.ActiveDirectoryRights.ToString();
                    // clean the commas out
                    string cleanAdRights = adRights.Replace(", ", " ");
                    // chuck them into an array
                    string[] adRightsArray = cleanAdRights.Split(' ');
                    // an array of interesting privs
                    string[] intRightsArray = new string[]{ "WriteOwner", "GenericAll", "WriteProperty", "WriteDacl", "CreateChild", "DeleteChild", "Self", "DeleteTree", "Delete"};
                    // if we see one of these, the ACE just got more interesting.
                    foreach (string right in adRightsArray)
                    {
                        if (intRightsArray.Contains(right))
                        {
                            aceInterestLevel++;
                        }
                    }
                    string trusteeSid = gpoAce.IdentityReference.ToString();
                    // array of sid endings for the SIDs of default high-priv trustees.
                    // this is extremely lazy but it will work for now.
                    string[] boringSidEndings = new string[] { "-3-0" , "-5-9", "5-18", "-512", "-519" };
                    // if the last 4 chars of trusteeSid match an entry in boringSidEndings, reduce the interest level back to default.
                    if (boringSidEndings.Contains(trusteeSid.Substring((trusteeSid.Length - 4), 4)))
                    {
                        aceInterestLevel = 1;
                    }
                    string trusteeName = GetUserFromSid(trusteeSid);
                    string acType = gpoAce.AccessControlType.ToString();
                    string trusteeNAcType = trusteeName + " - " + acType + " - " + trusteeSid;
// From the original Grouper
//// an array of permissions that aren't exciting
//                        $boringPerms = @()
//                        $boringPerms += "Read"
//                        $boringPerms += "Apply Group Policy"
//// an array of users who have RW permissions on GPOs by default, so they're boring too.
//                        $boringTrustees = @()
//                        $boringTrustees += "Domain Admins"
//                        $boringTrustees += "Enterprise Admins"
//                        $boringTrustees += "ENTERPRISE DOMAIN CONTROLLERS"
//                        $boringTrustees += "SYSTEM"

                    if (aceInterestLevel >= GlobalVar.IntLevelToShow)
                    {
                        // create a JObject of the new stuff we know 
                        JObject aceToMerge = new JObject()
                        {
                            new JProperty(trusteeNAcType, new JArray(JArray.FromObject(adRightsArray)))
                        };
                        gpoAclJObject.Merge(aceToMerge, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Union
                        });
                }
                }
                
                //add the JObject to our blob of data about the gpo
                if (gpoAclJObject.HasValues)
                {
                    gpoData.Add("ACLs", gpoAclJObject);
                }
            // then add all of the above to the big blob of data about all gpos
            gposData.Add(gpoUid, gpoData);
            }
            return gposData; 
        }
    
        public static string GetDomainSid()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            string domainSid = id.User.AccountDomainSid.ToString();
            return domainSid;
        }

        public static string GetUserFromSid(string sid)
        {
            string account = new System.Security.Principal.SecurityIdentifier(sid).Translate(typeof(System.Security.Principal.NTAccount)).ToString();
            return account;
        }
}