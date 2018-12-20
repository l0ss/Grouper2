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
                    JObject privRightsResults = AssessPrivRights(privRights);
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
                JObject matchedRegValues = AssessRegValues(regValues);
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

        public static JObject AssessPrivRights(JToken privRights)
        {
            JObject jsonData = JankyDb.Instance;
            JArray intPrivRights = (JArray)jsonData["privRights"]["item"];

            // create an object to put the results in
            Dictionary<string, Dictionary<string, string>> matchedPrivRights = new Dictionary<string, Dictionary<string, string>>();

            //set an intentionally non-matchy domainSid value unless we doing online checks.
            string domainSid = "X";
            if (GlobalVar.OnlineChecks)
            {
                domainSid = LDAPstuff.GetDomainSid();
            }

            foreach (JProperty privRight in privRights.Children<JProperty>())
            {
                foreach (JToken intPrivRight in intPrivRights)
                {
                    // if the priv is interesting
                    if ((string)intPrivRight["privRight"] == privRight.Name)
                    {
                        //create a dict to put the trustees into
                        Dictionary<string, string> trusteesDict = new Dictionary<string, string>();
                        //then for each trustee it's granted to
                        foreach (string trustee in privRight.Value)
                        {
                            string displayName = "unknown";
                            // clean up the trustee SID
                            string trusteeClean = trustee.Trim('*');
                            JToken checkedSid = Utility.CheckSid(trusteeClean);

                            // display some info if they match.
                            if (checkedSid != null)
                            {
                                displayName = (string)checkedSid["displayName"];
                            }
                            // if they don't match, handle that.
                            else
                            {
                                if (GlobalVar.OnlineChecks)
                                {
                                    try
                                    {
                                        if (trusteeClean.StartsWith(domainSid))
                                        {
                                            string resolvedSid = LDAPstuff.GetUserFromSID(trusteeClean);
                                            displayName = resolvedSid;
                                        }
                                    }
                                    catch (IdentityNotMappedException e)
                                    {
                                        displayName = "Failed to resolve SID";
                                    }
                                    //LDAPStuff.ResolveSID?
                                    //TODO: look up unknown SIDS in the domain if we can.
                                }
                            }
                            trusteesDict.Add(trusteeClean, displayName);
                        }
                        // add the results to our dictionary of trustees
                        string matchedPrivRightName = privRight.Name;
                        matchedPrivRights.Add(matchedPrivRightName, trusteesDict);
                    }
                }
            }
            // cast our dict to a jobject and return it.
            JObject matchedPrivRightsJson = (JObject)JToken.FromObject(matchedPrivRights);
            return matchedPrivRightsJson;
        }

        public static JObject AssessRegValues(JToken regValues)
        {
            JObject jsonData = JankyDb.Instance;
            // get our data about what regkeys are interesting
            JArray intRegKeys = (JArray)jsonData["regKeys"]["item"];
            // set up a dictionary for our results to go into
            Dictionary<string, string[]> matchedRegValues = new Dictionary<string, string[]>();

            foreach (JProperty regValue in regValues.Children<JProperty>())
            {
                // iterate over the list of interesting keys in our json "db".
                foreach (JToken intRegKey in intRegKeys)
                {
                    // if it matches
                    if ((string)intRegKey["regKey"] == regValue.Name)
                    {
                        string matchedRegKey = regValue.Name;
                        //create a list to put the values in
                        List<string> regKeyValueList = new List<string>();
                        foreach (string thing in regValue.Value)
                        {
                            // put the values in the list
                            regKeyValueList.Add(thing);
                        }
                        string[] regKeyValueArray = regKeyValueList.ToArray();
                        matchedRegValues.Add(matchedRegKey, regKeyValueArray);
                    }
                }
            }
            // cast our output into a jobject and return it
            JObject matchedRegValuesJson = (JObject)JToken.FromObject(matchedRegValues);
            return matchedRegValuesJson;
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
                JObject gppCategoryJson = (JObject)gppToAssess[gppCategory];
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