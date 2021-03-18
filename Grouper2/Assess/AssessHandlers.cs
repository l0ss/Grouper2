using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Grouper2
{
    class AssessHandlers
    {
        // Assesses the contents of a registry.pol
        public static JObject AssessRegistryPol(JObject registryPolToAssess)
        {
            Dictionary<string, JObject> assessedRegistryPol = new Dictionary<string, JObject>();

            if (registryPolToAssess != null)
            {
                JObject matchedRegValues = InfAssess.AssessInf.AssessRegValues(registryPolToAssess);
                if (matchedRegValues != null)
                {
                    assessedRegistryPol.Add("Registry Values", matchedRegValues);
                }
            }

            JObject assessedRegistryPolJson = (JObject)JToken.FromObject(assessedRegistryPol);
            return assessedRegistryPolJson;
        }

        // Assesses the contents of a GPTmpl
        public static JObject AssessGptmpl(JObject infToAssess)
        {
            // create a dict to put all our results into
            Dictionary<string, JObject> assessedGpTmpl = new Dictionary<string, JObject>();

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
                    JObject privRightsResults = InfAssess.AssessInf.AssessPrivRights(privRights);
                    if (privRightsResults.Count > 0)
                    {
                        assessedGpTmpl.Add("Privilege Rights", privRightsResults);
                    }
                    knownKeys.Add("Privilege Rights");
                }
            ///////////////////////////////////////////////////////////////
            // Registry Values
            ///////////////////////////////////////////////////////////////
            JToken regValues = infToAssess["Registry Values"];

            if (regValues != null)
            {
                JObject matchedRegValues = InfAssess.AssessInf.AssessRegValues(regValues);
                if (matchedRegValues != null)
                {
                    assessedGpTmpl.Add("Registry Values", matchedRegValues);
                }
                knownKeys.Add("Registry Values");
            }
            ///////////////////////////////////////////////////////////////
            // System Access
            ///////////////////////////////////////////////////////////////
            JToken sysAccess = infToAssess["System Access"];
            if (sysAccess != null)
            {
                JObject assessedSysAccess = InfAssess.AssessInf.AssessSysAccess(sysAccess);
                if (assessedSysAccess != null)
                {
                    assessedGpTmpl.Add("System Access", assessedSysAccess);
                }
                knownKeys.Add("System Access");
            }
            ///////////////////////////////////////////////////////////////
            // Kerberos Policy
            ///////////////////////////////////////////////////////////////
            JToken kerbPolicy = infToAssess["Kerberos Policy"];
            if (kerbPolicy != null)
            {
                JObject assessedKerbPol = InfAssess.AssessInf.AssessKerbPolicy(kerbPolicy);
                if (assessedKerbPol != null)
                {
                    assessedGpTmpl.Add("Kerberos Policy", assessedKerbPol);
                }
                knownKeys.Add("Kerberos Policy");
            }
            ///////////////////////////////////////////////////////////////
            // Registry Keys
            ///////////////////////////////////////////////////////////////
            JToken regKeys = infToAssess["Registry Keys"];
            if (regKeys != null)
            {
                JObject assessedRegKeys = InfAssess.AssessInf.AssessRegKeys(regKeys);
                if (assessedRegKeys != null)
                {
                    assessedGpTmpl.Add("Registry Keys", assessedRegKeys);
                }
                knownKeys.Add("Registry Keys");
            }
            ///////////////////////////////////////////////////////////////
            // Group Membership
            ///////////////////////////////////////////////////////////////
            JToken grpMembership = infToAssess["Group Membership"];
            if (grpMembership != null)
            {
                JObject assessedgrpMembership = InfAssess.AssessInf.AssessGroupMembership(grpMembership);
                if (assessedgrpMembership != null)
                {
                    assessedGpTmpl.Add("Group Membership", assessedgrpMembership);
                }
                knownKeys.Add("Group Membership");
            }
            ///////////////////////////////////////////////////////////////
            // Service General Setting
            ///////////////////////////////////////////////////////////////
            JToken svcGenSetting = infToAssess["Service General Setting"];
            if (svcGenSetting != null)
            {
                JObject assessedSvcGenSetting = InfAssess.AssessInf.AssessServiceGenSetting(svcGenSetting);
                if (assessedSvcGenSetting != null && assessedSvcGenSetting.HasValues)
                {
                    assessedGpTmpl.Add("Service General Setting", assessedSvcGenSetting);
                }
                knownKeys.Add("Service General Setting");
            }

            //catch any stuff that falls through the cracks, i.e. look for headings on sections that we aren't parsing.
            List<string> headingsInInf =  new List<string>();
            foreach (JProperty section in infToAssess.Children<JProperty>())
            {
                string sectionName = section.Name;
                headingsInInf.Add(sectionName);
            }
            var slippedThrough = headingsInInf.Except(knownKeys);
            if (slippedThrough.Any() && GlobalVar.DebugMode)
            {
                Console.WriteLine("We didn't parse any of these sections:");
                foreach (string unparsedHeader in slippedThrough)
                {
                    Console.WriteLine(unparsedHeader);
                }
            }
            //mangle our json thing into a jobject and return it
            JObject assessedGpTmplJson = (JObject)JToken.FromObject(assessedGpTmpl);
            return assessedGpTmplJson;
        }

        public static JObject AssessGppJson(JObject gppToAssess)
        {
            GPPAssess.AssessGpp assessGpp = new GPPAssess.AssessGpp(gppToAssess);
            // get an array of categories in our GPP to assess to look at
            string[] gppCategories = gppToAssess.Properties().Select(p => p.Name).ToArray();
            // create a dict to put our results into before returning them
            Dictionary<string, JObject> assessedGppDict = new Dictionary<string, JObject>();
            // iterate over the array sending appropriate gpp data to the appropriate assess() function.
            foreach (string gppCategory in gppCategories)
            {
                //JObject gppCategoryJson = (JObject)gppToAssess[gppCategory];
                JObject assessedGpp = assessGpp.GetAssessed(gppCategory);

                if (assessedGpp != null)
                {
                    if (assessedGpp.HasValues)
                    {
                        assessedGppDict.Add(gppCategory, assessedGpp);
                    }
                }
            }
            JObject assessedGppJson = (JObject)JToken.FromObject(assessedGppDict);
            return assessedGppJson;
        }
    }
}