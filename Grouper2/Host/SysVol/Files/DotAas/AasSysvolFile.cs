using System;

namespace Grouper2.Host.SysVol.Files
{
    public class AasSysvolFile : SysvolFile
    {
        public AasSysvolFile(string path) : base(path)
        {
            this.Type = SysvolObjectType.AasFile;
        }
    }
}