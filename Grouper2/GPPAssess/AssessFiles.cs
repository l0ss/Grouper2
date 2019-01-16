using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedFiles(JObject gppCategory)
        {
            JObject assessedFiles = new JObject();

            if (gppCategory["File"] is JArray)
            {
                foreach (JObject gppFile in gppCategory["File"])
                {
                    JObject assessedFile = GetAssessedFile(gppFile);
                    if (assessedFile != null)
                    {
                        assessedFiles.Add(gppFile["@uid"].ToString(), assessedFile);
                    }
                }
            }
            else
            {
                JObject gppFile = (JObject) JToken.FromObject(gppCategory["File"]);
                JObject assessedFile = GetAssessedFile(gppFile);
                if (assessedFile != null)
                {
                    assessedFiles.Add(gppFile["@uid"].ToString(), assessedFile);
                }
            }

            return assessedFiles;
        }

        private JObject GetAssessedFile(JObject gppFile)
        {
            int interestLevel = 3;
            JObject assessedFile = new JObject();
            JToken gppFileProps = gppFile["Properties"];
            assessedFile.Add("Name", gppFile["@name"].ToString());
            assessedFile.Add("Status", gppFile["@status"].ToString());
            assessedFile.Add("Changed", gppFile["@changed"].ToString());
            string gppFileAction = Utility.GetActionString(gppFileProps["@action"].ToString());
            assessedFile.Add("Action", gppFileAction);
            JToken targetPathJToken = gppFileProps["@targetPath"];
            if (targetPathJToken != null)
            {
                assessedFile.Add("Target Path", gppFileProps["@targetPath"].ToString());
            }

            JToken fromPathJToken = gppFileProps["@fromPath"];
            if (fromPathJToken != null)
            {
                string fromPath = gppFileProps["@fromPath"].ToString();

                if (GlobalVar.OnlineChecks && (fromPath.Length > 0))
                {
                    JObject assessedPath = Utility.InvestigatePath(gppFileProps["@fromPath"].ToString());
                    assessedFile.Add("From Path", assessedPath);
                    if (assessedPath["InterestLevel"] != null)
                    {
                        int pathInterest = (int) assessedPath["InterestLevel"];
                        interestLevel = interestLevel + pathInterest;
                    }
                }
                else
                {
                    assessedFile.Add("From Path", fromPath);
                }
            }

            // if it's too boring to be worth showing, return an empty jobj.
            if (interestLevel <= GlobalVar.IntLevelToShow)
            {
                return null;
            }

            return assessedFile;
        }
    }
}