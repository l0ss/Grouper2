using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        // these are the reg keys that the admin doesn't usually set manually
        // e.g. set using the 'security policy' gui
        public List<AuditedRegistryValues> AssessRegValues(JToken regValues)
        {
            if (regValues == null) 
                throw new ArgumentNullException(nameof(regValues));
            // get our data about what regkeys are interesting
            RegKey[] intRegKeysData = JankyDb.RegKeys.ToArray();
            List<AuditedRegistryValues> assessedRegValues = new List<AuditedRegistryValues>();

            foreach (JProperty regValue in regValues.Children<JProperty>())
            {
                // iterate over the list of interesting keys in our json "db".
                foreach (RegKey intRegKey in intRegKeysData)
                {
                    // ensure it matches an interesting key before continuing
                    if (!regValue.Name.ToLower().Contains(intRegKey.Key.ToLower())) continue;

                    // if it matches at all it's a 1.
                    int interestLevel = 1;
                    // but can be overriden by the keys from the db
                    if (intRegKey.IntLevel > 1)
                    {
                        interestLevel = intRegKey.IntLevel;
                    }
                    // break off here if the interest isn't high enough
                    if (interestLevel < this.InterestLevel) continue;

                    // determine the key type based on the value[0] number used
                    // and collect info now we know it's interesting
                    if (regValue.Value[0].ToString() == "4") //REG_DWORD
                    {
                        // if it's a dword it'll only have one value so add the whole thing
                        assessedRegValues.Add(new AuditedRegistryValues()
                        {
                            Name = regValue.Name,
                            KeyValues = new List<string>()
                            {
                                regValue.Value[1].ToString()
                            }
                        });
                    }
                    else if (regValue.Value[0].ToString() == "7") //REG_MULTI_SZ
                    {
                        // if it's a multi we'll need to process the rest of the values as well
                        assessedRegValues.Add(new AuditedRegistryValues()
                        {
                            Name = regValue.Name,
                            KeyValues = regValue.Value.Skip(1).Select(value => value.ToString()).ToList()
                        });
                    }
                }
            }

            // only return the list if there is something in it
            return assessedRegValues.Count > 0 
                ? assessedRegValues 
                : null;
        }
    }
}