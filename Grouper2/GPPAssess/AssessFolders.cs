using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedFolders(JObject gppCategory)
        {
            int interestLevel = 0;
            JProperty gppFoldersProp = new JProperty("Folder", gppCategory["Folder"]);
            JObject assessedGppFolders = new JObject(gppFoldersProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppFolders = new JObject();
            }

            return assessedGppFolders;
        }
    }
}