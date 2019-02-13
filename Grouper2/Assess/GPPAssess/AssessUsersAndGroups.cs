using System;
using System.CodeDom;
using System.Collections.Generic;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        private JObject GetAssessedGroups(JObject gppCategory)
        {
            JObject assessedGroups = new JObject();
            JObject assessedUsers = new JObject();

            if (gppCategory["Group"] != null)
            {
                if (gppCategory["Group"] is JArray)
                {
                    foreach (JObject gppGroup in gppCategory["Group"])
                    {
                        JObject assessedGroup = GetAssessedGroup(gppGroup);
                        if (assessedGroup != null)
                        {
                            assessedGroups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                        }
                    }
                }
                else
                {
                    JObject gppGroup = (JObject) JToken.FromObject(gppCategory["Group"]);
                    JObject assessedGroup = GetAssessedGroup(gppGroup);
                    if (assessedGroup != null)
                    {
                        assessedGroups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                    }
                }
            }

            JObject assessedGppGroups = (JObject) JToken.FromObject(assessedGroups);

            if (gppCategory["User"] != null)
            {
                if (gppCategory["User"] is JArray)
                {
                    foreach (JObject gppUser in gppCategory["User"])
                    {
                        JObject assessedUser = GetAssessedUser(gppUser);
                        if (assessedUser != null)
                        {
                            assessedUsers.Add(gppUser["@uid"].ToString(), assessedUser);
                        }
                    }
                }
                else
                {
                    JObject gppUser = (JObject) JToken.FromObject(gppCategory["User"]);
                    JObject assessedUser = GetAssessedUser(gppUser);
                    if (assessedUser != null)
                    {
                        assessedUsers.Add(gppUser["@uid"].ToString(), assessedUser);
                    }
                }
            }

            JObject assessedGppUsers = (JObject) JToken.FromObject(assessedUsers);

            JProperty assessedUsersJson = new JProperty("GPP Users", assessedGppUsers);
            JProperty assessedGroupsJson = new JProperty("GPP Groups", assessedGppGroups);
            // chuck the users and groups together in one JObject
            JObject assessedGppGroupsJson = new JObject();
            // only want to actually output these things if there's anything useful in them.
            if (assessedUsers.Count > 0)
            {
                assessedGppGroupsJson.Add(assessedUsersJson);
            }

            if (assessedGroups.Count > 0)
            {
                assessedGppGroupsJson.Add(assessedGroupsJson);
            }
            
            return assessedGppGroupsJson;
        }

        private JObject GetAssessedUser(JObject gppUser)
        {
            // jobj for results from this specific user.
            JObject assessedUser = new JObject();

            //set base interest level
            int interestLevel = 3;

            JToken gppUserProps = gppUser["Properties"];

            // check what the entry is doing to the user and turn it into real word
            string userAction = gppUserProps["@action"].ToString();
            userAction = JUtil.GetActionString(userAction);

            // get the username and a bunch of other details:
            assessedUser.Add("Name", JUtil.GetSafeString(gppUser, "@name"));
            assessedUser.Add("User Name", JUtil.GetSafeString(gppUserProps, "@userName"));
            assessedUser.Add("Changed", JUtil.GetSafeString(gppUser, "@changed"));
            assessedUser.Add("Account Disabled", JUtil.GetSafeString(gppUserProps, "@acctDisabled"));
            assessedUser.Add("Password Never Expires", JUtil.GetSafeString(gppUserProps, "@neverExpires"));
            assessedUser.Add("Description", JUtil.GetSafeString(gppUserProps, "@description"));
            assessedUser.Add("Full Name", JUtil.GetSafeString(gppUserProps, "@fullName"));
            assessedUser.Add("New Name", JUtil.GetSafeString(gppUserProps, "@newName"));
            assessedUser.Add("Action", userAction);

            // check for cpasswords
            if (gppUserProps["@cpassword"] != null)
            {
                string cpassword = gppUserProps["@cpassword"].ToString();
                if (cpassword.Length > 0)
                {
                    string decryptedCpassword = "";
                    decryptedCpassword = Util.DecryptCpassword(cpassword);
                    // if we find one, that's super interesting.
                    assessedUser.Add("Cpassword", cpassword);
                    assessedUser.Add("Decrypted Password", decryptedCpassword);
                    interestLevel = 10;
                }
            }

            // if it's too boring to be worth showing, return an empty jobj.
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
            }
            //Utility.Output.DebugWrite(assessedUser.ToString());
            return assessedUser;
        }

        private JObject GetAssessedGroup(JObject gppGroup)
        {
            //jobj for results from this specific group
            JObject assessedGroup = new JObject();
            int interestLevel = 3;

            JToken gppGroupProps = gppGroup["Properties"];

            // check what the entry is doing to the group and turn it into real word
            string groupAction = gppGroupProps["@action"].ToString();
            groupAction = JUtil.GetActionString(groupAction);

            // get the group name and a bunch of other details:
            assessedGroup.Add("Name", JUtil.GetSafeString(gppGroup, "@name"));
            //TODO if the name is an interesting group, make the finding more interesting.
            
            
            assessedGroup.Add("Changed", JUtil.GetSafeString(gppGroup, "@changed"));
            assessedGroup.Add("Description", JUtil.GetSafeString(gppGroup, "@description"));
            assessedGroup.Add("New Name", JUtil.GetSafeString(gppGroupProps, "@newName"));
            assessedGroup.Add("Delete All Users", JUtil.GetSafeString(gppGroupProps, "@deleteAllUsers"));
            assessedGroup.Add("Delete All Groups", JUtil.GetSafeString(gppGroupProps, "@deleteAllGroups"));
            assessedGroup.Add("Remove Accounts", JUtil.GetSafeString(gppGroupProps, "@removeAccounts"));
            assessedGroup.Add("Action", groupAction);

            JArray gppGroupMemberJArray = new JArray();


            if (gppGroupProps["Members"] != null)
            {
                if (!(gppGroupProps["Members"] is JValue))
                {
                    if (gppGroupProps["Members"]["Member"] != null)
                    {
                        JToken members = gppGroupProps["Members"]["Member"];
                        string membersType = members.Type.ToString();
                        if (membersType == "Array")
                        {
                            foreach (JToken member in members.Children())
                            {
                                gppGroupMemberJArray.Add(GetAssessedGroupMember(member));
                            }
                        }
                        else if (membersType == "Object")
                        {
                            gppGroupMemberJArray.Add(GetAssessedGroupMember(members));
                        }
                        else
                        {
                            Utility.Output.DebugWrite("Something went squirrely with Group Memberships");
                            Utility.Output.DebugWrite(members.Type.ToString());
                            Utility.Output.DebugWrite(" " + membersType + " ");
                            Utility.Output.DebugWrite(members.ToString());
                        }
                    }
                }
            }

            string membersString = gppGroupMemberJArray.ToString();
            membersString = membersString.Replace("\"", "");
            membersString = membersString.Replace(",", "");
            membersString = membersString.Replace("[", "");
            membersString = membersString.Replace("]", "");
            membersString = membersString.Replace("{", "");
            membersString = membersString.Replace("}", "");
            membersString = membersString.Replace("    ", "");
            membersString = membersString.Replace("\n\n\n", "\n");
            membersString = membersString.Trim();

            assessedGroup.Add("Members", membersString);

            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
            }

            return assessedGroup;
        }

        private JObject GetAssessedGroupMember(JToken member)
        {
            List<JProperty> memberProps = new List<JProperty>
            {
                JUtil.GetSafeJProp("Name", member, "@name"),
                JUtil.GetSafeJProp("Action", member, "@action"),
                JUtil.GetSafeJProp("SID", member, "@sid")
            };

            string memberSid = JUtil.GetSafeString(member, "@sid");
            if (!string.IsNullOrEmpty(memberSid))
            {
                string resolvedSID = LDAPstuff.GetUserFromSid(memberSid);
                memberProps.Add(new JProperty("Display Name From SID", resolvedSID));
            }

            JObject assessedMember = new JObject();
            foreach (JProperty memberProp in memberProps)
            {
                if (memberProp != null)
                {
                    assessedMember.Add(memberProp);
                }
            }

            if (assessedMember.HasValues) return assessedMember;
            return null;
        }
    }
}