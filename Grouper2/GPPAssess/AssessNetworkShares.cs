using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNetworkShareSettings(JObject gppCategory)
        {
            /*
             
               [
               {
               "@clsid": "{2888C5E7-94FC-4739-90AA-2C1536D68BC0}",
               "@image": "2",
               "@name": "desktopshare",
               "@changed": "2018-11-13 06:30:49",
               "@uid": "{62B54811-08D2-4500-B705-0D6662953DC1}",
               "Properties": {
               "@action": "U",
               "@name": "desktopshare",
               "@path": "C:\\Users\\l0ss\\Desktop",
               "@comment": "",
               "@allRegular": "0",
               "@allHidden": "0",
               "@allAdminDrive": "0",
               "@limitUsers": "NO_CHANGE",
               "@abe": "NO_CHANGE"
               }
               },
               {
               "@clsid": "{2888C5E7-94FC-4739-90AA-2C1536D68BC0}",
               "@image": "2",
               "@name": "othershare",
               "@changed": "2018-11-13 06:31:05",
               "@uid": "{6C034FD3-5F05-433A-9D1A-CCE8C9A679A1}",
               "Properties": {
               "@action": "U",
               "@name": "othershare",
               "@path": "C:\\temp\\temp\\",
               "@comment": "derp",
               "@allRegular": "0",
               "@allHidden": "0",
               "@allAdminDrive": "0",
               "@limitUsers": "NO_CHANGE",
               "@abe": "NO_CHANGE"
               }
               }
               ]
             */

            //Utility.DebugWrite("\nNetShare");
            //Utility.DebugWrite(gppCategory["NetShare"].ToString());
            int interestLevel = 0;
            JProperty gppNetSharesProp = new JProperty("NetShare", gppCategory["NetShare"]);
            JObject assessedGppNetShares = new JObject(gppNetSharesProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppNetShares = new JObject();
            }

            return assessedGppNetShares;
        }
    }
}