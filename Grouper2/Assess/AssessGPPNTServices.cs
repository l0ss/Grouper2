using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    public class AssessGPPNTServices
    {
            public static JObject GetAssessedNTServices(JObject GPPNTServices)
            {
                JProperty NTServiceProp = new JProperty("NTService", GPPNTServices["NTService"]);
                JObject AssessedGPPNTServices = new JObject(NTServiceProp);
                return AssessedGPPNTServices;
                //Utility.DebugWrite("GPP is about NTServices");
                //Console.WriteLine(GPPNTServices["NTService"]);
            }
    }
}
