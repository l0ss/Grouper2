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
        public Finding Audit(NetworkOptions file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            try
            {
                JObject cruft = file.JankyXmlStuff;
                return GetAssessedNetworkOptions(cruft);
            }
            catch (Exception e)
            {
                Log.Degub($"unable to mangle into json and do a simple thing", e, file);
                throw;
            }
        }
        private AuditedGppXmlNetworkOptions GetAssessedNetworkOptions(JObject gppCategory)
        {
            /*
             DUN
               2/5 GPOs processed. 40% complete.
               {
               "@clsid": "{9B0D030D-9396-49c1-8DEF-08B35B5BB79E}",
               "@name": "sdf",
               "@userContext": "1",
               "@image": "2",
               "@changed": "2018-11-06 05:39:24",
               "@uid": "{A3F6372C-7FCD-4487-A145-BF2236E1D2DD}",
               "Properties": {
               "@action": "U",
               "@user": "0",
               "@name": "sdf",
               "@phoneNumber": "5346745"
               }
             */

            //Utility.Output.DebugWrite("\nDUN");
            //Utility.Output.DebugWrite(gppCategory["DUN"].ToString());
            //int interestLevel = 0;
//            JProperty gppNetworkOptionsProp = new JProperty("DUN", gppCategory["DUN"]);
//            JObject assessedGppNetworkOptions = new JObject(gppNetworkOptionsProp);
            if (0 > this.InterestLevel)
            {
                return new AuditedGppXmlNetworkOptions()
                {
                    Dun = new JObject(gppCategory["DUN"])
                };
            }
            return null;
        }
    }
}