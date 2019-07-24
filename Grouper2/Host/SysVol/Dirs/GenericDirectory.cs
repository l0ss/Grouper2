using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grouper2.Host.DcConnection;
using Grouper2.Host.SysVol.Files;

namespace Grouper2.Host.SysVol.Dirs
{
    public class GenericDirectory : SysvolDirectory
    {
        public GenericDirectory(string path) : base(path, SysvolObjectType.UselessFluffDirectory)
        {
        }

    }
}
