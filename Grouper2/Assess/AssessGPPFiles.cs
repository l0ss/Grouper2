using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    public class AssessGPPFiles
    {
            public static JObject GetAssessedFiles(JObject GPPFiles)
            {
                JProperty GPPFileProp = new JProperty("File", GPPFiles["File"]);
                JObject AssessedGPPFiles = new JObject(GPPFileProp);
                return AssessedGPPFiles;
                //Utility.DebugWrite("GPP is about Files");
                //Console.WriteLine(GPPFiles["File"]);
            }
    }
}
