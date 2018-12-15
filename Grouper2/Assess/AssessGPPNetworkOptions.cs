using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    class AssessGPPNetworkOptions
    {
            public static JObject GetAssessedNetworkOptions(JObject GPPNetworkOptions)
            {
                JProperty GPPNetworkOptionsProp = new JProperty("DUN", GPPNetworkOptions["DUN"]);
                JObject AssessedGPPNetworkOptions = new JObject(GPPNetworkOptionsProp);
                return AssessedGPPNetworkOptions;
                //Utility.DebugWrite("GPP is about Network Options");
                //Console.WriteLine(GPPNetworkOptions["DUN"]);
            }
    }
}
