using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
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

    public static JObject AssessGroupMembership(JToken parsedGrpMemberships)
    {
        // base interest level
        int interestLevel = 4;
        // really not sure about this one at all. Think it's ok now but could use a recheck.
        // output object
        JObject assessedGrpMemberships = new JObject();
        // cast input object
        JEnumerable<JToken> parsedGrpMembershipsEnumerable = parsedGrpMemberships.Children();

        foreach (JToken parsedGrpMembership in parsedGrpMembershipsEnumerable)
        {
            JProperty parsedGrpMembershipJProp = (JProperty) parsedGrpMembership;

            // break immediately if there's no value.
            if (parsedGrpMembershipJProp.Value.ToString() == "")
            {
                continue;
            }
            // strip the asterisk off the front of the line
            string cleanedKey = parsedGrpMembershipJProp.Name.Trim('*');
            // split out the sid from the 'memberof' vs 'members' bit.
            string[] splitKey = cleanedKey.Split('_');
            // get the sid
            string cleanSid = splitKey[0];
            // get the type of entry
            string memberWhat = splitKey[2];

            // check if the Key SID is a well known sid and get some info about it if it is.
            JToken checkedSid = Utility.CheckSid(cleanSid);
            string displayName = "";
            // if we're online, try to look up the Key SID
            if (GlobalVar.OnlineChecks)
            {
                displayName = LDAPstuff.GetUserFromSid(cleanSid);
            }
            // otherwise try to get it from the well known sid data
            else if (checkedSid != null)
            {
                displayName = checkedSid["displayName"].ToString();
            }
            else
            {
                displayName = cleanSid;
            }

            if (memberWhat == "Memberof")
            {
                JProperty assessedGrpMemberKey = AssessGrpMemberItem(parsedGrpMembershipJProp.Name.Split('_')[0]);

                if (parsedGrpMembershipJProp.Value is JArray)
                {
                    foreach (string rawGroup in parsedGrpMembershipJProp.Value)
                    {
                        JProperty assessedGrpMemberItem = AssessGrpMemberItem(rawGroup);
                        if (!(assessedGrpMemberships.ContainsKey(assessedGrpMemberItem.Name)))
                        {
                            assessedGrpMemberships.Add(
                                new JProperty(assessedGrpMemberItem.Name,
                                    new JObject(
                                        new JProperty("SID", assessedGrpMemberItem.Value),
                                        new JProperty("Members", new JObject())))
                                
                            );
                        }

                        JObject targetJObject = (JObject) assessedGrpMemberships[assessedGrpMemberItem.Name]["Members"];
                        targetJObject.Add(assessedGrpMemberKey);
                    }
                }
                else
                {


                    // get a cleaned up version of the memberof
                    JProperty assessedGrpMemberValue = AssessGrpMemberItem(parsedGrpMembershipJProp.Value.ToString());


                    if (!(assessedGrpMemberships.ContainsKey(assessedGrpMemberValue.Name)))
                    {
                        // create one
                        assessedGrpMemberships.Add(
                            new JProperty(assessedGrpMemberValue.Name,
                                new JObject(
                                new JProperty("SID", assessedGrpMemberValue.Value),
                                new JProperty("Members", new JObject())))
                        );
                    }

                    JObject targetJObject = (JObject)assessedGrpMemberships[assessedGrpMemberValue.Name]["Members"];
                    targetJObject.Add(assessedGrpMemberKey);
                }

            }
            else
            {
                // if we don't have an entry for this group yet
                //Utility.DebugWrite(displayName);
                if (!(assessedGrpMemberships.ContainsKey(displayName)))
                {
                    assessedGrpMemberships.Add(
                        new JProperty(displayName,
                            new JObject(
                                new JProperty("SID", cleanSid),
                                new JProperty("Members", new JObject())))
                    );
                }

                JObject targetJObject = (JObject)assessedGrpMemberships[displayName]["Members"];
                // iterate over members and put them in the appropriate JArray
                if (parsedGrpMembershipJProp.Value is JArray)
                {
                    foreach (string rawMember in parsedGrpMembershipJProp.Value)
                    {
                        JProperty assessedGrpMember = AssessGrpMemberItem(rawMember);
                        try
                        {
                            targetJObject.Add(assessedGrpMember);
                        }
                        catch (Exception e)
                        {
                            Utility.DebugWrite(e.ToString());
                        }
                    }
                }
                else
                {
                    JProperty assessedGrpMember = AssessGrpMemberItem(parsedGrpMembershipJProp.Value.ToString());
                    try
                    {
                        //Utility.DebugWrite(assessedGrpMember.ToString());
                        targetJObject.Add(assessedGrpMember);
                    }
                    catch (Exception e)
                    {
                        Utility.DebugWrite(e.ToString());
                    }
                }
            }
            
            // if the resulting interest level of this shit is sufficient, add it to the output JObject.
            if (GlobalVar.IntLevelToShow <= interestLevel)
            {
                return assessedGrpMemberships;
            }
        }

        return null;
    }

    public static JProperty AssessGrpMemberItem(string rawMember)
    {
        string memberDisplayName = rawMember;
        string memberSid = "unknown";
        // if it's a SID
        if (rawMember.StartsWith("*"))
        {
            // clean it up
            memberSid = rawMember.Trim('*');
            if (GlobalVar.OnlineChecks)
            {
                // look it up
                memberDisplayName = LDAPstuff.GetUserFromSid(memberSid);
            }
            else
            {
                // see if it's well known
                JToken checkedMemberSid = Utility.CheckSid(memberSid);
                if (checkedMemberSid != null)
                {
                    memberDisplayName = checkedMemberSid["displayName"].ToString();
                }
            }
        }
        return new JProperty(memberDisplayName, memberSid);
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