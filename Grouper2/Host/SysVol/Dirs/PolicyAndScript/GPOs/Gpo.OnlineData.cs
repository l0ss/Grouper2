using System;
using System.Collections.Generic;
using Grouper2.Host.DcConnection;

namespace Grouper2.Host.SysVol
{
    public partial class Gpo : SysvolDirectory
    {
        public Gpo() : base("LDAP", SysvolObjectType.GpoDirectory)
        {
            this.GpoPackages = new List<GpoPackage>();
        }
        // These are the portions of the GPO which can be filled out by the DC via LDAP

        public List<GpoPackage> GpoPackages { get; set; }
        public DcConnection.Sddl.Sddl GpoAcls { get; set; }
        public string DisplayName { get; set; }
        public string DistinguishedName { get; set; }
        public string Created { get; set; }
        public string GpoStatus { get; set; }

    }
}
