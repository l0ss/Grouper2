using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;

namespace Grouper2.Host.SysVol
{
    /// <summary>
    /// Sysvol is an abstract representation of the sysvol directory
    /// </summary>
    public partial class Sysvol
    {
        public string Location { get; private set; }
        public SysvolMapper.TreeNode<DaclProvider> map { get; set; }

        /// <summary>
        /// Init an instance of sysvol based on location and mapping strategy.
        /// Instantiation will cause directory reads on the sysvol filesystem to populate the mapping
        ///
        /// If NoNtfrs is set, Only top level Policy and Script directories are mapped
        /// </summary>
        protected Sysvol(string location, bool noNtfrs, bool noScripts)
        {
            // read in the location
            this.Location = location;
            this.map = SysvolMapper.MapSysvol(location, noNtfrs, noScripts);

        }
    }

    
    
}
