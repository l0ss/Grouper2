using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        // checked for JUtil.GetSafeJProp
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
                        string gppGroupUid = gppGroup["@uid"].ToString();
                        assessedGroups.Merge(JUtil.GetSafeJProp(gppGroupUid, assessedGroup));
                    }
                }
                else
                {
                    JObject gppGroup = (JObject) JToken.FromObject(gppCategory["Group"]);
                    JObject assessedGroup = GetAssessedGroup(gppGroup);
                    string gppGroupUid = gppGroup["@uid"].ToString();
                    assessedGroups.Merge(JUtil.GetSafeJProp(gppGroupUid, assessedGroup));
                }
            }
            
            if (gppCategory["User"] != null)
            {
                if (gppCategory["User"] is JArray)
                {
                    foreach (JObject gppUser in gppCategory["User"])
                    {
                        JObject assessedUser = GetAssessedUser(gppUser);
                        string gppUserUid = gppUser["@uid"].ToString();
                        assessedUsers.Merge(JUtil.GetSafeJProp(gppUserUid, assessedUser));
                    }
                }
                else
                {
                    JObject gppUser = (JObject) JToken.FromObject(gppCategory["User"]);
                    JObject assessedUser = GetAssessedUser(gppUser);
                    string gppUserUid = gppUser["@uid"].ToString();
                    assessedUsers.Merge(JUtil.GetSafeJProp(gppUserUid, assessedUser));
                }
            }
            
            // chuck the users and groups together in one JObject
            JObject assessedGppGroupsJson = new JObject();
            // only want to actually output these things if there's anything useful in them.
            assessedGppGroupsJson.Merge(JUtil.GetSafeJProp("GPP Users", assessedUsers));
            assessedGppGroupsJson.Merge(JUtil.GetSafeJProp("GPP Groups", assessedGroups));

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
            assessedUser.Merge(JUtil.GetSafeJProp("Name", gppUser, "@name"));
            assessedUser.Merge(JUtil.GetSafeJProp("User Name", gppUserProps, "@userName"));
            assessedUser.Merge(JUtil.GetSafeJProp("Changed", gppUser, "@changed"));
            assessedUser.Merge(JUtil.GetSafeJProp("Account Disabled", gppUserProps, "@acctDisabled"));
            assessedUser.Merge(JUtil.GetSafeJProp("Password Never Expires", gppUserProps, "@neverExpires"));
            assessedUser.Merge(JUtil.GetSafeJProp("Description", gppUserProps, "@description"));
            assessedUser.Merge(JUtil.GetSafeJProp("Full Name", gppUserProps, "@fullName"));
            assessedUser.Merge(JUtil.GetSafeJProp("New Name", gppUserProps, "@newName"));
            assessedUser.Merge(JUtil.GetSafeJProp("Action", userAction));

            // check for cpasswords
            if (gppUserProps["@cpassword"] != null)
            {
                string cpassword = gppUserProps["@cpassword"].ToString();
                if (cpassword.Length > 0)
                {
                    string decryptedCpassword = Util.DecryptCpassword(cpassword);
                    // if we find one, that's super interesting.
                    assessedUser.Merge(JUtil.GetSafeJProp("Cpassword", cpassword));
                    assessedUser.Merge(JUtil.GetSafeJProp("Decrypted Password", decryptedCpassword));
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
            assessedGroup.Merge(JUtil.GetSafeJProp("Name", gppGroup, "@name"));
            //TODO if the name is an interesting group, make the finding more interesting.
            assessedGroup.Merge(JUtil.GetSafeJProp("Changed", gppGroup, "@changed"));
            assessedGroup.Merge(JUtil.GetSafeJProp("Description", gppGroup, "@description"));
            assessedGroup.Merge(JUtil.GetSafeJProp("New Name", gppGroupProps, "@newName"));
            assessedGroup.Merge(JUtil.GetSafeJProp("Delete All Users", gppGroupProps, "@deleteAllUsers"));
            assessedGroup.Merge(JUtil.GetSafeJProp("Delete All Groups", gppGroupProps, "@deleteAllGroups"));
            assessedGroup.Merge(JUtil.GetSafeJProp("Remove Accounts", gppGroupProps, "@removeAccounts"));
            assessedGroup.Merge(JUtil.GetSafeJProp("Action", groupAction));

            JArray gppGroupMemberJArray = new JArray();

            // this is kind of pants
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

            // strings are good.
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

            assessedGroup.Merge(JUtil.GetSafeJProp("Members", membersString));

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
                {JUtil.GetSafeJProp("Name", member, "@name")},
                {JUtil.GetSafeJProp("Action", member, "@action")}
            };
            string memberSid = JUtil.GetSafeString(member, "@sid");
            assessedMember.Merge(JUtil.GetSafeJProp("SID", memberSid));

            string resolvedSid = LDAPstuff.GetUserFromSid(memberSid);
            assessedMember.Add("DisplayName", resolvedSid);

            return assessedMember;
        }
    }
}