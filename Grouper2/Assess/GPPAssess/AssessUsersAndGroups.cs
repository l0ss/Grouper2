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
            //set base interest level
            int interestLevel = 3;

            JToken gppUserProps = gppUser["Properties"];

            // check what the entry is doing to the user and turn it into real word
            string userAction = gppUserProps["@action"].ToString();
            userAction = JUtil.GetActionString(userAction);

            string cpassword = "";
            string decryptedCpassword = "";
            // check for cpasswords
            if (gppUserProps["@cpassword"] != null)
            {
                cpassword = gppUserProps["@cpassword"].ToString();
                if (cpassword.Length > 0)
                {
                    decryptedCpassword = Util.DecryptCpassword(cpassword);
                    interestLevel = 10;
                }
            }

            List<JToken> userProps = new List<JToken>
            {
                JUtil.GetSafeJProp("Name", gppUser, "@name"),
                JUtil.GetSafeJProp("Changed", gppUser, "@changed"),
                JUtil.GetSafeJProp("User Name", gppUserProps, "@userName"),
                JUtil.GetSafeJProp("cPassword", cpassword),
                JUtil.GetSafeJProp("Decrypted Password", decryptedCpassword),
                JUtil.GetSafeJProp("Account Disabled", gppUserProps, "@acctDisabled"),
                JUtil.GetSafeJProp("Password Never Expires", gppUserProps, "@neverExpires"),
                JUtil.GetSafeJProp("Description", gppUserProps, "@description"),
                JUtil.GetSafeJProp("Full Name", gppUserProps, "@fullName"),
                JUtil.GetSafeJProp("New Name", gppUserProps, "@newName"),
                JUtil.GetSafeJProp("Action", userAction),
            };

            JObject assessedUser = new JObject();
            foreach (JToken userProp in userProps)
            {
                if (userProp != null)
                {
                    assessedUser.Add(userProp);
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
            int interestLevel = 3;

            JToken gppGroupProps = gppGroup["Properties"];

            // check what the entry is doing to the group and turn it into real word
            string groupAction = JUtil.GetActionString(gppGroupProps["@action"].ToString());
            
            //TODO if the name is an interesting group, make the finding more interesting.

            JArray gppGroupMemberJArray = new JArray();
            string membersString = "";

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
                            Output.DebugWrite("Something went squirrely with Group Memberships");
                            Output.DebugWrite(members.Type.ToString());
                            Output.DebugWrite(" " + membersType + " ");
                            Output.DebugWrite(members.ToString());
                        }
                    }
                }
                // munge jarray string to make it tidier.
                membersString = gppGroupMemberJArray.ToString();
                membersString = membersString.Replace("\"", "");
                membersString = membersString.Replace(",", "");
                membersString = membersString.Replace("[", "");
                membersString = membersString.Replace("]", "");
                membersString = membersString.Replace("{", "");
                membersString = membersString.Replace("}", "");
                membersString = membersString.Replace("    ", "");
                membersString = membersString.Replace("\r\n  \r\n  \r\n", "\r\n\r\n");
                membersString = membersString.Trim();
            }

            List<JToken> groupProps = new List<JToken>
            {
                JUtil.GetSafeJProp("Name", gppGroup, "@name"),
                JUtil.GetSafeJProp("Description", gppGroup, "@description"),
                JUtil.GetSafeJProp("New Name", gppGroupProps, "@newName"),
                JUtil.GetSafeJProp("Delete All Users", gppGroupProps, "@deleteAllUsers"),
                JUtil.GetSafeJProp("Delete All Groups", gppGroupProps, "@deleteAllGroups"),
                JUtil.GetSafeJProp("Remove Accounts", gppGroupProps, "@removeAccounts"),
                JUtil.GetSafeJProp("Action", groupAction),
                JUtil.GetSafeJProp("Members", membersString)
            };

            JObject assessedGroup = new JObject();

            foreach (JProperty groupProp in groupProps)
            {
                if (groupProp != null)
                {
                    assessedGroup.Add(groupProp);
                }
            }

            if ((interestLevel > GlobalVar.IntLevelToShow) && (assessedGroup.HasValues))
            {
                return assessedGroup;
            }

            return null;
        }

        private JObject GetAssessedGroupMember(JToken member)
        {
            List<JToken> memberProps = new List<JToken>
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