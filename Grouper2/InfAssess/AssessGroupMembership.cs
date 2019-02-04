using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.InfAssess
{
    internal static partial class AssessInf
    {
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

                        JObject targetJObject = (JObject) assessedGrpMemberships[assessedGrpMemberValue.Name]["Members"];
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

                    JObject targetJObject = (JObject) assessedGrpMemberships[displayName]["Members"];
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
    }
}