using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        // ReSharper disable once UnusedMember.Local
        private JObject GetAssessedDataSources(JObject gppCategory)
        {
            JObject assessedGppDataSources = new JObject();
            if (gppCategory["DataSource"] is JArray)
            {
                foreach (JToken gppDataSource in gppCategory["DataSource"])
                {
                    JProperty assessedGppDataSource = AssessGppDataSource(gppDataSource);
                    if (assessedGppDataSource != null)
                    {
                        assessedGppDataSources.Add(assessedGppDataSource);
                    }
                }
            }
            else
            {
                JProperty assessedGppDataSource = AssessGppDataSource(gppCategory["DataSource"]);
                assessedGppDataSources.Add(assessedGppDataSource);
            }

            if (assessedGppDataSources.HasValues)
            {
                return assessedGppDataSources;
            }
            else
            {
                return null;
            }
            
        }
        
        static JProperty AssessGppDataSource(JToken gppDataSource)
        {
            int interestLevel = 1;
            string gppDataSourceUid = Utility.GetSafeString(gppDataSource, "@uid");
            string gppDataSourceName = Utility.GetSafeString(gppDataSource, "@name");
            string gppDataSourceChanged = Utility.GetSafeString(gppDataSource, "@changed");
            
            JToken gppDataSourceProps = gppDataSource["Properties"];
            string gppDataSourceAction = Utility.GetActionString(gppDataSourceProps["@action"].ToString());
            string gppDataSourceUserName = Utility.GetSafeString(gppDataSourceProps, "@username");
            string gppDataSourcecPassword = Utility.GetSafeString(gppDataSourceProps, "@cpassword");
            string gppDataSourcePassword = "";
            if (gppDataSourcecPassword.Length > 0)
            {
                gppDataSourcePassword = Utility.DecryptCpassword(gppDataSourcecPassword);
                interestLevel = 10;
            }

            string gppDataSourceDsn = Utility.GetSafeString(gppDataSourceProps, "@dsn");
            string gppDataSourceDriver = Utility.GetSafeString(gppDataSourceProps, "@driver");
            string gppDataSourceDescription = Utility.GetSafeString(gppDataSourceProps, "@description");
            JToken gppDataSourceAttributes = gppDataSourceProps["Attributes"];

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                JObject assessedGppDataSource = new JObject
                {
                    {"Name", gppDataSourceName},
                    {"Changed", gppDataSourceChanged},
                    {"Action", gppDataSourceAction},
                    {"Username", gppDataSourceUserName}
                };
                if (gppDataSourcecPassword.Length > 0)
                {
                    assessedGppDataSource.Add("cPassword", gppDataSourcecPassword);
                    assessedGppDataSource.Add("Decrypted Password", gppDataSourcePassword);
                }
                assessedGppDataSource.Add("DSN", gppDataSourceDsn);
                assessedGppDataSource.Add("Driver", gppDataSourceDriver);
                assessedGppDataSource.Add("Description", gppDataSourceDescription);
                if ((gppDataSourceAttributes != null) && (gppDataSourceAttributes.HasValues))
                {
                    assessedGppDataSource.Add("Attributes", gppDataSourceAttributes);
                }

                return new JProperty(gppDataSourceUid, assessedGppDataSource);
            }

            return null;
        }
    }
}