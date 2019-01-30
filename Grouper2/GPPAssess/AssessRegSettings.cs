using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json.Linq;

namespace Grouper2
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
                { "Name", Utility.GetSafeString(gppRegCollection, "@name") },
                { "Changed", Utility.GetSafeString(gppRegCollection, "@changed") },
                { "Description", Utility.GetSafeString(gppRegCollection, "@desc") }
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
            JObject assessedRegistrySetting = new JObject();
            int interestLevel = 1;
            assessedRegistrySetting.Add("Display Name", Utility.GetSafeString(gppRegSetting, "@name"));
            assessedRegistrySetting.Add("Status", Utility.GetSafeString(gppRegSetting, "@status"));
            assessedRegistrySetting.Add("Changed", Utility.GetSafeString(gppRegSetting, "@changed"));
            assessedRegistrySetting.Add("Action", Utility.GetActionString(gppRegSetting["Properties"]["@action"].ToString()));
            assessedRegistrySetting.Add("Default", Utility.GetSafeString(gppRegSetting["Properties"], "@default"));
            assessedRegistrySetting.Add("Hive", Utility.GetSafeString(gppRegSetting["Properties"], "@hive"));
            string key = Utility.GetSafeString(gppRegSetting["Properties"], "@key");
            JObject investigatedKey = Utility.InvestigateString(key);
            if ((int) investigatedKey["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int) investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Add("Key", investigatedKey);
            string name = Utility.GetSafeString(gppRegSetting["Properties"], "@name");
            JObject investigatedName = Utility.InvestigateString(name);
            if ((int)investigatedName["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int)investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Add("Name", investigatedName);
            assessedRegistrySetting.Add("Type", Utility.GetSafeString(gppRegSetting["Properties"], "@type"));
            string value = Utility.GetSafeString(gppRegSetting["Properties"], "@value");
            JObject investigatedValue = Utility.InvestigateString(value);
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