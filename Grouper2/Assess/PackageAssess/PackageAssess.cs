using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class PackageAssess
    {
        public static JProperty AssessPackage(KeyValuePair<string, JToken> gpoPackageKvp)
        {
            JToken gpoPackage = gpoPackageKvp.Value;
            int interestLevel = 3;
            JArray assessedPackage = new JArray();

            if (gpoPackage["MSI Path"] != null)
            {
                string msiPath = gpoPackage["MSI Path"].ToString();
                JObject assessedMsiPath = FileSystem.InvestigatePath(msiPath);
                if ((assessedMsiPath != null) && (assessedMsiPath.HasValues))
                {
                    gpoPackage["MSI Path"] = assessedMsiPath;
                    if (assessedMsiPath["InterestLevel"] != null)
                    {
                        if ((int)assessedMsiPath["InterestLevel"] > interestLevel)
                        {
                            interestLevel = (int)assessedMsiPath["InterestLevel"];
                        }
                    }
                }

                if (interestLevel >= GlobalVar.IntLevelToShow)
                {
                    return new JProperty(gpoPackageKvp.Key, gpoPackage);
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

        }
        
    }
}
