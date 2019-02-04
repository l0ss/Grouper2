using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNetworkShareSettings(JObject gppCategory)
        {
            JToken gppNetSharesJToken = gppCategory["NetShare"];
            
            JObject assessedGppNetShares = new JObject();
            if (gppNetSharesJToken is JArray)
            {
                foreach (JToken netShare in gppNetSharesJToken)
                {
                    JProperty assessedGppNetShare = GetAssessedNetworkShare(netShare);
                    if (assessedGppNetShare != null)
                    {
                        assessedGppNetShares.Add(assessedGppNetShare);
                    }
                }
            }
            else
            {
                JProperty assessedGppNetShare = GetAssessedNetworkShare(gppNetSharesJToken);
                if (assessedGppNetShare != null)
                {
                    assessedGppNetShares.Add(assessedGppNetShare);
                }
            }

            if (assessedGppNetShares.HasValues)
            {
                return assessedGppNetShares;
            }

            return null;
        }

        private JProperty GetAssessedNetworkShare(JToken netShare)
        {
            int interestLevel = 1;

            JObject assessedGppNetShare = new JObject
            {
                {"Name", Utility.GetSafeString(netShare, "@name")},
                {"Changed", Utility.GetSafeString(netShare, "@changed")},
                {"Action", Utility.GetActionString(netShare["Properties"]["@action"].ToString())}
            };

            if (netShare["Properties"]["@path"] != null)
            {
                assessedGppNetShare.Add("Path", FileSystem.InvestigatePath(netShare["Properties"]["@path"].ToString()));
            }
            assessedGppNetShare.Add("Comment", Utility.GetSafeString(netShare["Properties"], "@comment"));


            if (assessedGppNetShare["Path"]["InterestLevel"] != null)
            {
                interestLevel = interestLevel + int.Parse(assessedGppNetShare["Path"]["InterestLevel"].ToString());
                if (interestLevel >= GlobalVar.IntLevelToShow)
                {
                    return new JProperty(netShare["@uid"].ToString(), assessedGppNetShare);
                }
            }

            return null;
        }
    }
}