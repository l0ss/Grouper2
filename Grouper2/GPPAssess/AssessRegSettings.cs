using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedRegistrySettings(JObject gppCategory)
        {
            // I both hate and fear this part of the thing. I want it to go away.

            JObject assessedGppRegSettingsOut = new JObject();

            if (gppCategory["Collection"] != null)
            {
                JObject assessedGppRegCollections = GetAssessedRegistryCollection(gppCategory["Collection"]);
                if (assessedGppRegCollections != null)
                {
                    assessedGppRegSettingsOut.Add(assessedGppRegCollections);
                }
            }

            if (gppCategory["Registry"] != null)
            {
                JObject assessedGppRegSettings = GetAssessedRegistrySetting(gppCategory["Registry"]);
                if (assessedGppRegSettings != null)
                {
                    assessedGppRegSettings.Add(assessedGppRegSettings);
                }
            }

            if (assessedGppRegSettingsOut.HasValues)
            {
                return assessedGppRegSettingsOut;
            }
            else
            {
                return null;
            }
        }

        private JObject GetAssessedRegistryCollection(JToken gppRegCollection)
        {
            //Utility.DebugWrite("Collection");
            //Utility.DebugWrite(gppRegCollection.ToString());
            return null;
        }

        private JObject GetAssessedRegistrySetting(JToken gppRegSetting)
        {
            //Utility.DebugWrite("Setting");
            //Utility.DebugWrite(gppRegSetting.ToString());
            return null;
        }
    }
}