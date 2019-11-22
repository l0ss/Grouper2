using System;
using System.Collections.Generic;
using System.Linq;
using Grouper2.Utility;

namespace Grouper2.Host.SysVol
{
    /// <summary>
    /// PolicyDirectory is an abstract representation of directories which probably hold GPOs in the sysvol directory
    /// </summary>
    public class PolicyDirectory : SysvolDirectory
    {

        /// <summary>
        /// PolicyDirectory instantiation will cause directory reads to list the Subdirectories
        /// </summary>
        public PolicyDirectory(string location) : base(location, SysvolObjectType.PolicyFolder)
        {

        }
    }
}
