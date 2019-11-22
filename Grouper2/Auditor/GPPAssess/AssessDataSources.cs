using System;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(DataSources file)
        {
            // null inbound data check
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            
            // try to get the not xml but almost stuff
            JObject gppCategory = file.JankyXmlStuff["DataSources"] as JObject;
            if (gppCategory == null)
                return null; // we know some stuff won't work, so just continue on happily

            AuditedDataSource assessed = new AuditedDataSource();
            if (gppCategory["DataSource"] is JArray)
            {
                foreach (JToken gppDataSource in gppCategory["DataSource"])
                {
                    // attempt to assess the data source entry
                    AuditedDataSourceEntry assessedGppDataSource = AssessGppDataSource(gppDataSource);
                    if (assessedGppDataSource == null) 
                        continue;
                    
                    // add the interest and contents to the return object
                    assessed.TryBumpInterest(assessedGppDataSource);
                    assessed.Contents.Add(assessedGppDataSource);
                }
            }
            else
            {
                // attempt to assess the data source entry
                AuditedDataSourceEntry assessedGppDataSource = AssessGppDataSource(gppCategory["DataSource"]);
                // if there was something back
                if (assessedGppDataSource != null)
                {
                    // add the interest and contents to the return object
                    assessed.TryBumpInterest(assessedGppDataSource);
                    assessed.Contents.Add(assessedGppDataSource);
                }
            }

            // if there were actual findings added, return
            if (assessed.Contents.Count > 0) return assessed;
            // otherwise null
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="gppDataSource">Cthulu</param>
        /// <returns>He Comes</returns>
        private AuditedDataSourceEntry AssessGppDataSource(JToken gppDataSource)
        {
            int interestLevel = 1;
            string gppDataSourceUid = JUtil.GetSafeString(gppDataSource, "@uid");
            string gppDataSourceName = JUtil.GetSafeString(gppDataSource, "@name");
            string gppDataSourceChanged = JUtil.GetSafeString(gppDataSource, "@changed");

            JToken gppDataSourceProps = gppDataSource["Properties"];
            string gppDataSourceAction = JUtil.GetActionString(gppDataSourceProps["@action"].ToString());
            string gppDataSourceUserName = JUtil.GetSafeString(gppDataSourceProps, "@username");
            string gppDataSourcecPassword = JUtil.GetSafeString(gppDataSourceProps, "@cpassword");
            string gppDataSourcePassword = "";
            if (gppDataSourcecPassword.Length > 0)
            {
                gppDataSourcePassword = Util.DecryptCpassword(gppDataSourcecPassword);
                interestLevel = 10;
            }

            string gppDataSourceDsn = JUtil.GetSafeString(gppDataSourceProps, "@dsn");
            string gppDataSourceDriver = JUtil.GetSafeString(gppDataSourceProps, "@driver");
            string gppDataSourceDescription = JUtil.GetSafeString(gppDataSourceProps, "@description");
            JToken gppDataSourceAttributes = gppDataSourceProps["Attributes"];

            if (interestLevel >= InterestLevel)
            {
                AuditedDataSourceEntry ret = new AuditedDataSourceEntry
                {
                    Interest = interestLevel,
                    Uid = gppDataSourceUid,
                    Name = gppDataSourceName,
                    Changed = gppDataSourceChanged,
                    Action = gppDataSourceAction,
                    Username = gppDataSourceUserName,
                    Dsn = gppDataSourceDsn,
                    Driver = gppDataSourceDriver,
                    Description = gppDataSourceDescription
                };

                if (gppDataSourcecPassword.Length > 0)
                {
                    ret.CPassword = gppDataSourcecPassword;
                    ret.CPasswordDecrypted = gppDataSourcePassword;
                }

                if (gppDataSourceAttributes != null && gppDataSourceAttributes.HasValues)
                    ret.Attributes = gppDataSourceAttributes;

                return ret;
            }

            return null;
        }
    }
}