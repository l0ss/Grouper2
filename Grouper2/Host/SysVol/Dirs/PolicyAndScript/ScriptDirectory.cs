using System;

namespace Grouper2.Host.SysVol
{
    /// <summary>
    /// ScriptDirectory is an abstract representation of directories which probably hold scripts in the sysvol directory
    /// </summary>
    public class ScriptDirectory : SysvolDirectory
    {
        public ScriptDirectory(string path) : base(path, SysvolObjectType.ScriptDirectory)
        {

        }
    }
}
