using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    public class AssessGPPNetShares
    {
            public static JObject GetAssessedNetShares(JObject GPPNetShares)
            {
                JProperty GPPNetSharesProp = new JProperty("NetShare", GPPNetShares["NetShare"]);
                JObject AssessedGPPNetShares = new JObject(GPPNetSharesProp);
                return AssessedGPPNetShares;
                //Utility.DebugWrite("GPP is about Network Shares");
                //Console.WriteLine(GPPNetShares["NetShare"]);
            }
    }
}
