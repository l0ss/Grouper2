using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedEnvironmentVariables(JObject gppCategory)
        {
            int interestLevel = 0;
            JProperty gppEVProp = new JProperty("EnvironmentVariable", gppCategory["EnvironmentVariable"]);
            JObject assessedGppEVs = new JObject(gppEVProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppEVs = new JObject();
            }

            return assessedGppEVs;
        }
    }
}