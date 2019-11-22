namespace Grouper2.Host.SysVol
{
    public class UserDirectory : SysvolDirectory
    {
        public UserDirectory(string location) : base(location, SysvolObjectType.UserDirectory)
        {
        }
    }
}
