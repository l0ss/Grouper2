using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    public class AssessGPPFolders
    {
            public static JObject GetAssessedFolders(JObject GPPFolders)
            {
                JProperty GPPFoldersProp = new JProperty("Folder", GPPFolders["Folder"]);
                JObject AssessedGPPFolders = new JObject(GPPFoldersProp);
                return AssessedGPPFolders;
                //Utility.DebugWrite("GPP is about Folders");
                //Console.WriteLine(GPPFolders["Folder"]);
            }
    }
}
