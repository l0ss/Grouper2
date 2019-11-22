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
        public Finding Audit(NtServices file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedNtServices(file.JankyXmlStuff);
        }
        private AuditedGppXmlNtServices GetAssessedNtServices(JObject gppCategory)
        {
            AuditedGppXmlNtServices assessedGppNtServices = new AuditedGppXmlNtServices();

            if (gppCategory["NTServices"]["NTService"] is JArray)
            {
                foreach (JToken gppNtService in gppCategory["NTServices"]["NTService"])
                {
                    AuditedGppXmlNtService assessedGppNtService;
                    try
                    {
                        assessedGppNtService = AssessGppNtService(gppNtService);
                    }
                    catch (Exception e)
                    {
                        Log.Degub("Unable to assess an nt service", e, this);
                        continue;
                    }
                    
                    if (assessedGppNtService != null)
                    {
                        try
                        {
                            assessedGppNtServices.Services.Add(assessedGppNtService.Uid,assessedGppNtService);
                        }
                        catch (Exception e)
                        {
                            Log.Degub($"unable to add a service to the output with uid {assessedGppNtService.Uid}", e, this);
                            continue;
                        }
                        
                    }
                }
            }
            else
            {
                AuditedGppXmlNtService assessedGppNtService;
                try
                {
                    assessedGppNtService = AssessGppNtService(gppCategory["NTServices"]["NTService"]);
                }
                catch (Exception e)
                {
                    Log.Degub("Unable to assess an nt service", e, this);
                    return null;
                }
                
                if (assessedGppNtService != null)
                {
                    try
                    {
                        assessedGppNtServices.Services.Add(assessedGppNtService.Uid,assessedGppNtService);
                    }
                    catch (Exception e)
                    {
                        Log.Degub($"unable to add a service to the output with uid {assessedGppNtService.Uid}", e, this);
                        return null;
                    }
                }
            }

            // only return if we have list entries
            return assessedGppNtServices.Services.Count > 0 
                ? assessedGppNtServices 
                : null;
        }

        private AuditedGppXmlNtService AssessGppNtService(JToken gppNtService)
        {
            if (gppNtService == null) 
                throw new ArgumentNullException(nameof(gppNtService));
            // only get the stuff we need to determine whether to exit early to save on string ops
            AuditedGppXmlNtService ret = new AuditedGppXmlNtService()
            {
                Interest = 1,
                CPass = JUtil.GetSafeString(gppNtService["Properties"], "@cpassword"),
                Program = FileSystem.InvestigatePath(JUtil.GetSafeString(gppNtService["Properties"], "@program")),
                Args = FileSystem.InvestigateString(JUtil.GetSafeString(gppNtService["Properties"], "@args"), this.InterestLevel)
            };
            
            if (!string.IsNullOrWhiteSpace(ret.CPass))
            {
                ret.Username = JUtil.GetSafeString(gppNtService["Properties"], "@accountName");
                ret.CPassDecrypted = Util.DecryptCpassword(ret.CPass);
                ret.Interest = 10;
            }
            else ret.CPass = null;

            // attempt to adjust the interest if we got something good
            // use the value to determine whether to exit early
            ret.TryBumpInterest(ret.Program);
            ret.TryBumpInterest(ret.Args);
            if (ret.Interest < this.InterestLevel) return null;
            
            // we didn't exit, so let's do this
            // .......
            // OMFG, WTAF 
            ret.Uid = JUtil.GetSafeString(gppNtService, "@uid");
            ret.Changed = JUtil.GetSafeString(gppNtService, "@changed");
            ret.Name = JUtil.GetSafeString(gppNtService, "@name");
            ret.ServiceName = JUtil.GetSafeString(gppNtService["Properties"], "@serviceName");
            ret.Timeout = JUtil.GetSafeString(gppNtService["Properties"], "@timeout");
            ret.StartupType = JUtil.GetSafeString(gppNtService["Properties"], "@startupType");
            ret.ActionFailure = JUtil.GetSafeString(gppNtService["Properties"], "@firstFailure");
            ret.ActionFailure2 = JUtil.GetSafeString(gppNtService["Properties"], "@secondFailure");
            ret.ActionFailure3 = JUtil.GetSafeString(gppNtService["Properties"], "@thirdFailure");
            return ret;
        }
    }
}

