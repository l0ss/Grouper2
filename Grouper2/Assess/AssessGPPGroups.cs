using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Grouper2.Assess
{
    class AssessGPPGroups
    {
        public static JObject GetAssessedGroups(JObject GPPGroups)
        {
            Dictionary<string, Dictionary<string, string>> AssessedGroupsDict = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> AssessedUsersDict = new Dictionary<string, Dictionary<string, string>>();

            foreach (JToken User in GPPGroups["User"])
            {
                // dictionary for results from this specific user.
                Dictionary<string, string> AssessedUserDict = new Dictionary<string, string>
                {
                    { "InterestLevel", "3" }
                };
                // check what the entry is doing to the user and turn it into real word
                string UserAction = User["Properties"]["@action"].ToString();
                switch (UserAction)
                {
                    case "U":
                        UserAction = "Update";
                        break;
                    case "A":
                        UserAction = "Add";
                        break;
                    case "D":
                        UserAction = "Delete";
                        break;
                    default:
                        Console.WriteLine("oh no this is new");
                        break;
                }
                // get the username and a bunch of other details:
                AssessedUserDict.Add("Name", User["@name"].ToString());
                AssessedUserDict.Add("User Name", User["Properties"]["@userName"].ToString());
                AssessedUserDict.Add("DateTime Changed", User["@changed"].ToString());
                AssessedUserDict.Add("Account Disabled", User["Properties"]["@acctDisabled"].ToString());
                AssessedUserDict.Add("Password Never Expires", User["Properties"]["@neverExpires"].ToString());
                AssessedUserDict.Add("Description", User["Properties"]["@description"].ToString());
                AssessedUserDict.Add("Full Name", User["Properties"]["@fullName"].ToString());
                AssessedUserDict.Add("New Name", User["Properties"]["@newName"].ToString());

                // check for cpasswords 
                string cpassword = User["Properties"]["@cpassword"].ToString();
                string DecryptedCpassword = "";
                if (cpassword.Length > 0)
                {
                    DecryptedCpassword = Utility.DecryptCpassword(cpassword);
                    // if we find one, that's super interesting.
                    AssessedUserDict.Add("Cpassword", DecryptedCpassword);
                    AssessedUserDict["InterestLevel"] = "10";
                }
                // add to the output dict with a uid to keep it unique.
                AssessedUsersDict.Add(User["@uid"].ToString(), AssessedUserDict);
            }

            // repeat the process for Groups
            foreach (JToken Group in GPPGroups["Group"])
            {
                //dictionary for results from this specific group
                Dictionary<string, string> AssessedGroupDict = new Dictionary<string, string>();
                string GroupAction = Group["Properties"]["@action"].ToString();
                switch (GroupAction)
                {
                    case "U":
                        break;
                    case "A":
                        break;
                    default:
                        Console.WriteLine("oh no this is new");
                        break;
                }
            }

            // cast our Dictionaries back into JObjects
            JProperty AssessedUsersJson = new JProperty("GPP User settings", JToken.FromObject(AssessedUsersDict));
            JProperty AssessedGroupsJson = new JProperty("GPP Group settings", JToken.FromObject(AssessedGroupsDict));
            // chuck the users and groups together in one JObject
            JObject AssessedGPPGroupsJson = new JObject();
            // only want to actually output these things if there's anything useful in them.
            if (AssessedUsersDict.Count > 0)
            {
                AssessedGPPGroupsJson.Add(AssessedUsersJson);
            }
            if (AssessedGroupsDict.Count > 0) {
                AssessedGPPGroupsJson.Add(AssessedGroupsJson);
            }
            return AssessedGPPGroupsJson;
        }
    }
}
