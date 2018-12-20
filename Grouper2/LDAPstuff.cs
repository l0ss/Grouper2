using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using Grouper2;

class LDAPstuff
    {
        public static JObject GetDomainGpos()
        {
            DirectoryEntry rootDse = new DirectoryEntry("LDAP://rootDSE");
            DirectoryEntry root = new DirectoryEntry("GC://" + rootDse.Properties["defaultNamingContext"].Value);
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
                string gpoDn = gpoDe.Properties["distinguishedName"].Value.ToString();
                gpoData.Add("Distinguished Name", gpoDn);
                string gpoUid = gpoDe.Properties["name"].Value.ToString();
                gpoData.Add("UID", gpoUid);
                string gpoDispName = gpoDe.Properties["displayName"].Value.ToString();
                gpoData.Add("Display Name", gpoDispName);
                // get the acl
                //JObject gpoAclJson = new JObject();
                ActiveDirectorySecurity gpoAcl = gpoDe.ObjectSecurity;
                // make a JObject to put the acl in
                JObject gpoAclJObject = new JObject();
                //iterate over the aces in the acl
                foreach (ActiveDirectoryAccessRule gpoAce in gpoAcl.GetAccessRules(true, true, typeof(NTAccount)))
                {
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

                    string trustee = gpoAce.IdentityReference.ToString();
                    string acType = gpoAce.AccessControlType.ToString();

                    string trusteeNAcType = trustee + " " + acType;
                    
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
                
                //add the JObject to our blob of data about the gpo
                gpoData.Add("ACLs", gpoAclJObject);
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

        public static string GetUserFromSID(string sid)
        {
            string account = new System.Security.Principal.SecurityIdentifier(sid).Translate(typeof(System.Security.Principal.NTAccount)).ToString();
            return account;
        }
}