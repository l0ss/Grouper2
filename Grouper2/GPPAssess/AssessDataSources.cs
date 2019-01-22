using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedDataSources(JObject gppCategory)
        {
            /*{
                "@clsid": "{5C209626-D820-4d69-8D50-1FACD6214488}",
                "@name": "thing",
                "@image": "0",
                "@changed": "2019-01-15 12:50:18",
                "@uid": "{79D5F174-338C-48D7-A4E6-B46A26D72394}",
                "@userContext": "1",
                "@removePolicy": "0",
                "Properties": {
                    "@action": "C",
                    "@userDSN": "1",
                    "@dsn": "thing",
                    "@driver": "SQL Server",
                    "@description": "hlkjhlkj",
                    "@username": "hgkjhgkjh\\tkuhgk",
                    "@cpassword": "/yIv9hPoIkBuYXZZ7onYlg",
                    "Attributes": {
                        "Attribute": [
                        {
                            "@name": "qwer",
                            "@value": "asdf"
                        },
                        {
                            "@name": "qwer1",
                            "@value": "asdf1"
                        }
                        ]
                    }
                }
            },
            {
                "@clsid": "{5C209626-D820-4d69-8D50-1FACD6214488}",
                "@userContext": "1",
                "@name": "yuio",
                "@image": "0",
                "@changed": "2019-01-15 12:50:50",
                "@uid": "{C0EB588B-8163-4F92-9697-6A0E76B6E93C}",
                "Properties": {
                    "@action": "C",
                    "@userDSN": "0",
                    "@dsn": "yuio",
                    "@driver": "HJKL",
                    "@description": "DGDHJ",
                    "@username": "VCXV",
                    "@cpassword": "/yIv9hPoIkBuYXZZ7onYlg"
                }
            }*/

            //Utility.DebugWrite("\nDataSource");
            //Utility.DebugWrite(gppCategory["DataSource"].ToString());
            // dont forget cpasswords
            int interestLevel = 0;
            JProperty gppDataSourcesProp = new JProperty("DataSource", gppCategory["DataSource"]);
            JObject assessedGppDataSources = new JObject(gppDataSourcesProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppDataSources = new JObject();
            }

            return assessedGppDataSources;
        }
    }
}