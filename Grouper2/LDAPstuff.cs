using Newtonsoft.Json.Linq;
using System;
using System.DirectoryServices;
using System.Collections.Generic;

class LDAPstuff
    {
        public static JObject GetDomainGpos()
        {
        DirectoryEntry rootDse = new DirectoryEntry("LDAP://rootDSE");
        DirectoryEntry root = new DirectoryEntry("GC://" + rootDse.Properties["defaultNamingContext"].Value.ToString());
        DirectorySearcher searcher = new DirectorySearcher(root)
        {
            Filter = "(objectClass=groupPolicyContainer)"
        };

        SearchResultCollection gpos = searcher.FindAll();

        // new dictionary for data from each GPO to go into
        Dictionary<string, Dictionary<string, string>> GPOsData = new Dictionary<string, Dictionary<string, string>>();

        foreach (SearchResult gpo in gpos)
        {
            // new dictionary for data from this GPO
            Dictionary<string, string> GPOData = new Dictionary<string, string>();

            DirectoryEntry gpoDe = gpo.GetDirectoryEntry();
            string gpoDn = gpo.GetDirectoryEntry().Properties["distinguishedName"].Value.ToString();
            string[] gpoUidSplit0 = gpoDn.Split(',');
            string[] gpoUidSplit1 = gpoUidSplit0[0].Split('=');
            string gpoUid = gpoUidSplit1[1];
            GPOData.Add("UID", gpoUid);
            GPOData.Add("DistinguishedName", gpoDn);
      
            DirectoryEntry gpoObject = new DirectoryEntry($"LDAP://{gpoDn}");
            GPOData.Add("DisplayName", gpoObject.Properties["displayName"].Value.ToString());
            GPOsData.Add(gpoUid, GPOData);
        }

        JObject domainGposJson = (JObject)JToken.FromObject(GPOsData);

        return domainGposJson; 
        }
    }