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
            userAction = Utility.GetActionString(userAction);

            // get the username and a bunch of other details:
            assessedUser.Add("Name", Utility.GetSafeString(gppUser, "@name"));
            assessedUser.Add("User Name", Utility.GetSafeString(gppUserProps, "@userName"));
            assessedUser.Add("Changed", Utility.GetSafeString(gppUser, "@changed"));
            assessedUser.Add("Account Disabled", Utility.GetSafeString(gppUserProps, "@acctDisabled"));
            assessedUser.Add("Password Never Expires", Utility.GetSafeString(gppUserProps, "@neverExpires"));
            assessedUser.Add("Description", Utility.GetSafeString(gppUserProps, "@description"));
            assessedUser.Add("Full Name", Utility.GetSafeString(gppUserProps, "@fullName"));
            assessedUser.Add("New Name", Utility.GetSafeString(gppUserProps, "@newName"));
            assessedUser.Add("Action", userAction);

            // check for cpasswords
            if (gppUserProps["@cpassword"] != null)
            {
                string cpassword = gppUserProps["@cpassword"].ToString();
                if (cpassword.Length > 0)
                {
                    string decryptedCpassword = "";
                    decryptedCpassword = Utility.DecryptCpassword(cpassword);
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
            //Utility.DebugWrite(assessedUser.ToString());
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
            groupAction = Utility.GetActionString(groupAction);

            // get the group name and a bunch of other details:
            assessedGroup.Add("Name", Utility.GetSafeString(gppGroup, "@name"));
            //TODO if the name is an interesting group, make the finding more interesting.
            string[] highPrivLocalGroups = new string[]
            {
                "Administrators",
                "Backup Operators",

            };
            
            assessedGroup.Add("Changed", Utility.GetSafeString(gppGroup, "@changed"));
            assessedGroup.Add("Description", Utility.GetSafeString(gppGroup, "@description"));
            assessedGroup.Add("New Name", Utility.GetSafeString(gppGroupProps, "@newName"));
            assessedGroup.Add("Delete All Users", Utility.GetSafeString(gppGroupProps, "@deleteAllUsers"));
            assessedGroup.Add("Delete All Groups", Utility.GetSafeString(gppGroupProps, "@deleteAllGroups"));
            assessedGroup.Add("Remove Accounts", Utility.GetSafeString(gppGroupProps, "@removeAccounts"));
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
                            Utility.DebugWrite("Something went squirrely with Group Memberships");
                            Utility.DebugWrite(members.Type.ToString());
                            Utility.DebugWrite(" " + membersType + " ");
                            Utility.DebugWrite(members.ToString());
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
            JObject assessedMember = new JObject
            {
                {"Name", Utility.GetSafeString(member, "@name")},
                {"Action", Utility.GetSafeString(member, "@action")}
            };
            string memberSid = Utility.GetSafeString(member, "@sid");
            if (memberSid.Length > 0)
            {
                assessedMember.Add("SID", memberSid);
                if (GlobalVar.OnlineChecks)
                {
                    string resolvedSID = LDAPstuff.GetUserFromSid(memberSid);
                    assessedMember.Add("Display Name From SID", resolvedSID);
                }
            }

            return assessedMember;
        }
    }
}