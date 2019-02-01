using System.Collections.Generic;
using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessRegValues(JToken regValues)
    {
        int interestLevel = 15;
        JObject jsonData = JankyDb.Instance;
        // get our data about what regkeys are interesting
        JArray intRegKeys = (JArray) jsonData["regKeys"]["item"];
        // set up a jobj for our results to go into
        JObject assessedRegValues = new JObject();

        foreach (JProperty regValue in regValues.Children<JProperty>())
        {
            // iterate over the list of interesting keys in our json "db".
            foreach (JToken intRegKey in intRegKeys)
            {
                // if it matches
                if ((string) intRegKey["regKey"] == regValue.Name)
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
                    if (interestLevel >= GlobalVar.IntLevelToShow)
                    {
                        JArray regKeyValueJArray = new JArray();
                        foreach (string value in regKeyValueArray)
                        {
                            regKeyValueJArray.Add(value);
                        }

                        assessedRegValues.Add(matchedRegKey, regKeyValueJArray);
                    }
                }
            }
        }

        return assessedRegValues;
    }
}