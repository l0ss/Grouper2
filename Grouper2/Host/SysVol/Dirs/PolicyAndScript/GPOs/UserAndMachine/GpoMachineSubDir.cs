namespace Grouper2.Host.SysVol
{
    public class MachineDirectory : SysvolDirectory
    {

        public MachineDirectory(string location) : base(location, SysvolObjectType.MachineDirectory)
        {
        }
    }
}
