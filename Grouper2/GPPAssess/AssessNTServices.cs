using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNTServices(JObject gppCategory)
        {
            // dont forget cpasswords
            //Utility.DebugWrite(gppCategory["NTService"].ToString());
            int interestLevel = 0;
            JProperty ntServiceProp = new JProperty("NTService", gppCategory["NTService"]);
            JObject assessedNtServices = new JObject(ntServiceProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedNtServices = new JObject();
            }

            return assessedNtServices;
        }
    }
}