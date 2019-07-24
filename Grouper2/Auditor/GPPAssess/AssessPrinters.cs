using System;
using System.Runtime.CompilerServices;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private Finding Audit(Printers file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedPrinters(file.JankyXmlStuff);
        }
        private AuditedGppXmlPrinters GetAssessedPrinters(JObject gppCategory)
        {
            JToken portPrinters = gppCategory["PortPrinter"];
            
            JToken sharedPrinters = gppCategory["SharedPrinters"];

            JToken[] gppPrinterTypes = new JToken[] { portPrinters, sharedPrinters};

            AuditedGppXmlPrinters assessedGppPrinters = new AuditedGppXmlPrinters();

            foreach (JToken printerType in gppPrinterTypes)
            {
                if (printerType is JArray)
                {
                    foreach (JToken gppPrinter in printerType)
                    {
                        AuditedGppXmlPrintersPrinter assessedGppPrinter = AssessGppPrinter(gppPrinter);
                        if (assessedGppPrinter != null)
                        {
                            assessedGppPrinters.Printers.Add(assessedGppPrinter.Uid,assessedGppPrinter);
                        }
                    }
                }
                else if (printerType != null)
                {
                    AuditedGppXmlPrintersPrinter assessedGppPrinter = AssessGppPrinter(printerType);
                    assessedGppPrinters.Printers.Add(assessedGppPrinter.Uid,assessedGppPrinter);
                }
            }
            
            // return if has value
            return assessedGppPrinters.Printers.Count > 0 ? assessedGppPrinters : null;

        }

        private AuditedGppXmlPrintersPrinter AssessGppPrinter(JToken gppPrinter)
        {
            JToken gppPrinterProps = gppPrinter["Properties"];

            AuditedGppXmlPrintersPrinter ret = new AuditedGppXmlPrintersPrinter()
            {
                Interest = 1,
                Uid = JUtil.GetSafeString(gppPrinter, "@uid"),
                Name = JUtil.GetSafeString(gppPrinter, "@name"),
                Action = JUtil.GetActionString(gppPrinterProps["@action"].ToString()),
                Changed = JUtil.GetSafeString(gppPrinter, "@changed"),
                Username = JUtil.GetSafeString(gppPrinterProps, "@username"),
                LocalName = JUtil.GetSafeString(gppPrinterProps, "@localName"),
                Address = JUtil.GetSafeString(gppPrinterProps, "@ipAddress"),
                SnmpString = JUtil.GetSafeString(gppPrinterProps, "@snmpCommunity"),
                Path = FileSystem.InvestigatePath(JUtil.GetSafeString(gppPrinterProps, "@path")),
                Comment = FileSystem.InvestigateString(JUtil.GetSafeString(gppPrinterProps, "@comment"), this.InterestLevel),
                Location = FileSystem.InvestigateString(JUtil.GetSafeString(gppPrinterProps, "@location"), this.InterestLevel),
                CPassword = JUtil.GetSafeString(gppPrinterProps, "@cpassword"),
            };
            if (!string.IsNullOrWhiteSpace(ret.CPassword))
            {
                ret.CPasswordDecrypted = Util.DecryptCpassword(ret.CPassword);
                ret.Interest = 10;
            }

            // check each of our potentially interesting values to see if it raises our overall interest level
            if (!string.IsNullOrWhiteSpace(ret.SnmpString)) ret.TryBumpInterest(7);
            ret.TryBumpInterest(ret.Path);
            ret.TryBumpInterest(ret.Location);
            ret.TryBumpInterest(ret.Comment);

            // return if it is interesting enough
            return ret.Interest >= this.InterestLevel ? ret : null;
        }
    }
}