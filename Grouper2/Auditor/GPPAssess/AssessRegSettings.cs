using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(Registry file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedRegistrySettings(file.JankyXmlStuff);
        }
        private AuditedGppXmlRegSettings GetAssessedRegistrySettings(JObject gppCategory)
        {
            // I both hate and fear this part of the thing. I want it to go away.

            AuditedGppXmlRegSettings assessedGppRegSettingsOut = new AuditedGppXmlRegSettings();

            if (gppCategory["Collection"] != null)
            {
                Dictionary<string, AuditedGppXmlRegCollection> assessedGppRegCollections = GetAssessedRegistryCollections(gppCategory["Collection"]);
                if (assessedGppRegCollections != null)
                {
                    assessedGppRegSettingsOut.RegCollections = assessedGppRegCollections;
                }
            }

            if (gppCategory["Registry"] != null)
            {
                Dictionary<string, AuditedGppXmlRegSetting> assessedGppRegSettingses = GetAssessedRegistrySettingses(gppCategory["Registry"]);
                if (assessedGppRegSettingses != null)
                {
                    assessedGppRegSettingsOut.RegSetting = assessedGppRegSettingses;
                }
            }

            // only return if there is something there, otherwise, null it
            if (assessedGppRegSettingsOut.RegCollections.Count > 0 || assessedGppRegSettingsOut.RegSetting.Count > 0)
            {
                return assessedGppRegSettingsOut;
            }
            return null;
        }
        

        private Dictionary<string, AuditedGppXmlRegCollection> GetAssessedRegistryCollections(JToken gppRegCollections)
        // another one of these methods to handle if the thing is a JArray or a single object.
        {
            Dictionary<string, AuditedGppXmlRegCollection> assessedRegistryCollections = new Dictionary<string, AuditedGppXmlRegCollection>();
            if (gppRegCollections is JArray)
            {
                int inc = 0;
                foreach (JToken gppRegCollection in gppRegCollections)
                {
                    // get the thing and skip if null
                    AuditedGppXmlRegCollection assessedGppRegCollection = GetAssessedRegistryCollection(gppRegCollection);
                    if (assessedGppRegCollection == null) continue;
                    // add the thing
                    assessedRegistryCollections.Add(inc.ToString(), assessedGppRegCollection);
                    inc++;
                    
                }
            }
            else
            {
                AuditedGppXmlRegCollection assessedGppRegCollection = GetAssessedRegistryCollection(gppRegCollections);
                if (assessedGppRegCollection != null)
                {
                    assessedRegistryCollections.Add("0", assessedGppRegCollection);
                }
            }

            // only return items, otherwise null
            return assessedRegistryCollections.Count > 0 
                ? assessedRegistryCollections 
                : null;
        }

        private AuditedGppXmlRegCollection GetAssessedRegistryCollection(JToken gppRegCollection)
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

            if (gppRegCollection["Registry"] != null && gppRegCollection.HasValues)
            {
                AuditedGppXmlRegCollection assessedRegistryCollection = new AuditedGppXmlRegCollection()
                {
                    Name = JUtil.GetSafeString(gppRegCollection, "@name"),
                    Changed = JUtil.GetSafeString(gppRegCollection, "@changed"),
                    Description = JUtil.GetSafeString(gppRegCollection, "@desc")
                };
                JToken registrySettingses = gppRegCollection["Registry"];
                Dictionary<string, AuditedGppXmlRegSetting> assessedRegistrySettingses = GetAssessedRegistrySettingses(registrySettingses);
                if (assessedRegistrySettingses != null && assessedRegistrySettingses.Count > 0)
                {
                    assessedRegistryCollection.Settings = assessedRegistrySettingses;
                }
                else
                {
                    return null;
                }
                return assessedRegistryCollection;
            }

            return null;
        }

        

        private Dictionary<string, AuditedGppXmlRegSetting> GetAssessedRegistrySettingses(JToken gppRegSettingses)
        // we name this method like we gollum cos otherwise the naming scheme goes pear-shaped
        // this method just figures out if it's a JArray or a single object and handles it appropriately
        {
            Dictionary<string, AuditedGppXmlRegSetting> assessedRegistrySettingses = new Dictionary<string, AuditedGppXmlRegSetting>();
            if (gppRegSettingses is JArray)
            {
                int inc = 0;
                foreach (JToken gppRegSetting in gppRegSettingses)
                {
                    AuditedGppXmlRegSetting assessedGppRegSetting = GetAssessedRegistrySetting(gppRegSetting);
                    if (assessedGppRegSetting != null)
                    {
                        assessedRegistrySettingses.Add(inc.ToString(), assessedGppRegSetting);
                        inc++;
                    }
                }
            }
            else
            {
                AuditedGppXmlRegSetting assessedGppRegSetting = GetAssessedRegistrySetting(gppRegSettingses);
                if (assessedGppRegSetting != null)
                {
                    assessedRegistrySettingses.Add("0", assessedGppRegSetting);
                }
            }

            // return if there is something to return, otherwise null
            return assessedRegistrySettingses.Count > 0 
                ? assessedRegistrySettingses 
                : null;
        }

        private AuditedGppXmlRegSetting GetAssessedRegistrySetting(JToken gppRegSetting)
        {
            List<RegKey> intRegKeysData = JankyDb.RegKeys.ToList();
            // get our data about what regkeys are interesting

            AuditedGppXmlRegSetting auditedSetting = new AuditedGppXmlRegSetting()
            {
                Interest = 1,
                DisplayName = JUtil.GetSafeString(gppRegSetting, "@name"),
                Status = JUtil.GetSafeString(gppRegSetting, "@status"),
                Changed = JUtil.GetSafeString(gppRegSetting, "@changed"),
                Action = JUtil.GetActionString(gppRegSetting["Properties"]["@action"].ToString()),
                Default = JUtil.GetSafeString(gppRegSetting["Properties"], "@default"),
                Hive = JUtil.GetSafeString(gppRegSetting["Properties"], "@hive")
            };
            string key = JUtil.GetSafeString(gppRegSetting["Properties"], "@key");

            foreach (RegKey intRegKey in intRegKeysData)
            {
                if (key.ToLower().Contains(intRegKey.Key.ToLower()))
                {
                    auditedSetting.TryBumpInterest(intRegKey.IntLevel);
                }
            }
            
            AuditedString investigatedKey = FileSystem.InvestigateString(key, this.InterestLevel) ??
                                            throw new ArgumentNullException(
                                                nameof(gppRegSetting));
            auditedSetting.TryBumpInterest(investigatedKey);
            auditedSetting.Key = investigatedKey;
            string name = JUtil.GetSafeString(gppRegSetting["Properties"], "@name");
            AuditedString investigatedName = FileSystem.InvestigateString(name, this.InterestLevel) ??
                                             throw new ArgumentNullException(
                                                 nameof(gppRegSetting));
            auditedSetting.TryBumpInterest(investigatedName);
            auditedSetting.Name = investigatedName;
            auditedSetting.Type = JUtil.GetSafeString(gppRegSetting["Properties"], "@type");
            
            string value = JUtil.GetSafeString(gppRegSetting["Properties"], "@value");
            AuditedString investigatedValue = FileSystem.InvestigateString(value, this.InterestLevel) ??
                                              throw new ArgumentNullException(
                                                  nameof(gppRegSetting));
            auditedSetting.TryBumpInterest(investigatedValue);
            auditedSetting.Value = investigatedValue;

            // only return a value if interest is high enough, otherwise null
            return auditedSetting.Interest >= this.InterestLevel 
                ? auditedSetting
                : null;
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