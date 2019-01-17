using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedEnvironmentVariables(JObject gppCategory)
        {

            int interestLevel = 1;

            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                return null;
            }

            JObject assessedGppEvs = new JObject();

            if (gppCategory["EnvironmentVariable"] is JArray)
            {
                foreach (JToken gppEv in gppCategory["EnvironmentVariable"])
                {
                    JObject assessedGppEv = AssessGppEv(gppEv);
                    string uid = assessedGppEv["UID"].ToString();
                    assessedGppEvs.Add(uid, assessedGppEv);
                }
            }
            else
            {
                JObject assessedGppEv = AssessGppEv(gppCategory["EnvironmentVariable"]);
                string uid = assessedGppEv["UID"].ToString();
                assessedGppEv["UID"].Remove();
                assessedGppEvs.Add(uid, assessedGppEv);
            }

            return assessedGppEvs;
        }

        static JObject AssessGppEv(JToken gppEv)
        {
            JObject AssessedGppEv = new JObject();
            
            AssessedGppEv.Add("Name", Utility.GetSafeString(gppEv, "@name"));
            AssessedGppEv.Add("Status", Utility.GetSafeString(gppEv, "@status"));
            AssessedGppEv.Add("Changed", Utility.GetSafeString(gppEv, "@changed"));
            AssessedGppEv.Add("Action", Utility.GetActionString(gppEv["Properties"]["@action"].ToString()));
            AssessedGppEv.Add("UID", Utility.GetSafeString(gppEv, "@uid"));
            
            return AssessedGppEv;
        }
    }
}