using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedIniFiles(JObject gppCategory)
        {
            /*
             {
               "@clsid": "{EEFACE84-D3D8-4680-8D4B-BF103E759448}",
               "@name": "propertywhooo",
               "@status": "propertywhooo",
               "@image": "2",
               "@changed": "2018-11-13 06:29:07",
               "@uid": "{1168179B-89D4-464B-B092-7136D0970979}",
               "Properties": {
               "@path": "C:\\temp\\temp.ini",
               "@section": "sectionwhee",
               "@value": "valuewhaaaa",
               "@property": "propertywhooo",
               "@action": "U"
               }
               }
             */

            //Utility.DebugWrite("\nIni");
            //Utility.DebugWrite(gppCategory["Ini"].ToString());
            int interestLevel = 0;
            JProperty gppIniFilesProp = new JProperty("Ini", gppCategory["Ini"]);
            JObject assessedGppIniFiles = new JObject(gppIniFilesProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppIniFiles = new JObject();
            }

            return assessedGppIniFiles;
        }
    }
}