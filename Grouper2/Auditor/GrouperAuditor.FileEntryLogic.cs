using System;
using System.Collections.Concurrent;
using System.IO;
using Grouper2.Host.DcConnection;
using Grouper2.Host.SysVol;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public class AuditFileResult
    {
        public string ParentGpoUid { get; set; }
        public SysvolObjectType MachineOrUser { get; set; }
        public Finding FileFinding { get; set; }
    }

    public partial class GrouperAuditor
    {
        
        internal void AuditFile(string parent, DaclProvider file, SysvolObjectType gpoSubType)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            if (file.Type == SysvolObjectType.UselessFluffFile || file.FileSubType == FileType.Unwanted)
            {
                return;
            }
            Finding finding = null;
            switch (file)
            {
                case XmlSysvolFile xmlFile:
                    finding = Audit(xmlFile);
                    break;
                case IniSysvolFile iniFile:
                    finding = Audit(iniFile);
                    break;
                case InfSysvolFile infFile:
                    finding = Audit(infFile);
                    break;
                case PolSysvolFile polFile:
                    finding = Audit(polFile);
                    break;
                case Script script:
                    AuditScript(script);
                    break;
            }

            if (finding != null)
            {
                ConcurrentFindings.Add(new AuditFileResult()
                {
                    FileFinding = finding,
                    MachineOrUser = gpoSubType,
                    ParentGpoUid = parent
                });
            }
        }

        private void AuditScript(Script script)
        {
            Scripts.Add(script.Audit(JankyDb.Vars.Interest));
        }
    }
}