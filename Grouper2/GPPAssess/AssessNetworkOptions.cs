using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNetworkOptions(JObject gppCategory)
        {
            int interestLevel = 0;
            JProperty gppNetworkOptionsProp = new JProperty("DUN", gppCategory["DUN"]);
            JObject assessedGppNetworkOptions = new JObject(gppNetworkOptionsProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppNetworkOptions = new JObject();
            }

            return assessedGppNetworkOptions;
        }
    }
}