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
        public Finding Audit(NetworkShareSettings file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedNetworkShareSettings(file.JankyXmlStuff);
        }
        private AuditedGppXmlNetShares GetAssessedNetworkShareSettings(JObject gppCategory)
        {
            JToken gppNetSharesJToken = gppCategory["NetworkShareSettings"]["NetShare"];
            AuditedGppXmlNetShares assessedGppNetShares = new AuditedGppXmlNetShares();
            
            //JObject assessedGppNetShares = new JObject();
            if (gppNetSharesJToken is JArray)
            {
                foreach (JToken netShare in gppNetSharesJToken)
                {
                    AuditedGppXmlNetShare assessedGppNetShare = GetAssessedNetworkShare(netShare);
                    if (assessedGppNetShare != null)
                    {
                        assessedGppNetShares.Shares.Add(assessedGppNetShare.Uid,assessedGppNetShare);
                    }
                }
            }
            else
            {
                AuditedGppXmlNetShare assessedGppNetShare = GetAssessedNetworkShare(gppNetSharesJToken);
                if (assessedGppNetShare != null)
                {
                    assessedGppNetShares.Shares.Add(assessedGppNetShare.Uid,assessedGppNetShare);
                }
            }

            if (assessedGppNetShares.Shares.Count > 0)
            {
                return assessedGppNetShares;
            }

            return null;
        }

        private AuditedGppXmlNetShare GetAssessedNetworkShare(JToken netShare)
        {
            if (netShare == null) 
                throw new ArgumentNullException(nameof(netShare));
            // removed InvestigatePath because it's a network share, it's literally always going to be local and therefore not super interesting.
            if (2 >= this.InterestLevel)
            {
                return new AuditedGppXmlNetShare()
                {
                    Uid = netShare["@uid"].ToString(),
                    Name = JUtil.GetSafeString(netShare, "@name"),
                    Changed = JUtil.GetSafeString(netShare, "@changed"),
                    Action = JUtil.GetActionString(netShare["Properties"]["@action"].ToString()),
                    Path = JUtil.GetSafeString(netShare["Properties"], "@path"),
                    Comment = JUtil.GetSafeString(netShare["Properties"], "@comment")
                };
            }
            return null;
        }
    }
}