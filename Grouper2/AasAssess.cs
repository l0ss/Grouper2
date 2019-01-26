using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public class AasAssess
    {
        public static JObject AssessAasFile(JObject parsedAasFile)
        {
            // for now it just passes these through
            // TODO
            JObject assessedAasFile = parsedAasFile;

            return assessedAasFile;
        }
    }
}