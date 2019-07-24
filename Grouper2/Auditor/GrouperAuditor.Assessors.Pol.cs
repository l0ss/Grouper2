using System;
using System.Collections.Generic;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        public Finding Audit(PolSysvolFile file)
        {
            // attempt a file read and parse
            DotPolFileContents data;
            try
            {
                data = file.GetFileData();
            }
            catch (Exception e)
            {
                Log.Degub("Failed to read/parse a pol file");
                data = null;
            }

            AuditedDotPolFile audit = AuditDotPolFile(data);
            return audit;
        }

        private AuditedDotPolFile AuditDotPolFile(DotPolFileContents polData)
        {
            if (polData == null || polData.Entries.Count == 0)
                return null;
            
            // TODO: parse the registry entries for interesting shit
            
            return new AuditedDotPolFile();
        }
    }
}