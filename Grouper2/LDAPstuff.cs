using Newtonsoft.Json.Linq;
using System;
using System.DirectoryServices;
using System.Collections.Generic;

class LDAPstuff
    {
        static public JObject GetDomainGPOs()
        {
        DirectoryEntry rootDse = new DirectoryEntry("LDAP://rootDSE");
        DirectoryEntry root = new DirectoryEntry("GC://" + rootDse.Properties["defaultNamingContext"].Value.ToString());
        DirectorySearcher searcher = new DirectorySearcher(root)
        {
            Filter = "(objectClass=groupPolicyContainer)"
        };

        SearchResultCollection GPOs = searcher.FindAll();

        // new dictionary for data from each GPO to go into
        Dictionary<string, Dictionary<string, string>> GPOsData = new Dictionary<string, Dictionary<string, string>>();

        foreach (SearchResult GPO in GPOs)
        {
            // new dictionary for data from this GPO
            Dictionary<string, string> GPOData = new Dictionary<string, string>();

            DirectoryEntry GPODE = GPO.GetDirectoryEntry();
            string GPODN = GPO.GetDirectoryEntry().Properties["distinguishedName"].Value.ToString();
            string[] GPOUIDsp0 = GPODN.Split(',');
            string[] GPOUIDsp1 = GPOUIDsp0[0].Split('=');
            string GPOUID = GPOUIDsp1[1];
            GPOData.Add("UID", GPOUID);
            GPOData.Add("DistinguishedName", GPODN);
      
            DirectoryEntry gpoObject = new DirectoryEntry($"LDAP://{GPODN}");
            //GPOData.Add("SDDL", gpoObject.ObjectSecurity.ToString());
            GPOData.Add("DisplayName", gpoObject.Properties["displayName"].Value.ToString());
            GPOsData.Add(GPOUID, GPOData);
        }

        JObject DomainGPOsJson = (JObject)JToken.FromObject(GPOsData);
        //Console.WriteLine(DomainGPOsJson.ToString());

        return DomainGPOsJson; 
        }
    }