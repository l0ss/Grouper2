using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedDataSources(JObject gppCategory)
        {
            // dont forget cpasswords
            int interestLevel = 0;
            JProperty gppDataSourcesProp = new JProperty("DataSource", gppCategory["DataSource"]);
            JObject assessedGppDataSources = new JObject(gppDataSourcesProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppDataSources = new JObject();
            }

            return assessedGppDataSources;
        }
    }
}