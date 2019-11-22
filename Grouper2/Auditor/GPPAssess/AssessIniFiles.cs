using System;
using System.Runtime.CompilerServices;
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
        public Finding Audit(IniFiles file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedIniFiles(file.JankyXmlStuff);
        }
        private AuditedGppXmlIniFiles GetAssessedIniFiles(JObject gppCategory)
        {
            //JObject assessedGppInis = new JObject();
            AuditedGppXmlIniFiles ret = new AuditedGppXmlIniFiles();

            if (gppCategory["IniFiles"]["Ini"] is JArray)
            {
                foreach (JToken gppIni in gppCategory["IniFiles"]["Ini"])
                {
                    AuditedGppXmlIniFilesFile assessedGppIni = AssessGppIni(gppIni);
                    if (assessedGppIni == null)
                    {
                        continue;
                    }

                    try
                    {
                        ret.Inis.Add(assessedGppIni.Uid,assessedGppIni);
                    }
                    catch (ArgumentException e)
                    {
                        Log.Degub($"Unable to add an assessed INI with guid {assessedGppIni.Uid}", e, assessedGppIni);
                    }
                    
                }
            }
            else
            {
                AuditedGppXmlIniFilesFile assessedGppIni = AssessGppIni(gppCategory["IniFiles"]["Ini"]);
                if (assessedGppIni != null)
                {
                    try
                    {
                        ret.Inis.Add(assessedGppIni.Uid,assessedGppIni);
                    }
                    catch (ArgumentException e)
                    {
                        Log.Degub($"Unable to add an assessed INI with guid {assessedGppIni.Uid}", e, assessedGppIni);
                    }
                }
            }

            return ret.Inis.Count > 0 
                ? ret 
                : null;
            
        }
        
        private AuditedGppXmlIniFilesFile AssessGppIni(JToken gppIni)
        {
            if (gppIni == null) 
                throw new ArgumentNullException(nameof(gppIni));
            int interestLevel = 1;
            string gppIniUid = JUtil.GetSafeString(gppIni, "@uid");
            string gppIniName = JUtil.GetSafeString(gppIni, "@name");
            string gppIniChanged = JUtil.GetSafeString(gppIni, "@changed");
            string gppIniStatus = JUtil.GetSafeString(gppIni, "@status");
            
            JToken gppIniProps = gppIni["Properties"];
            string gppIniAction = JUtil.GetActionString(gppIniProps["@action"].ToString());
            AuditedPath gppIniPath = FileSystem.InvestigatePath(JUtil.GetSafeString(gppIniProps, "@path"));
            AuditedString gppIniSection = FileSystem.InvestigateString(JUtil.GetSafeString(gppIniProps, "@section"), this.InterestLevel);
            AuditedString gppIniValue = FileSystem.InvestigateString(JUtil.GetSafeString(gppIniProps, "@value"), this.InterestLevel);
            AuditedString gppIniProperty = FileSystem.InvestigateString(JUtil.GetSafeString(gppIniProps, "@property"), this.InterestLevel);
            
            // check each of our potentially interesting values to see if it raises our overall interest level
            Finding[] valuesWithInterest = {gppIniPath, gppIniSection, gppIniValue, gppIniProperty};
            foreach (Finding val in valuesWithInterest)
            {
                if (val == null) continue;
                if (val.Interest > interestLevel)
                {
                    interestLevel = val.Interest;
                }
            }

            // if there is sufficient interest, return an audit object, else null
            if (interestLevel >= this.InterestLevel)
            {
                return new AuditedGppXmlIniFilesFile()
                {
                    Uid = gppIniUid,
                    Name = gppIniName,
                    Changed = gppIniChanged,
                    PathInfo = gppIniPath,
                    Action = gppIniAction,
                    Status = gppIniStatus,
                    Section = gppIniSection,
                    Value = gppIniValue,
                    Property = gppIniProperty
                };
            }
            return null;
        }
    }
}