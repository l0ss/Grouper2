using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedIniFiles(JObject gppCategory)
        {
            int interestLevel = 0;
            JProperty gppIniFilesProp = new JProperty("Ini", gppCategory["Ini"]);
            JObject assessedGppIniFiles = new JObject(gppIniFilesProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppIniFiles = new JObject();
            }

            return assessedGppIniFiles;
        }
    }
}