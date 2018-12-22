using System.Collections.Generic;
using System.Security.Principal;
using Grouper2;
using Newtonsoft.Json.Linq;

internal static class AssessInf
{
    public static JObject AssessPrivRights(JToken privRights)
    {
        JObject jsonData = JankyDb.Instance;
        JArray intPrivRights = (JArray)jsonData["privRights"]["item"];

        // create an object to put the results in
        Dictionary<string, Dictionary<string, string>> matchedPrivRights = new Dictionary<string, Dictionary<string, string>>();

        //set an intentionally non-matchy domainSid value unless we doing online checks.
        string domainSid = "X";
        if (GlobalVar.OnlineChecks)
        {
            domainSid = LDAPstuff.GetDomainSid();
        }

        foreach (JProperty privRight in privRights.Children<JProperty>())
        {
            foreach (JToken intPrivRight in intPrivRights)
            {
                // if the priv is interesting
                if ((string)intPrivRight["privRight"] == privRight.Name)
                {
                    //create a dict to put the trustees into
                    Dictionary<string, string> trusteesDict = new Dictionary<string, string>();
                    //then for each trustee it's granted to
                    foreach (string trustee in privRight.Value)
                    {
                        string displayName = "unknown";
                        // clean up the trustee SID
                        string trusteeClean = trustee.Trim('*');
                        JToken checkedSid = Utility.CheckSid(trusteeClean);

                        // display some info if they match.
                        if (checkedSid != null)
                        {
                            displayName = (string)checkedSid["displayName"];
                        }
                        // if they don't match, handle that.
                        else
                        {
                            if (GlobalVar.OnlineChecks)
                            {
                                try
                                {
                                    if (trusteeClean.StartsWith(domainSid))
                                    {
                                        string resolvedSid = LDAPstuff.GetUserFromSid(trusteeClean);
                                        displayName = resolvedSid;
                                    }
                                }
                                catch (IdentityNotMappedException)
                                {
                                    displayName = "Failed to resolve SID";

                                }
                            }
                        }
                        trusteesDict.Add(trusteeClean, displayName);
                    }
                    // add the results to our dictionary of trustees
                    string matchedPrivRightName = privRight.Name;
                    matchedPrivRights.Add(matchedPrivRightName, trusteesDict);
                }
            }
        }
        // cast our dict to a jobject and return it.
        JObject matchedPrivRightsJson = (JObject)JToken.FromObject(matchedPrivRights);
        return matchedPrivRightsJson;
    }

    public static JObject AssessRegValues(JToken regValues)
    {
        JObject jsonData = JankyDb.Instance;
        // get our data about what regkeys are interesting
        JArray intRegKeys = (JArray)jsonData["regKeys"]["item"];
        // set up a dictionary for our results to go into
        Dictionary<string, string[]> matchedRegValues = new Dictionary<string, string[]>();

        foreach (JProperty regValue in regValues.Children<JProperty>())
        {
            // iterate over the list of interesting keys in our json "db".
            foreach (JToken intRegKey in intRegKeys)
            {
                // if it matches
                if ((string)intRegKey["regKey"] == regValue.Name)
                {
                    string matchedRegKey = regValue.Name;
                    //create a list to put the values in
                    List<string> regKeyValueList = new List<string>();
                    foreach (string thing in regValue.Value)
                    {
                        // put the values in the list
                        regKeyValueList.Add(thing);
                    }
                    string[] regKeyValueArray = regKeyValueList.ToArray();
                    matchedRegValues.Add(matchedRegKey, regKeyValueArray);
                }
            }
        }
        // cast our output into a jobject and return it
        JObject matchedRegValuesJson = (JObject)JToken.FromObject(matchedRegValues);
        return matchedRegValuesJson;
    }

    public static JObject AssessSysAccess(JToken sysAccess)
    {
        //placeholder only
        JObject sysAccessJson = (JObject)JToken.FromObject(sysAccess);
        return sysAccessJson;
    }

    public static JObject AssessKerbPolicy(JToken kerbPolicy)
    {
        //placeholder only
        JObject sysAccessJson = (JObject)JToken.FromObject(kerbPolicy);
        return sysAccessJson;
    }

    public static JObject AssessRegKeys(JToken regKeys)
    {
        //placeholder only
        JObject regKeysJson = (JObject)JToken.FromObject(regKeys);
        return regKeysJson;
    }

    public static JObject AssessGroupMembership(JToken grpMembership)
    {
        //placeholder only
        JObject grpMembershipJson = (JObject)JToken.FromObject(grpMembership);
        return grpMembershipJson;
    }

    public static JObject AssessServiceGenSetting(JToken svcGenSetting)
    {
        //placeholder only
        JObject svcGenSettingJson = (JObject)JToken.FromObject(svcGenSetting);
        return svcGenSettingJson;
    }
}