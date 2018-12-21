using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Security.Principal;

namespace Grouper2
{
    class AssessHandlers
    {
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
                    JObject privRightsResults = AssessInf.AssessPrivRights(privRights);
                    if (privRightsResults.Count > 0)
                    {
                        assessedGpTmpl.Add("privRights", privRightsResults);
                    }
                    knownKeys.Add("Privilege Rights");
                }
            ///////////////////////////////////////////////////////////////
            // Registry Values
            ///////////////////////////////////////////////////////////////
            JToken regValues = infToAssess["Registry Values"];

            if (regValues != null)
            {
                JObject matchedRegValues = AssessInf.AssessRegValues(regValues);
                if (matchedRegValues.Count > 0)
                {
                    assessedGpTmpl.Add("regValues", matchedRegValues);
                }
                knownKeys.Add("Registry Values");
            }
            

            //TODO:
            //System Access
            //Registry Keys
            //Group Membership
            //Service General Setting

            //catch any stuff that falls through the cracks, i.e. look for headings on sections that we aren't parsing.

            List<string> headingsInInf =  new List<string>();
            foreach (JProperty section in infToAssess.Children<JProperty>())
            {
                string sectionName = section.Name;
                headingsInInf.Add(sectionName);
            }
            var slippedThrough = headingsInInf.Except(knownKeys);
            if (slippedThrough.Any())
            {
                Utility.DebugWrite("We didn't parse any of these sections:");
                foreach (var unparsedHeader in slippedThrough)
                {
                    Console.WriteLine(unparsedHeader);
                    //  System Access +
                    //  Kerberos Policy -
                    //  Event Audit -
                    //  Registry Values +
                    //  Registry Keys +
                    //  Group Membership +
                    //  Service General Setting +
                }
            }
            
            //mangle our json thing into a jobject and return it
            JObject assessedGpTmplJson = (JObject)JToken.FromObject(assessedGpTmpl);
            return assessedGpTmplJson;
        }

        public static JObject AssessGppJson(JObject gppToAssess)
        {
            AssessGpp assessGpp = new AssessGpp(gppToAssess);
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
                    assessedGppDict.Add(gppCategory, assessedGpp);
                }

            }
            JObject assessedGppJson = (JObject)JToken.FromObject(assessedGppDict);
            return assessedGppJson;
        }
    }
}