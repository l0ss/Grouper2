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
            int interestLevel = 2;
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
                            string trustee = trusteeJToken.ToString();
                            if (GlobalVar.OnlineChecks)
                            {
                                trustees.Add(GetTrustee(trustee));
                            }
                            else
                            {
                                trustees.Add(new JProperty(trustee, "Unable to resolve SID"));
                            }
                        }
                    }
                    else
                    {
                        if (GlobalVar.OnlineChecks)
                        {
                            trustees.Add(GetTrustee(privRight.Value.ToString()));
                        }
                        else
                        {
                            trustees.Add("Trustee", privRight.Value.ToString());
                        }
                    }

                    // add the results to our jobj of trustees if they are interesting enough.
                    if (interestLevel >= GlobalVar.IntLevelToShow)
                    {
                        assessedPrivRights.Add(privRight.Name, trustees);
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
        string trusteeClean = trustee.Trim('*');

        try
        {
            string resolvedSid = LDAPstuff.GetUserFromSid(trusteeClean);
            displayName = resolvedSid;
        }
        catch (IdentityNotMappedException)
        {
            displayName = "Failed to resolve SID.";
            // check if it's a well known trustee in our JankyDB
            JToken checkedSid = Utility.CheckSid(trusteeClean);
            if (checkedSid != null)
            {
                displayName = (string)checkedSid["displayName"];
            }
        }

        return new JProperty(trusteeClean, displayName);
    }
}