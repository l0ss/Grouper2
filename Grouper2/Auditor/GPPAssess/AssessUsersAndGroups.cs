using System;
using System.Collections.Generic;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(Groups file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedGroups(file.JankyXmlStuff);
        }
        private AuditedGppXmlGroups GetAssessedGroups(JObject gppCategory)
        {
            AuditedGppXmlGroups ret = new AuditedGppXmlGroups();
            if (gppCategory["Groups"] != null)
            {
                JToken gppGroups = gppCategory["Groups"]; 

                // first groups
                if (gppGroups["Group"] != null)
                {
                    if (gppGroups["Group"] is JArray)
                    {
                        foreach (JObject gppGroup in gppGroups["Group"])
                        {
                            AuditedGppXmlGroupsGroup assessedGroup = GetAssessedGroup(gppGroup);
                            if (assessedGroup != null)
                            {
                                ret.Groups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                            }
                        }
                    }
                    else
                    {
                        JObject gppGroup = (JObject) JToken.FromObject(gppGroups["Group"]);
                        AuditedGppXmlGroupsGroup assessedGroup = GetAssessedGroup(gppGroup);
                        if (assessedGroup != null)
                        {
                            ret.Groups.Add(gppGroup["@uid"].ToString(), assessedGroup);
                        }
                    }
                }

                // now users
                if (gppGroups["User"] != null)
                {
                    if (gppGroups["User"] is JArray)
                    {
                        foreach (JToken jToken in gppGroups["User"])
                        {
                            JObject gppUser = (JObject) jToken;
                            AuditedGppXmlGroupsUser assessedUser = GetAssessedUser(gppUser);
                            if (assessedUser != null)
                            {
                                ret.Users.Add(gppUser["@uid"].ToString(), assessedUser);
                            }
                        }
                    }
                    else
                    {
                        JObject gppUser = (JObject) JToken.FromObject(gppGroups["User"]);
                        AuditedGppXmlGroupsUser assessedUser = GetAssessedUser(gppUser);
                        if (assessedUser != null)
                        {
                            ret.Users.Add(gppUser["@uid"].ToString(), assessedUser);
                        }
                    }
                }
            }

            return ret;
        }

        private AuditedGppXmlGroupsUser GetAssessedUser(JObject gppUser)
        {
            JToken gppUserProps = gppUser["Properties"];
            //set base interest level
            AuditedGppXmlGroupsUser assessedUser = new AuditedGppXmlGroupsUser()
            {
                Interest = 3,
                Name = JUtil.GetSafeString(gppUser, "@name"),
                Changed = JUtil.GetSafeString(gppUser, "@changed"),
                CPassword = gppUserProps["@cpassword"].ToString(),
                Username = JUtil.GetSafeString(gppUserProps, "@userName"),
                AccountDisabled = JUtil.GetSafeString(gppUserProps, "@acctDisabled"),
                PasswordNeverExpires = JUtil.GetSafeString(gppUserProps, "@neverExpires"),
                Description = JUtil.GetSafeString(gppUserProps, "@description"),
                FullName = JUtil.GetSafeString(gppUserProps, "@fullName"),
                NewName = JUtil.GetSafeString(gppUserProps, "@newName"),
                Action = JUtil.GetActionString(gppUserProps["@action"].ToString())
            };

            if (!string.IsNullOrWhiteSpace(assessedUser.CPassword))
            {
                assessedUser.CPasswordDecrypted = Util.DecryptCpassword(assessedUser.CPassword);
                assessedUser.TryBumpInterest(10);
            }

            // if it's too boring to be worth showing, return an empty jobj.
            return assessedUser.Interest < this.InterestLevel 
                ? null 
                : assessedUser;
            //Utility.Output.DebugWrite(assessedUser.ToString());
        }

        private AuditedGppXmlGroupsGroup GetAssessedGroup(JObject gppGroup)
        {
            // int interestLevel = 3; this is added to the return object below
            // this will facilitate the below
            //TODO if the name is an interesting group, make the finding more interesting.
            
            JToken gppGroupProps = gppGroup["Properties"];
            
            // extract member data
            List<AuditedGppXmlGroupsGroupMember> gppGroupMemberJArray = new List<AuditedGppXmlGroupsGroupMember>();
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
            }

            AuditedGppXmlGroupsGroup ret = new AuditedGppXmlGroupsGroup()
            {
                Interest = 3,
                Name = JUtil.GetSafeString(gppGroup, "@name"),
                Description = JUtil.GetSafeString(gppGroup, "@description"),
                NewName = JUtil.GetSafeString(gppGroupProps, "@newName"),
                DelUsers = JUtil.GetSafeString(gppGroupProps, "@deleteAllUsers"),
                DelGroups = JUtil.GetSafeString(gppGroupProps, "@deleteAllGroups"),
                DelAccounts = JUtil.GetSafeString(gppGroupProps, "@removeAccounts"),
                Action = JUtil.GetActionString(gppGroupProps["@action"].ToString()),
                Members = gppGroupMemberJArray
            };

            // return if something interesting
            return ret.Interest > this.InterestLevel 
                ? ret 
                : null;
        }

        private AuditedGppXmlGroupsGroupMember GetAssessedGroupMember(JToken member)
        {
            // get the basic info
            AuditedGppXmlGroupsGroupMember assessedMember = new AuditedGppXmlGroupsGroupMember()
            {
                Sid = JUtil.GetSafeString(member, "@sid"),
                Name = JUtil.GetSafeString(member, "@name"),
                Action = JUtil.GetSafeString(member, "@action")
            };
            
            // try to get a display name for the sid
            if (!string.IsNullOrEmpty(assessedMember.Sid))
            {
                string resolvedSid = this._netconn.GetUserFromSid(assessedMember.Sid);
                assessedMember.DisplayName = resolvedSid;
            }
            
            // if at least one fo the fields has data, return it, else null
            if (!string.IsNullOrWhiteSpace(assessedMember.Sid) 
                || !string.IsNullOrWhiteSpace(assessedMember.Name) 
                || !string.IsNullOrWhiteSpace(assessedMember.Action)) 
                return assessedMember;
            return null;
        }
    }
}