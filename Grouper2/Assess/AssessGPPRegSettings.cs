using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    public class AssessGPPRegSettings
    {
            public static JObject GetAssessedRegSettings(JObject GPPRegSettings)
            {
                JProperty GPPRegSettingsProp = new JProperty("RegSettings", GPPRegSettings["Registry"]);
                JObject AssessedGPPRegSettings = new JObject(GPPRegSettingsProp);
                return AssessedGPPRegSettings;
                //Utility.DebugWrite("GPP is about RegistrySettings");
                //Console.WriteLine(GPPRegSettings["Registry"]);
            }
    }
}
