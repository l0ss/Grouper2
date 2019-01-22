using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNTServices(JObject gppCategory)
        {
            /*
             [
               {
               "@clsid": "{AB6F0B67-341F-4e51-92F9-005FBFBA1A43}",
               "@name": "derp",
               "@image": "3",
               "@changed": "2018-11-06 05:41:27",
               "@uid": "{C870768C-AEB5-48EC-A0A0-F74629602675}",
               "Properties": {
               "@startupType": "MANUAL",
               "@serviceName": "derp",
               "@serviceAction": "STOP",
               "@timeout": "30"
               }
               },
               {
               "@clsid": "{AB6F0B67-341F-4e51-92F9-005FBFBA1A43}",
               "@name": "flerp",
               "@image": "4",
               "@changed": "2018-11-13 06:36:53",
               "@uid": "{D5118C2D-EB5C-47C0-8979-9F1EF52DA726}",
               "@userContext": "0",
               "@removePolicy": "0",
               "Properties": {
               "@startupType": "DISABLED",
               "@serviceName": "flerp",
               "@serviceAction": "STOP",
               "@timeout": "30",
               "@accountName": "LocalSystem",
               "@interact": "1",
               "@firstFailure": "RUNCMD",
               "@resetFailCountDelay": "0",
               "@program": "C:\\thing\\thing\\fuck",
               "@args": "-password ispasswordlol",
               "@append": "1"
               }
               }
               ]
             */

            //Utility.DebugWrite("\nNTService");
            //Utility.DebugWrite(gppCategory["NTService"].ToString());
            // dont forget cpasswords
            Utility.DebugWrite(gppCategory["NTService"].ToString());
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