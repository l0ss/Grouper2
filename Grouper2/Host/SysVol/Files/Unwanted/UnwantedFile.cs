using System.Collections.Generic;
using Grouper2.Host.DcConnection;

namespace Grouper2.Host.SysVol.Files.Unwanted
{
    public class UnwantedFile : SysvolFile
    {
        public UnwantedFile(string path) : base(path)
        {
            Type = SysvolObjectType.UselessFluffFile;
        }

        public override Dictionary<string, Dacl> Dacls(Ldap ldap, int desiredInterestLevel)
        {
            return null;
        }
    }
}