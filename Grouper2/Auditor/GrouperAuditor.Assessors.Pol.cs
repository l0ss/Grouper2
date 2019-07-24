using System;
using System.Collections.Generic;
using Grouper2.Host.SysVol.Files;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        public Finding Audit(PolSysvolFile file)
        {
            return AuditDotPolFile(file.GetFileData());
        }

        private AuditedDotPolFile AuditDotPolFile(List<RegistryEntry> polData)
        {
            if (polData == null || polData.Count == 0)
                return null;
            
            // TODO: parse the registry entries for interesting shit
            
            return new AuditedDotPolFile();
        }
    }
}