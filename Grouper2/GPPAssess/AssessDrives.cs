using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedDrives(JObject gppCategory)
        {
            // dont forget cpasswords
            int interestLevel = 0;
            JProperty gppDriveProp = new JProperty("Drive", gppCategory["Drive"]);
            JObject assessedGppDrives = new JObject(gppDriveProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppDrives = new JObject();
            }

            return assessedGppDrives;
        }
    }
}