using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNetworkShareSettings(JObject gppCategory)
        {
            int interestLevel = 0;
            JProperty gppNetSharesProp = new JProperty("NetShare", gppCategory["NetShare"]);
            JObject assessedGppNetShares = new JObject(gppNetSharesProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppNetShares = new JObject();
            }

            return assessedGppNetShares;
        }
    }
}