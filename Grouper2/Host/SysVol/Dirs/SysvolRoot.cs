namespace Grouper2.Host.SysVol
{
    public class SysvolRoot : SysvolDirectory
    {
        public SysvolRoot(string path) : base(path, SysvolObjectType.RootFolder)
        {
        }
    }
}