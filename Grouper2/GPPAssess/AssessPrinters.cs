using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedPrinters(JObject gppCategory)
        {
            // dont forget cpasswords
            int interestLevel = 0;
            JProperty gppSharedPrintersProp = new JProperty("SharedPrinter", gppCategory["SharedPrinter"]);
            JObject assessedGppSharedPrinters = new JObject(gppSharedPrintersProp);
            if (interestLevel < GlobalVar.IntLevelToShow)
            {
                assessedGppSharedPrinters = new JObject();
            }

            return assessedGppSharedPrinters;
        }
    }
}