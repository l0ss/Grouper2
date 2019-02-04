using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace Grouper2
{
    public partial class AssessGpp
    {
        // ReSharper disable once UnusedMember.Local
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
                    JProperty assessedGppEv = AssessGppEv(gppEv);
                    assessedGppEvs.Add(assessedGppEv);
                }
            }
            else
            {
                JProperty assessedGppEv = AssessGppEv(gppCategory["EnvironmentVariable"]);
                assessedGppEvs.Add(assessedGppEv);
            }

            return assessedGppEvs;
        }

        static JProperty AssessGppEv(JToken gppEv)
        {
            JObject assessedGppEv = new JObject();
            assessedGppEv.Add("Name", Utility.GetSafeString(gppEv, "@name"));
            assessedGppEv.Add("Status", Utility.GetSafeString(gppEv, "@status"));
            assessedGppEv.Add("Changed", Utility.GetSafeString(gppEv, "@changed"));
            assessedGppEv.Add("Action", Utility.GetActionString(gppEv["Properties"]["@action"].ToString()));
            return new JProperty(Utility.GetSafeString(gppEv, "@uid"), assessedGppEv);
        }
    }
}