using System;
using System.IO;
using Grouper2.Auditor;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Host.SysVol.Files
{
    public class Script : SysvolFile
    {
        public Script(string location) : base(location)
        {
        }

        public AuditedScript Audit(int desiredInterest)
        {
            AuditedScript investigatedScript = new AuditedScript()
            {
                Name = this.Path
            };

            // get the file info so we can check size
            FileInfo scriptFileInfo = new FileInfo(this.Path);
            // if it's not too big
            if (scriptFileInfo.Length >= 200000) return null;

            // feed the whole thing through Utility.InvestigateFileContents
            investigatedScript.Contents = FileSystem.InvestigateFileContents(this.Path, JankyDb.Vars.Interest);
            // if we got anything good, add the result to processedScripts

            // only return the object if it is of sufficient interest
            return investigatedScript.Contents.Interest >= desiredInterest 
                ? investigatedScript 
                : null;
        }
    }
}