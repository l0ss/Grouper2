using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using Newtonsoft.Json.Linq;

namespace Grouper2.InfAssess
{
    internal static partial class AssessInf
    {
        public static JObject AssessPrivRights(JToken privRights)
        {
            JObject jankyDb = JankyDb.Instance;
            JArray intPrivRights = (JArray) jankyDb["privRights"];

            // create an object to put the results in
            JObject assessedPrivRights = new JObject();

            //set an intentionally non-matchy domainSid value unless we doing online checks.
            string domainSid = "X";
            if (GlobalVar.OnlineChecks)
            {
                domainSid = LDAPstuff.GetDomainSid();
            }

            //iterate over the entries
            foreach (JProperty privRight in privRights.Children<JProperty>())
            {
                foreach (JToken intPrivRight in intPrivRights)
                {
                    // if the priv is interesting
                    if ((string) intPrivRight["privRight"] == privRight.Name)
                    {
                        //create a jobj to put the trustees into
                        JObject trustees = new JObject();
                        //then for each trustee it's granted to
                        if (privRight.Value is JArray)
                        {
                            foreach (JToken trusteeJToken in privRight.Value)
                            {
                                int interestLevel = 2;
                                string trustee = trusteeJToken.ToString();
                                string trusteeClean = trustee.Trim('*');
                                string trusteeHighOrLow = Utility.GetWKSidHighOrLow(trusteeClean);
                                if (trusteeHighOrLow == "Low")
                                {
                                    interestLevel = 10;
                                }
                                if (trusteeHighOrLow == "High")
                                {
                                    interestLevel = 0;
                                }
                                if (interestLevel >= GlobalVar.IntLevelToShow)
                                {
                                    trustees.Add(GetTrustee(trusteeClean));
                                }
                            }
                        }
                        else
                        {
                            int interestLevel = 2;
                            string trusteeClean = privRight.Value.ToString().Trim('*');
                            string trusteeHighOrLow = Utility.GetWKSidHighOrLow(trusteeClean);
                            if (trusteeHighOrLow == "Low")
                            {
                                interestLevel = 10;
                            }
                            if (trusteeHighOrLow == "High")
                            {
                                interestLevel = 0;
                            }
                            if (interestLevel >= GlobalVar.IntLevelToShow)
                            {
                                trustees.Add(GetTrustee(trusteeClean));
                            }
                        }

                        // add the results to our jobj of trustees if they are interesting enough.
                        if (trustees.HasValues)
                        {
                            assessedPrivRights.Add(new JProperty(privRight.Name, trustees));
                        }
                    }
                }
            }

            return assessedPrivRights;
        }

        static JProperty GetTrustee(string trustee)
        {
            string displayName = "";
            // clean up the trustee SID

           
           string resolvedSid = LDAPstuff.GetUserFromSid(trustee);
           displayName = resolvedSid;
           

            return new JProperty(trustee, displayName);
        }
    }
}