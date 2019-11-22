using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Grouper2.Auditor;
using Grouper2.Host.SysVol;
using Grouper2.Host.SysVol.Files;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class GrouperPlan
    {
        private void AuditGpo(Gpo gpo, ref ConcurrentBag<string> taskErrors)
        {
            if (gpo == null) 
                throw new ArgumentNullException(nameof(gpo));
            try
            {
                Auditor.AuditGpo(gpo);
            }
            catch (Exception e)
            {
                taskErrors.Add($"TASK ERROR - GPO: {gpo.Uid} ERROR MSG: {e}");
            }
        }

        private void AuditFile(string gpoUid, DaclProvider file, ref ConcurrentBag<string> taskErrors, SysvolObjectType gpoSubdirObjectType)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            try
            {
                Auditor.AuditFile(gpoUid, file, gpoSubdirObjectType);
            }
            catch (Exception e)
            {
                taskErrors.Add($"TASK ERROR - FILE: {file.Path} ERROR MSG: {e}");
            }
        }
    }
}