using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Grouper2
{
    class AssessHandlers
    {
        // Assesses the contents of a GPTmpl
        public static JObject AssessGPTmpl(JObject InfToAssess)
        {
            // create a dict to put all our results into
            Dictionary<string, JObject> AssessedGPTmpl = new Dictionary<string, JObject>();

            // an array for GPTmpl headings to ignore.
            List<string> KnownKeys = new List<string>
            {
                "Unicode",
                "Version"
            };

            // go through each category we care about and look for goodies.
            ///////////////////////////////////////////////////////////////
            // Privilege Rights
            ///////////////////////////////////////////////////////////////
            JToken PrivRights = InfToAssess["Privilege Rights"];

            if (PrivRights != null)
                {
                    JObject PrivRightsResults = AssessPrivRights(PrivRights);
                    if (PrivRightsResults.Count > 0)
                    {
                        AssessedGPTmpl.Add("PrivRights", PrivRightsResults);
                    }
                    KnownKeys.Add("Privilege Rights");
                }
            ///////////////////////////////////////////////////////////////
            // Registry Values
            ///////////////////////////////////////////////////////////////
            JToken RegValues = InfToAssess["Registry Values"];

            if (RegValues != null)
            {
                JObject MatchedRegValues = AssessRegValues(RegValues);
                if (MatchedRegValues.Count > 0)
                {
                    AssessedGPTmpl.Add("RegValues", MatchedRegValues);
                }
                KnownKeys.Add("Registry Values");
            }
            

            //TODO:
            //System Access
            //Registry Keys
            //Group Membership
            //Service General Setting

            //catch any stuff that falls through the cracks, i.e. look for headings on sections that we aren't parsing.

            List<string> HeadingsInInf =  new List<string>();
            foreach (JProperty Section in InfToAssess.Children<JProperty>())
            {
                string SectionName = Section.Name;
                HeadingsInInf.Add(SectionName);
            }
            var SlippedThrough = HeadingsInInf.Except(KnownKeys);
            if (SlippedThrough.Any())
            {
                Utility.DebugWrite("We didn't parse any of these sections:");
                foreach (var UnparsedHeader in SlippedThrough)
                {
                    Console.WriteLine(UnparsedHeader);
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
            JObject AssessedGPTmplJson = (JObject)JToken.FromObject(AssessedGPTmpl);
            return AssessedGPTmplJson;
        }

        public static JObject AssessPrivRights(JToken PrivRights)
        {
            JObject JsonData = JankyDB.Instance;
            JArray IntPrivRights = (JArray)JsonData["privRights"]["item"];

            // create an object to put the results in
            Dictionary<string, Dictionary<string, string>> MatchedPrivRights = new Dictionary<string, Dictionary<string, string>>();

            foreach (JProperty PrivRight in PrivRights.Children<JProperty>())
            {
                foreach (JToken IntPrivRight in IntPrivRights)
                {
                    // if the priv is interesting
                    if ((string)IntPrivRight["privRight"] == PrivRight.Name)
                    {
                        //create a dict to put the trustees into
                        Dictionary<string, string> TrusteesDict = new Dictionary<string, string>();
                        //then for each trustee it's granted to
                        foreach (string trustee in PrivRight.Value)
                        {
                            string DisplayName = "unknown";
                            // clean up the trustee SID
                            string TrusteeClean = trustee.Trim('*');
                            JToken CheckedSID = Utility.CheckSID(TrusteeClean);

                            // display some info if they match.
                            if (CheckedSID != null)
                            {
                                DisplayName = (string)CheckedSID["displayName"];
                            }
                            // if they don't match, handle that.
                            else
                            {
                                if (GlobalVar.OnlineChecks)
                                {
                                    //LDAPStuff.ResolveSID?
                                    //TODO: look up unknown SIDS in the domain if we can.
                                }
                            }
                            TrusteesDict.Add(TrusteeClean, DisplayName);
                        }
                        // add the results to our dictionary of trustees
                        string MatchedPrivRightName = PrivRight.Name;
                        MatchedPrivRights.Add(MatchedPrivRightName, TrusteesDict);
                    }
                }
            }
            // cast our dict to a jobject and return it.
            JObject MatchedPrivRightsJson = (JObject)JToken.FromObject(MatchedPrivRights);
            return MatchedPrivRightsJson;
        }

        public static JObject AssessRegValues(JToken RegValues)
        {
            JObject JsonData = JankyDB.Instance;
            // get our data about what regkeys are interesting
            JArray IntRegKeys = (JArray)JsonData["regKeys"]["item"];
            // set up a dictionary for our results to go into
            Dictionary<string, string[]> MatchedRegValues = new Dictionary<string, string[]>();

            foreach (JProperty RegValue in RegValues.Children<JProperty>())
            {
                // iterate over the list of interesting keys in our json "db".
                foreach (JToken IntRegKey in IntRegKeys)
                {
                    // if it matches
                    if ((string)IntRegKey["regKey"] == RegValue.Name)
                    {
                        string MatchedRegKey = RegValue.Name;
                        //create a list to put the values in
                        List<string> RegKeyValueList = new List<string>();
                        foreach (string thing in RegValue.Value)
                        {
                            // put the values in the list
                            RegKeyValueList.Add(thing);
                        }
                        string[] RegKeyValueArray = RegKeyValueList.ToArray();
                        MatchedRegValues.Add(MatchedRegKey, RegKeyValueArray);
                    }
                }
            }
            // cast our output into a jobject and return it
            JObject MatchedRegValuesJson = (JObject)JToken.FromObject(MatchedRegValues);
            return MatchedRegValuesJson;
        }

        public static JObject AssessGPPJson(JObject GPPToAssess)
        {
            // get an array of categories in our GPP to assess to look at
            string[] GPPCategories = GPPToAssess.Properties().Select(p => p.Name).ToArray();
            // create a dict to put our results into before returning them
            Dictionary<string, JObject> AssessedGPPDict = new Dictionary<string, JObject>();
            // iterate over the array sending appropriate gpp data to the appropriate assess() function.
            foreach (string GPPCategory in GPPCategories)
            {
                AssessGPP assessGPP = new AssessGPP(GPPToAssess);
                JObject AssessedGPP = assessGPP.GetAssessed(GPPCategory);

                if (AssessedGPP != null)
                {
                    AssessedGPPDict.Add(GPPCategory, AssessedGPP);
                }

            }
            JObject AssessedGPPJson = (JObject)JToken.FromObject(AssessedGPPDict);
            return AssessedGPPJson;
        }
    }
}