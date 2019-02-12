using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        // checked for GetSafeJProp
        private JObject GetAssessedRegistrySettings(JObject gppCategory)
        {
            // I both hate and fear this part of the thing. I want it to go away.

            JObject assessedGppRegSettingsOut = new JObject();

            if (gppCategory["Collection"] != null)
            {
                JObject assessedGppRegCollections = GetAssessedRegistryCollections(gppCategory["Collection"]);
                assessedGppRegSettingsOut.Merge(JUtil.GetSafeJProp("Registry Setting Collections", assessedGppRegCollections));
            }

            if (gppCategory["Registry"] != null)
            {
                JObject assessedGppRegSettingses = GetAssessedRegistrySettingses(gppCategory["Registry"]);
                assessedGppRegSettingsOut.Merge(JUtil.GetSafeJProp("Registry Settings", assessedGppRegSettingses));
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
                    assessedRegistryCollections.Merge(JUtil.GetSafeJProp(inc.ToString(), assessedGppRegCollection));
                    inc++;
                }
            }
            else
            {
                JToken assessedGppRegCollection = GetAssessedRegistryCollection(gppRegCollections);
                assessedRegistryCollections.Merge(JUtil.GetSafeJProp("0", assessedGppRegCollection));
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
            JObject assessedRegistryCollection = new JObject();
            
            // add collection-specific properties
            assessedRegistryCollection.Merge(JUtil.GetSafeJProp("Name", gppRegCollection, "@name"));
            assessedRegistryCollection.Merge(JUtil.GetSafeJProp("Changed", gppRegCollection, "@changed"));
            assessedRegistryCollection.Merge(JUtil.GetSafeJProp("Description", gppRegCollection, "@desc"));
            

            if ((gppRegCollection["Registry"] != null) && gppRegCollection.HasValues)
            {
                JToken registrySettingses = gppRegCollection["Registry"];
                JToken assessedRegistrySettingses = GetAssessedRegistrySettingses(registrySettingses);
                if ((assessedRegistrySettingses != null) && assessedRegistrySettingses.HasValues)
                {
                    assessedRegistryCollection.Merge(JUtil.GetSafeJProp("Registry Settings in Collection", assessedRegistrySettingses));
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
                    assessedRegistrySettingses.Merge(JUtil.GetSafeJProp(inc.ToString(), assessedGppRegSetting));
                    inc++;
                }
            }
            else
            {
                JObject assessedGppRegSetting = GetAssessedRegistrySetting(gppRegSettingses);
                assessedRegistrySettingses.Merge(JUtil.GetSafeJProp("0", assessedGppRegSetting));
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
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp( "Display Name", gppRegSetting, "@name"));
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Status", gppRegSetting, "@status"));
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Changed", gppRegSetting, "@changed"));
            JToken gppRegProps = gppRegSetting["Properties"];
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Action", JUtil.GetActionString(gppRegProps["@action"].ToString())));
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Default", gppRegProps, "@default"));
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Hive", gppRegProps, "@hive"));
            // get the actual key
            string key = JUtil.GetSafeString(gppRegProps, "@key");
            // investigate it
            JObject investigatedKey = FileSystem.InvestigateString(key);
            if ((int) investigatedKey["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int) investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Key", investigatedKey));
            // repeat for the name
            string name = JUtil.GetSafeString(gppRegProps, "@name");
            JObject investigatedName = FileSystem.InvestigateString(name);
            if ((int)investigatedName["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int)investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Name", investigatedName));
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Type", gppRegProps, "@type"));
            // then investigate the value
            string value = JUtil.GetSafeString(gppRegProps, "@value");
            JObject investigatedValue = FileSystem.InvestigateString(value);
            if ((int)investigatedValue["InterestLevel"] >= interestLevel)
            {
                interestLevel = (int)investigatedKey["InterestLevel"];
            }
            assessedRegistrySetting.Merge(JUtil.GetSafeJProp("Value", investigatedValue));

            if (interestLevel >= GlobalVar.IntLevelToShow)
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