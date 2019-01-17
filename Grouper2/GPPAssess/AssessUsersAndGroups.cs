using Newtonsoft.Json.Linq;

namespace Grouper2
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
            //foreach (JToken gppUser in gppUsers) {
            // jobj for results from this specific user.
            JObject assessedUser = new JObject();

            //set base interest level
            int interestLevel = 3;

            JToken gppUserProps = gppUser["Properties"];

            // check what the entry is doing to the user and turn it into real word
            string userAction = gppUserProps["@action"].ToString();
            userAction = Utility.GetActionString(userAction);

            // get the username and a bunch of other details:
            assessedUser.Add("Name", gppUser["@name"].ToString());
            assessedUser.Add("User Name", gppUserProps["@userName"].ToString());
            assessedUser.Add("DateTime Changed", gppUser["@changed"].ToString());
            assessedUser.Add("Account Disabled", gppUserProps["@acctDisabled"].ToString());
            assessedUser.Add("Password Never Expires", gppUserProps["@neverExpires"].ToString());
            assessedUser.Add("Description", gppUserProps["@description"].ToString());
            assessedUser.Add("Full Name", gppUserProps["@fullName"].ToString());
            assessedUser.Add("New Name", gppUserProps["@newName"].ToString());
            assessedUser.Add("Action", userAction);

            // check for cpasswords
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

            // if it's too boring to be worth showing, return an empty jobj.
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
            }

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
            assessedGroup.Add("DateTime Changed", Utility.GetSafeString(gppGroup, "@changed"));
            assessedGroup.Add("Description", Utility.GetSafeString(gppGroupProps, "@description"));
            assessedGroup.Add("New Name", Utility.GetSafeString(gppGroupProps, "@newName"));
            assessedGroup.Add("Delete All Users", Utility.GetSafeString(gppGroupProps, "@deleteAllUsers"));
            assessedGroup.Add("Delete All Groups", Utility.GetSafeString(gppGroupProps, "@deleteAllGroups"));
            assessedGroup.Add("Remove Accounts", Utility.GetSafeString(gppGroupProps, "@removeAccounts"));
            assessedGroup.Add("Action", groupAction);

            JArray gppGroupMemberArray = new JArray();
            if (gppGroupProps["Members"] != null)
            {
                JToken members = gppGroupProps["Members"]["Member"];
                string membersType = members.Type.ToString();
                if (membersType == "Array")
                {
                    foreach (JToken member in members.Children())
                    {
                        gppGroupMemberArray.Add(GetAssessedGroupMember(member));
                    }
                }
                else if (membersType == "Object")
                {
                    gppGroupMemberArray.Add(GetAssessedGroupMember(members));
                }
                else
                {
                    if (GlobalVar.DebugMode)
                    {
                        Utility.DebugWrite("Something went squirrely with Group Memberships");
                        Utility.DebugWrite(members.Type.ToString());
                        Utility.DebugWrite(" " + membersType + " ");
                        Utility.DebugWrite(members.ToString());
                    }
                }
            }

            assessedGroup.Add("Members", gppGroupMemberArray);

            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
            }

            return assessedGroup;
        }

        private JObject GetAssessedGroupMember(JToken member)
        {
            JObject assessedMember = new JObject();
            assessedMember.Add("Name", Utility.GetSafeString(member, "@name"));
            assessedMember.Add("Action", Utility.GetSafeString(member, "@action"));
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