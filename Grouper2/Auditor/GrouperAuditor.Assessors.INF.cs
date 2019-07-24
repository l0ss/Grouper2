using System;
using System.Collections.Generic;
using System.Linq;
using Grouper2.Host;
using Grouper2.Host.DcConnection;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        public Finding Audit(InfSysvolFile file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));

            //parse the inf file into a manageable format
            JObject parsedInfFile;
            try
            {
                parsedInfFile = file.ParseAsJsonObject();
            }
            catch (UnauthorizedAccessException e)
            {
                Log.Degub("unable to convert the file into a json object", e, file);
                return null;
            }

            //send the inf file to be assessed
            AuditedGptmpl pinf;
            try
            {
                pinf = AuditGptmpl(parsedInfFile);
            }
            catch (Exception e)
            {
                Log.Degub("unable to run AuditGptmpl() on the parsed content", e, parsedInfFile);
                return null;
            }
            
            return pinf;
        }

        private AuditedGptmpl AuditGptmpl(JObject infToAssess)
        {
            if (infToAssess == null) 
                throw new ArgumentNullException(nameof(infToAssess));
            // exit early if the file didn't read anty values in
            if (!infToAssess.HasValues)
            {
                return null;
            }

            // create a finding to put all our results into
            AuditedGptmpl ret = new AuditedGptmpl();

            // an array for GPTmpl headings to ignore.
            List<string> knownKeys = new List<string>
            {
                "Unicode",
                "Version"
            };

            // go through each category we care about and look for goodies.
            ///////////////////////////////////////////////////////////////
            // Privilege Rights
            ///////////////////////////////////////////////////////////////
            JToken privRights = infToAssess["Privilege Rights"];

            if (privRights != null)
            {
                List<AuditedPrivRight> privRightsResults = AssessPrivRights(privRights);
                if (privRightsResults != null)
                {
                    ret.AuditedPrivRight = privRightsResults;
                }

                // mark this as seen
                knownKeys.Add("Privilege Rights");
            }

            ///////////////////////////////////////////////////////////////
            // Registry Values
            ///////////////////////////////////////////////////////////////
            JToken regValues = infToAssess["Registry Values"];

            if (regValues != null)
            {
                List<AuditedRegistryValues> matchedRegValues = AssessRegValues(regValues);
                if (matchedRegValues != null)
                {
                    ret.RegistryValues = matchedRegValues;
                }

                knownKeys.Add("Registry Values");
            }

            ///////////////////////////////////////////////////////////////
            // System Access
            ///////////////////////////////////////////////////////////////
            JToken sysAccess = infToAssess["System Access"];
            if (sysAccess != null)
            {
                AuditedSystemAccess assessedSysAccess = AssessSysAccess(sysAccess);
                if (assessedSysAccess != null)
                {
                    ret.SystemAccess = assessedSysAccess;
                }

                knownKeys.Add("System Access");
            }

            ///////////////////////////////////////////////////////////////
            // Kerberos Policy
            ///////////////////////////////////////////////////////////////
            JToken kerbPolicy = infToAssess["Kerberos Policy"];
            if (kerbPolicy != null)
            {
                AuditedKerbPolicy assessedKerbPol = AssessKerbPolicy(kerbPolicy);
                if (assessedKerbPol != null)
                {
                    ret.KerbPolicy = assessedKerbPol;
                }

                knownKeys.Add("Kerberos Policy");
            }

            ///////////////////////////////////////////////////////////////
            // Registry Keys
            ///////////////////////////////////////////////////////////////
            JToken regKeys = infToAssess["Registry Keys"];
            if (regKeys != null)
            {
                Dictionary<string, AuditedRegistryKeys> assessedRegKeys = AssessRegKeys(regKeys);
                if (assessedRegKeys != null)
                {
                    ret.RegistryKeys = assessedRegKeys;
                }

                knownKeys.Add("Registry Keys");
            }

            ///////////////////////////////////////////////////////////////
            // Group Membership
            ///////////////////////////////////////////////////////////////
            JToken grpMembership = infToAssess["Group Membership"];
            if (grpMembership != null)
            {
                JObject assessedgrpMembership = AssessGroupMembership(grpMembership);
                if (assessedgrpMembership != null)
                {
                    ret.GroupMemberships = assessedgrpMembership;
                }

                knownKeys.Add("Group Membership");
            }

            ///////////////////////////////////////////////////////////////
            // Service General Setting
            ///////////////////////////////////////////////////////////////
            JToken svcGenSetting = infToAssess["Service General Setting"];
            if (svcGenSetting != null)
            {
                Dictionary<string, AuditedServiceGenSetting> assessedSvcGenSetting = AssessServiceGenSetting(svcGenSetting);
                if (assessedSvcGenSetting != null && assessedSvcGenSetting.Count > 0)
                {
                    ret.ServiceGenSettings = assessedSvcGenSetting;
                }

                knownKeys.Add("Service General Setting");
            }

            //catch any stuff that falls through the cracks, i.e. look for headings on sections that we aren't parsing.
            List<string> headingsInInf = new List<string>();
            foreach (JProperty section in infToAssess.Children<JProperty>())
            {
                string sectionName = section.Name;
                headingsInInf.Add(sectionName);
            }

            IEnumerable<string> slippedThrough = headingsInInf.Except(knownKeys);
            if (slippedThrough.Any() && this.DebugMode)
            {
                Console.WriteLine("We didn't parse any of these sections:");
                foreach (string unparsedHeader in slippedThrough)
                {
                    Console.WriteLine(unparsedHeader);
                }
            }

            //dont mangle our json thing into a jobject and return it
            return ret;
        }
    }
}