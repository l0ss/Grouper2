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
        public Finding Audit(EnvironmentVariables file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return this.GetAssessedEnvironmentVariables(file.JankyXmlStuff);
        }
        private AuditedGppXmlEnvVars GetAssessedEnvironmentVariables(JObject gppCategory)
        {
            AuditedGppXmlEnvVars assessedGppEvs = new AuditedGppXmlEnvVars()
            {
                Interest = 1
            };
            
            // return early if possible
            if (assessedGppEvs.Interest < this.InterestLevel)
                return null;



            if (gppCategory["EnvironmentVariables"]["EnvironmentVariable"] is JArray)
            {
                foreach (JToken gppEv in gppCategory["EnvironmentVariables"]["EnvironmentVariable"])
                {
                    AuditedGppXmlEnvVarsVar assessedGppEv = AssessGppEv(gppEv);
                    if (assessedGppEv != null)
                    {
                        assessedGppEvs.Vars.Add(assessedGppEv.Uid,assessedGppEv);
                    }
                }
            }
            else
            {
                AuditedGppXmlEnvVarsVar assessedGppEv = AssessGppEv(gppCategory["EnvironmentVariables"]["EnvironmentVariable"]);
                if (assessedGppEv != null)
                {
                    assessedGppEvs.Vars.Add(assessedGppEv.Uid,assessedGppEv);
                }
            }

            return assessedGppEvs;
        }

        private AuditedGppXmlEnvVarsVar AssessGppEv(JToken gppEv)
        {
            if (gppEv == null)
                return null;
            var props = gppEv["Properties"];
            return new AuditedGppXmlEnvVarsVar()
            {
                Uid = JUtil.GetSafeString(gppEv, "@uid"),
                Name = JUtil.GetSafeString(gppEv, "@name"),
                Status = JUtil.GetSafeString(gppEv, "@status"),
                Changed = JUtil.GetSafeString(gppEv, "@changed"),
                Action = JUtil.GetActionString(gppEv["Properties"]["@action"].ToString())
            };
        }
    }
}