using System.Security.Principal;
using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessPrivRights(JToken privRights)
    {
        JObject jsonData = JankyDb.Instance;
        JArray intPrivRights = (JArray) jsonData["privRights"]["item"];

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
            // our interest level always starts at 1. Everything is boring until proven otherwise.
            int interestLevel = 1;
            foreach (JToken intPrivRight in intPrivRights)
            {
                // if the priv is interesting
                if ((string) intPrivRight["privRight"] == privRight.Name)
                {
                    //create a jobj to put the trustees into
                    JObject trustees = new JObject();
                    //then for each trustee it's granted to
                    foreach (string trustee in privRight.Value)
                    {
                        string displayName = "unknown";
                        // clean up the trustee SID
                        string trusteeClean = trustee.Trim('*');
                        // check if it's a well known trustee in our JankyDB
                        JToken checkedSid = Utility.CheckSid(trusteeClean);

                        // extract some info if they match.
                        if (checkedSid != null)
                        {
                            displayName = (string) checkedSid["displayName"];
                        }
                        // if they don't match, try to resolve the sid with the domain.
                        // tbh it would probably be better to do this the other way around and prefer the resolved sid output over the contents of jankydb. @liamosaur?
                        else
                        {
                            //if (GlobalVar.OnlineChecks)
                            //{
                            try
                            {
                                if (trusteeClean.StartsWith(domainSid))
                                {
                                    string resolvedSid = LDAPstuff.GetUserFromSid(trusteeClean);
                                    displayName = resolvedSid;
                                }
                            }
                            catch (IdentityNotMappedException)
                            {
                                displayName = "Failed to resolve SID with domain.";
                            }

                            //}
                        }

                        trustees.Add(trusteeClean, displayName);
                    }

                    // add the results to our jobj of trustees if they are interesting enough.
                    string matchedPrivRightName = privRight.Name;
                    if (interestLevel >= GlobalVar.IntLevelToShow)
                    {
                        assessedPrivRights.Add(matchedPrivRightName, trustees);
                    }
                }
            }
        }

        return assessedPrivRights;
    }
}