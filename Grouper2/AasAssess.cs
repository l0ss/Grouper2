using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public class AasAssess
    {
        public static JObject AssessAasFile(JObject parsedAasFile)
        {
            JObject assessedAasFile = new JObject();
            int interestLevel = 3;
            if (parsedAasFile["MSI Path"] != null)
            {
                string msiPath = parsedAasFile["MSI Path"].ToString();
                JObject assessedMsiPath = FileSystem.InvestigatePath(msiPath);
                if (assessedMsiPath.HasValues)
                {
                    parsedAasFile["MSI Path"] = assessedMsiPath;
                    if ((int) assessedMsiPath["InterestLevel"] > interestLevel)
                    {
                        interestLevel = (int) assessedMsiPath["InterestLevel"];
                    }
                }
            }

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                return parsedAasFile;
            }
            else
            {
                return null;
            }
        }
    }
}