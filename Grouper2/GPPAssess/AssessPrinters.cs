using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedPrinters(JObject gppCategory)
        {/*
            {
            THIS IS VERY WEIRD I CANT MAKE THIS HAVE A CPASSWORD VALUE EVEN ON 2008

                "@clsid": "{9A5E9697-9095-436d-A0EE-4D128FDFBCE5}",
                "@name": "printer",
                "@status": "printer",
                "@image": "2",
                "@changed": "2019-01-15 12:55:10",
                "@uid": "{9DB15FAD-0D44-4D79-9349-E7E68C78A051}",
                "Properties": {
                    "@action": "U",
                    "@comment": "",
                    "@path": "\\\\thing.thing\\printer",
                    "@location": "",
                    "@default": "0",
                    "@skipLocal": "0",
                    "@deleteAll": "0",
                    "@persistent": "0",
                    "@deleteMaps": "0",
                    "@port": ""
                }
            }*/

            //Utility.DebugWrite("\nSharedPrinter");
            //Utility.DebugWrite(gppCategory["SharedPrinter"].ToString());
            // dont forget cpasswords
            int interestLevel = 0;
            JProperty gppSharedPrintersProp = new JProperty("SharedPrinter", gppCategory["SharedPrinter"]);
            JObject assessedGppSharedPrinters = new JObject(gppSharedPrintersProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppSharedPrinters = new JObject();
            }

            return assessedGppSharedPrinters;
        }
    }
}