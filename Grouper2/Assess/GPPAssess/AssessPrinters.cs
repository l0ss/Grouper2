using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        private JObject GetAssessedPrinters(JObject gppCategory)
        {
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
                        if (assessedGppPrinter != null)
                        {
                            assessedGppPrinters.Add(assessedGppPrinter);
                        }
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
            string gppPrinterUid = JUtil.GetSafeString(gppPrinter, "@uid");
            string gppPrinterName = JUtil.GetSafeString(gppPrinter, "@name");
            string gppPrinterChanged = JUtil.GetSafeString(gppPrinter, "@changed");
            JToken gppPrinterProps = gppPrinter["Properties"];
            string gppPrinterAction = JUtil.GetActionString(gppPrinterProps["@action"].ToString());
            string gppPrinterUsername = JUtil.GetSafeString(gppPrinterProps, "@username");
            string gppPrintercPassword = JUtil.GetSafeString(gppPrinterProps, "@cpassword");
            string gppPrinterPassword = "";
            if (gppPrintercPassword.Length > 0)
            {
                gppPrinterPassword = Util.DecryptCpassword(gppPrintercPassword);
                interestLevel = 10;
            }
            string gppPrinterAddress = JUtil.GetSafeString(gppPrinterProps, "@ipAddress");
            string gppPrinterLocalName = JUtil.GetSafeString(gppPrinterProps, "@localName");
            string gppPrinterSnmpCommString = JUtil.GetSafeString(gppPrinterProps, "@snmpCommunity");
            if (gppPrinterSnmpCommString.Length > 1) interestLevel = 7;
            JToken gppPrinterPath = FileSystem.InvestigatePath(JUtil.GetSafeString(gppPrinterProps, "@path"));
            JToken gppPrinterComment = FileSystem.InvestigateString(JUtil.GetSafeString(gppPrinterProps, "@comment"));
            JToken gppPrinterLocation = FileSystem.InvestigateString(JUtil.GetSafeString(gppPrinterProps, "@location"));

            // check each of our potentially interesting values to see if it raises our overall interest level
            JToken[] valuesWithInterest = { gppPrinterPath, gppPrinterComment, gppPrinterLocation,};
            foreach (JToken val in valuesWithInterest)
            {
                if ((val != null) && (val["InterestLevel"] != null))
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
                    {"Action", gppPrinterAction}
                };
                if (gppPrintercPassword.Length > 0)
                {
                    assessedGppPrinter.Add("Username", gppPrinterUsername);
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