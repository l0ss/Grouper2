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
                    assessedRegistryCollections.Add(inc.ToString(), assessedGppRegCollection);
                    inc++;
                }
            }
            else
            {
                JToken assessedGppRegCollection = GetAssessedRegistryCollection(gppRegCollections);
                assessedRegistryCollections.Add("0", assessedGppRegCollection);
            }

            if (assessedRegistryCollections != null && assessedRegistryCollections.HasValues)
            {
                return assessedRegistryCollections;
            }
            return null;
        }

        private JToken GetAssessedRegistryCollection(JToken gppRegCollection)
        {
            // this method handles the 'collection' object, which contains a bunch of individual regkeys
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
                    assessedRegistrySettingses.Add(inc.ToString(), assessedGppRegSetting);
                    inc++;
                }
            }
            else
            {
                JObject assessedGppRegSetting = GetAssessedRegistrySetting(gppRegSettingses);
                assessedRegistrySettingses.Add("0", assessedGppRegSetting);
            }

            if (assessedRegistrySettingses != null && assessedRegistrySettingses.HasValues)
            {
                return assessedRegistrySettingses;
            }
            return null;
        }

        private JObject GetAssessedRegistrySetting(JToken gppRegSetting)
        {
            JObject assessedRegistrySetting = JObject.FromObject(gppRegSetting);
            // this is the method that ACTUALLY looks at reg keys. blugh.
            if ((gppRegSetting != null) && gppRegSetting.HasValues)
            {
                return assessedRegistrySetting;
            }
            else
            {
                return null;
            }
        }
    }
}