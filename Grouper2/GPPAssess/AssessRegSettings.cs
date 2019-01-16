using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedRegistrySettings(JObject gppCategory)
        {
            int interestLevel = 0;
            if (gppCategory["Collection"] != null)
            {
                JProperty gppRegSettingsProp = new JProperty("RegSettingsColl", gppCategory["Collection"]);
                JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
                if (interestLevel < GlobalVar.IntLevelToShow)
                {
                    assessedGppRegSettings = new JObject();
                }

                return assessedGppRegSettings;
            }

            if (gppCategory["Registry"] != null)
            {
                JProperty gppRegSettingsProp = new JProperty("RegSettingsReg", gppCategory["Registry"]);
                JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
                if (interestLevel < GlobalVar.IntLevelToShow)
                {
                    assessedGppRegSettings = new JObject();
                }

                return assessedGppRegSettings;
            }

            if (gppCategory["RegistrySettings"] != null)
            {
                JProperty gppRegSettingsProp = new JProperty("RegSettingsRegSet", gppCategory["RegistrySettings"]);
                JObject assessedGppRegSettings = new JObject(gppRegSettingsProp);
                if (interestLevel < GlobalVar.IntLevelToShow)
                {
                    assessedGppRegSettings = new JObject();
                }

                return assessedGppRegSettings;
            }
            else
            {
                Utility.DebugWrite("Something fucked up.");
                Utility.DebugWrite(gppCategory.ToString());
                return null;
            }
        }
    }
}