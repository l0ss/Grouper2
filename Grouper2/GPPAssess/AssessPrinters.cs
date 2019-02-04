using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        private JObject GetAssessedPrinters(JObject gppCategory)
        {
            //Utility.DebugWrite(gppCategory.ToString());
            
            JToken portPrinters = gppCategory["PortPrinter"];
            
            JToken sharedPrinters = gppCategory["SharedPrinters"];

            JToken[] gppPrinterTypes = new JToken[] { portPrinters, sharedPrinters};

            JObject assessedGppPrinters = new JObject();

            foreach (JToken printerType in gppPrinterTypes)
            {
                if (printerType is JArray)
                {
                    foreach (JToken gppPrinter in printerType)
                    {
                        JProperty assessedGppPrinter = AssessGppPrinter(gppPrinter);
                        assessedGppPrinters.Add(assessedGppPrinter);
                    }
                }
                else if (printerType != null)
                {
                    JProperty assessedGppPrinter = AssessGppPrinter(printerType);
                    assessedGppPrinters.Add(assessedGppPrinter);
                }
            }
            
            if (assessedGppPrinters.HasValues)
            {
                return assessedGppPrinters;
            }
            else
            {
                return null;
            }

        }

        static JProperty AssessGppPrinter(JToken gppPrinter)
        {
            int interestLevel = 1;
            string gppPrinterUid = Utility.GetSafeString(gppPrinter, "@uid");
            string gppPrinterName = Utility.GetSafeString(gppPrinter, "@name");
            string gppPrinterChanged = Utility.GetSafeString(gppPrinter, "@changed");
            JToken gppPrinterProps = gppPrinter["Properties"];
            string gppPrinterAction = Utility.GetActionString(gppPrinterProps["@action"].ToString());
            string gppPrinterUsername = Utility.GetSafeString(gppPrinterProps, "@username");
            string gppPrintercPassword = Utility.GetSafeString(gppPrinterProps, "@cpassword");
            string gppPrinterPassword = "";
            if (gppPrintercPassword.Length > 0)
            {
                gppPrinterPassword = Utility.DecryptCpassword(gppPrintercPassword);
                interestLevel = 10;
            }
            string gppPrinterAddress = Utility.GetSafeString(gppPrinterProps, "@ipAddress");
            string gppPrinterLocalName = Utility.GetSafeString(gppPrinterProps, "@localName");
            string gppPrinterSnmpCommString = Utility.GetSafeString(gppPrinterProps, "@snmpCommunity");
            if (gppPrinterSnmpCommString.Length > 1) interestLevel = 7;
            JToken gppPrinterPath = FileSystem.InvestigatePath(Utility.GetSafeString(gppPrinterProps, "@path"));
            JToken gppPrinterComment = Utility.InvestigateString(Utility.GetSafeString(gppPrinterProps, "@comment"));
            JToken gppPrinterLocation = Utility.InvestigateString(Utility.GetSafeString(gppPrinterProps, "@location"));

            // check each of our potentially interesting values to see if it raises our overall interest level
            JToken[] valuesWithInterest = { gppPrinterPath, gppPrinterComment, gppPrinterLocation,};
            foreach (JToken val in valuesWithInterest)
            {
                if (val["InterestLevel"] != null)
                {
                    int valInterestLevel = int.Parse(val["InterestLevel"].ToString());
                    if (valInterestLevel > interestLevel)
                    {
                        interestLevel = valInterestLevel;
                    }
                }
            }

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                JObject assessedGppPrinter = new JObject
                {
                    {"Name", gppPrinterName},
                    {"Changed", gppPrinterChanged},
                    {"Action", gppPrinterAction},
                    {"Username", gppPrinterUsername}
                };
                if (gppPrintercPassword.Length > 0)
                {
                    assessedGppPrinter.Add("cPassword", gppPrintercPassword);
                    assessedGppPrinter.Add("Decrypted Password", gppPrinterPassword);
                }
                assessedGppPrinter.Add("Local Name", gppPrinterLocalName);
                assessedGppPrinter.Add("Address", gppPrinterAddress);
                assessedGppPrinter.Add("Path", gppPrinterPath);
                assessedGppPrinter.Add("SNMP Community String", gppPrinterSnmpCommString);
                assessedGppPrinter.Add("Comment", gppPrinterComment);
                assessedGppPrinter.Add("Location", gppPrinterLocation);

                return new JProperty(gppPrinterUid, assessedGppPrinter);
            }

            return null;
        }
    }
}