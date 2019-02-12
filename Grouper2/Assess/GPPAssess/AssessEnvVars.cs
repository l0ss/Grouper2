using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
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
            JObject assessedGppEv = new JObject
            {
                {"Name", JUtil.GetSafeString(gppEv, "@name")},
                {"Status", JUtil.GetSafeString(gppEv, "@status")},
                {"Changed", JUtil.GetSafeString(gppEv, "@changed")},
                {"Action", JUtil.GetActionString(gppEv["Properties"]["@action"].ToString())}
            };
            return new JProperty(JUtil.GetSafeString(gppEv, "@uid"), assessedGppEv);
        }
    }
}