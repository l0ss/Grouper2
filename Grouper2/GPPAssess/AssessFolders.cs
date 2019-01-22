using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedFolders(JObject gppCategory)
        {
           /* [
            {
                "@clsid": "{07DA02F5-F9CD-4397-A550-4AE21B6B4BD3}",
                "@name": "temp",
                "@status": "temp",
                "@image": "2",
                "@changed": "2018-11-06 05:37:50",
                "@uid": "{961528C8-00D0-4B40-8043-664B855C1C74}",
                "Properties": {
                    "@action": "U",
                    "@path": "C:\\temp\\temp",
                    "@readOnly": "0",
                    "@archive": "1",
                    "@hidden": "0"
                }
            },
            {
                "@clsid": "{07DA02F5-F9CD-4397-A550-4AE21B6B4BD3}",
                "@name": "temp2",
                "@status": "temp2",
                "@image": "2",
                "@changed": "2018-11-13 06:28:43",
                "@uid": "{483A5F50-4C7D-4A2E-B305-79D9C06CA96B}",
                "Properties": {
                    "@action": "U",
                    "@path": "C:\\temp\\temp2",
                    "@readOnly": "0",
                    "@archive": "1",
                    "@hidden": "0"
                }
            }*/

            //Utility.DebugWrite("\nFolder");
            //Utility.DebugWrite(gppCategory["Folder"].ToString());
            int interestLevel = 0;
            JProperty gppFoldersProp = new JProperty("Folder", gppCategory["Folder"]);
            JObject assessedGppFolders = new JObject(gppFoldersProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppFolders = new JObject();
            }

            return assessedGppFolders;
        }
    }
}