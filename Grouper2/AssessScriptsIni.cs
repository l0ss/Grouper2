using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class AssessScriptsIni
    {
        public static JObject GetAssessedScriptsIni(JObject parsedScriptsIni)
        {
            JObject assessedScriptsIni = new JObject();
            int interestLevel = 1;


            // does nothing yet, just passes through.
            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                assessedScriptsIni = parsedScriptsIni;
            }
            
            return assessedScriptsIni;
        }
    }
}
