using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    class AssessGPPIniFiles
    {
            // none of these assess functions do anything but return the values from the GPP yet.
            public static JObject GetAssessedIniFiles(JObject GPPIniFiles)
            {
                //Utility.DebugWrite("GPP is about GPPIniFiles");
                JObject AssessedGPPIniFiles = (JObject)GPPIniFiles["Ini"];
                //Console.WriteLine(AssessedGPPIniFiles.ToString());
                return AssessedGPPIniFiles;
            }
    }
}
