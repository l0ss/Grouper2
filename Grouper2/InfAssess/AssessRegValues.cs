using System.Collections.Generic;
using System.Linq;
using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessRegValues(JToken regValues)
    {
        JObject jsonData = JankyDb.Instance;
        // get our data about what regkeys are interesting
        JArray intRegKeys = (JArray) jsonData["regKeys"];
        // set up a jobj for our results to go into
        JObject assessedRegValues = new JObject();

        foreach (JProperty regValue in regValues.Children<JProperty>())
        {
            // iterate over the list of interesting keys in our json "db".
            foreach (JToken intRegKey in intRegKeys)
            {
                // if it matches an interesting key
                if ((string) intRegKey["regKey"] == regValue.Name)
                {
                    // get the name
                    string matchedRegKey = regValue.Name;
                    string keyTypeNum = regValue.Value[0].ToString();
                    string keyTypeString = "";
                    switch (keyTypeNum)
                    {
                        case "4":
                            keyTypeString = "REG_DWORD";
                            break;
                        case "7":
                            keyTypeString = "REG_MULTI_SZ";
                            break;
                    }

                    if (keyTypeString == "REG_DWORD")
                    {
                        // if it's a dword it'll only have one value
                        assessedRegValues.Add(matchedRegKey, regValue.Value[1].ToString());
                    }
                    else if (keyTypeString == "REG_MULTI_SZ")
                    {
                        // if it's a multi we'll need to process the rest of the values
                        JArray regValuesJArray = new JArray();
                        foreach (JToken value in regValue.Value.Skip(1))
                        {
                            regValuesJArray.Add(value.ToString());
                        }
                        assessedRegValues.Add(matchedRegKey, regValuesJArray);
                    }
                    
                }
            }
        }

        if (assessedRegValues.HasValues)
        {
            return assessedRegValues;
        }

        return null;

    }
}