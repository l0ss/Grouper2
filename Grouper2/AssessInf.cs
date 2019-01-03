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
        JObject assessedPrivRights = new JObject();

        //set an intentionally non-matchy domainSid value unless we doing online checks.
        string domainSid = "X";
        if (GlobalVar.OnlineChecks)
        {
            domainSid = LDAPstuff.GetDomainSid();
        }
        //iterate over the entries
        foreach (JProperty privRight in privRights.Children<JProperty>())
        {
            // our interest level always starts at 1. Everything is boring until proven otherwise.
            int interestLevel = 1;
            foreach (JToken intPrivRight in intPrivRights)
            {
                // if the priv is interesting
                if ((string)intPrivRight["privRight"] == privRight.Name)
                {
                    //create a jobj to put the trustees into
                    JObject trustees = new JObject();
                    //then for each trustee it's granted to
                    foreach (string trustee in privRight.Value)
                    {
                        string displayName = "unknown";
                        // clean up the trustee SID
                        string trusteeClean = trustee.Trim('*');
                        // check if it's a well known trustee in our JankyDB
                        JToken checkedSid = Utility.CheckSid(trusteeClean);

                        // extract some info if they match.
                        if (checkedSid != null)
                        {
                            displayName = (string)checkedSid["displayName"];
                        }
                        // if they don't match, try to resolve the sid with the domain.
                        // tbh it would probably be better to do this the other way around and prefer the resolved sid output over the contents of jankydb. @liamosaur?
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
                                    displayName = "Failed to resolve SID with domain.";
                                }
                            }
                        }
                        trustees.Add(trusteeClean, displayName);
                    }
                    // add the results to our jobj of trustees if they are interesting enough.
                    string matchedPrivRightName = privRight.Name;
                    if (interestLevel >= GlobalVar.IntLevelToShow)
                    {
                        assessedPrivRights.Add(matchedPrivRightName, trustees);
                    }
                }
            }
        }
        
        return assessedPrivRights;
    }

    public static JObject AssessRegValues(JToken regValues)
    {
        int interestLevel = 1;
        JObject jsonData = JankyDb.Instance;
        // get our data about what regkeys are interesting
        JArray intRegKeys = (JArray)jsonData["regKeys"]["item"];
        // set up a jobj for our results to go into
        JObject assessedRegValues = new JObject();

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
                    if (interestLevel >= GlobalVar.IntLevelToShow)
                    {
                        JArray regKeyValueJArray = new JArray();
                        foreach (string value in regKeyValueArray)
                        {
                            regKeyValueJArray.Add(value);
                        }
                        assessedRegValues.Add(matchedRegKey, regKeyValueJArray);
                    }
                }
            }
        }
        return assessedRegValues;
    }

    public static JObject AssessSysAccess(JToken sysAccess)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 1;
        JObject sysAccessJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            sysAccessJson = (JObject) JToken.FromObject(sysAccess);
        }
        return sysAccessJson;
    }

    public static JObject AssessKerbPolicy(JToken kerbPolicy)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 1;
        JObject kerbPolicyJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            kerbPolicyJson = (JObject)JToken.FromObject(kerbPolicy);
        }
        return kerbPolicyJson;
    }

    public static JObject AssessRegKeys(JToken regKeys)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 1;
        JObject regKeysJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            regKeys = (JObject)JToken.FromObject(regKeys);
        }
        return regKeysJson;
    }

    public static JObject AssessGroupMembership(JToken grpMembership)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 1;
        JObject grpMembershipJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            grpMembershipJson = (JObject)JToken.FromObject(grpMembership);
        }
        return grpMembershipJson;
    }

    public static JObject AssessServiceGenSetting(JToken svcGenSetting)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 1;
        JObject svcGenSettingJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            //Utility.DebugWrite(svcGenSetting.ToString());
            svcGenSettingJson = (JObject)JToken.FromObject(svcGenSetting);
        }
        return svcGenSettingJson;
    }
}