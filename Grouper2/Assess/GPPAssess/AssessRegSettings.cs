using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        private JObject GetAssessedRegistrySettings(JObject gppCategory)
        {
            // I both hate and fear this part of the thing. I want it to go away.

            JObject assessedGppRegSettingsOut = new JObject();

            if (gppCategory["Collection"] != null)
            {
                JObject assessedGppRegCollections = GetAssessedRegistryCollections(gppCategory["Collection"]);
                if (assessedGppRegCollections != null)
                {
                    assessedGppRegSettingsOut.Add("Registry Setting Collections", assessedGppRegCollections);
                }
            }

            if (gppCategory["Registry"] != null)
            {
                JObject assessedGppRegSettingses = GetAssessedRegistrySettingses(gppCategory["Registry"]);
                if (assessedGppRegSettingses != null)
                {
                    assessedGppRegSettingsOut.Add("Registry Settings", assessedGppRegSettingses);
                }
            }

            if (assessedGppRegSettingsOut.HasValues)
            {
                return assessedGppRegSettingsOut;
            }
            else
            {
                return null;
            }
        }
        

        private JObject GetAssessedRegistryCollections(JToken gppRegCollections)
        // another one of these methods to handle if the thing is a JArray or a single object.
        {
            JObject assessedRegistryCollections = new JObject();
            if (gppRegCollections is JArray)
            {
                int inc = 0;
                foreach (JToken gppRegCollection in gppRegCollections)
                {
                    JToken assessedGppRegCollection = GetAssessedRegistryCollection(gppRegCollection);
                    if (assessedGppRegCollection != null)
                    {
                        assessedRegistryCollections.Add(inc.ToString(), assessedGppRegCollection);
                        inc++;
                    }
                }
            }
            else
            {
                JToken assessedGppRegCollection = GetAssessedRegistryCollection(gppRegCollections);
                if (assessedGppRegCollection != null)
                {
                    assessedRegistryCollections.Add("0", assessedGppRegCollection);
                }
            }

            if (assessedRegistryCollections != null && assessedRegistryCollections.HasValues)
            {
                return assessedRegistryCollections;
            }
            return null;
        }

        private JToken GetAssessedRegistryCollection(JToken gppRegCollection)
        {
            // this method handles the 'collection' object, which contains a bunch of individual regkeys and these properties 

            /*
             
            Looks like the structure kind of goes like:

            You can have multiple collections in a collection JArray

            Collections have some top level properties like:
            @name
            @changed
            @uid
            @desc
            @bypassErrors
            Registry
                Contains a Settings JArray

             */
            JObject assessedRegistryCollection = new JObject
            {
                // add collection-specific properties
                { "Name", JUtil.GetSafeString(gppRegCollection, "@name") },
                { "Changed", JUtil.GetSafeString(gppRegCollection, "@changed") },
                { "Description", JUtil.GetSafeString(gppRegCollection, "@desc") }
            };

            if ((gppRegCollection["Registry"] != null) && gppRegCollection.HasValues)
            {
                JToken registrySettingses = gppRegCollection["Registry"];
                JToken assessedRegistrySettingses = GetAssessedRegistrySettingses(registrySettingses);
                if ((assessedRegistrySettingses != null) && assessedRegistrySettingses.HasValues)
                {
                    assessedRegistryCollection.Add("Registry Settings in Collection", assessedRegistrySettingses);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if ((assessedRegistryCollection != null) && assessedRegistryCollection.HasValues)
            {
                return assessedRegistryCollection;
            }
            else
            {
                return null;
            }
        }

        private JObject GetAssessedRegistrySettingses(JToken gppRegSettingses)
        // we name this method like we gollum cos otherwise the naming scheme goes pear-shaped
        // this method just figures out if it's a JArray or a single object and handles it appropriately
        {
            JObject assessedRegistrySettingses = new JObject();
            if (gppRegSettingses is JArray)
            {
                int inc = 0;
                foreach (JToken gppRegSetting in gppRegSettingses)
                {
                    JToken assessedGppRegSetting = GetAssessedRegistrySetting(gppRegSetting);
                    if (assessedGppRegSetting != null)
                    {
                        assessedRegistrySettingses.Add(inc.ToString(), assessedGppRegSetting);
                        inc++;
                    }
                }
            }
            else
            {
                JObject assessedGppRegSetting = GetAssessedRegistrySetting(gppRegSettingses);
                if (assessedGppRegSetting != null)
                {
                    assessedRegistrySettingses.Add("0", assessedGppRegSetting);
                }
            }

            if (assessedRegistrySettingses != null && assessedRegistrySettingses.HasValues)
            {
                return assessedRegistrySettingses;
            }
            return null;
        }

        private JObject GetAssessedRegistrySetting(JToken gppRegSetting)
        {
            JObject jankyDb = JankyDb.Instance;
            // get our data about what regkeys are interesting
            JArray intRegKeysData = (JArray)jankyDb["regKeys"];

            JObject assessedRegistrySetting = new JObject();
            assessedRegistrySetting.Add("Display Name", JUtil.GetSafeString(gppRegSetting, "@name"));
            assessedRegistrySetting.Add("Status", JUtil.GetSafeString(gppRegSetting, "@status"));
            assessedRegistrySetting.Add("Changed", JUtil.GetSafeString(gppRegSetting, "@changed"));
            assessedRegistrySetting.Add("Action", JUtil.GetActionString(gppRegSetting["Properties"]["@action"].ToString()));
            assessedRegistrySetting.Add("Default", JUtil.GetSafeString(gppRegSetting["Properties"], "@default"));
            assessedRegistrySetting.Add("Hive", JUtil.GetSafeString(gppRegSetting["Properties"], "@hive"));
            string key = JUtil.GetSafeString(gppRegSetting["Properties"], "@key");

            int interestLevel = 1;
            foreach (JToken intRegKey in intRegKeysData)
            {
                if (key.ToLower().Contains(intRegKey["regKey"].ToString().ToLower()))
                {
                    // get the name
                    string interestLevelString = intRegKey["intLevel"].ToString();
                    // if it matches at all it's a 1.
                    
                    // if we can get the interest level from it, do so, otherwise throw an error that we need to fix something.
                    if (!int.TryParse(interestLevelString, out interestLevel))
                    {
                        Utility.Output.DebugWrite(intRegKey["regKey"].ToString() +
                                                  " in jankydb doesn't have an interest level assigned.");
                    }
                }
            }
            
            JObject investigatedKey = FileSystem.InvestigateString(key);
            if ((int) investigatedKey["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int) investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Add("Key", investigatedKey);
            string name = JUtil.GetSafeString(gppRegSetting["Properties"], "@name");
            JObject investigatedName = FileSystem.InvestigateString(name);
            if ((int)investigatedName["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int)investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Add("Name", investigatedName);
            assessedRegistrySetting.Add("Type", JUtil.GetSafeString(gppRegSetting["Properties"], "@type"));
            string value = JUtil.GetSafeString(gppRegSetting["Properties"], "@value");
            JObject investigatedValue = FileSystem.InvestigateString(value);
            if ((int)investigatedValue["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int)investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Add("Value", investigatedValue);

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                return new JObject(assessedRegistrySetting);
            }
            else
            {
                return null;
            }
        }
        /*

            Settings objects are just a JArray of individual regkeys which have:
            @name
            @status
            @changed
            @uid
            Properties
                @action
                @displayDecimal
                @default
                @hive
                @key
                @name
                @type
                @value


        */
    }
}