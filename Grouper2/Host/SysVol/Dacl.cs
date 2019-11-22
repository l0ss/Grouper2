using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Grouper2.Host.DcConnection;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Host.SysVol.Files
{
    /// <summary>
    /// This is the most base class for data structures in sysvol
    /// </summary>
    public abstract class DaclProvider : IDaclProvider
    {
        // place to store the data for Dacls() method from the interface so it's not done twice
        protected Dictionary<string, Dacl> _dacls;

        public string Path { get; set; }
        public FileType FileSubType { get; set; }
        public SysvolObjectType Type { get; set; }
        public SysvolMajorType MajorType { get; set; }

        /// <summary>
        /// Get a list of Dacls associated with the file, filtered based on requested interest level
        /// </summary>
        /// <param name="ldap">The network interface responsible for doing network transactions</param>
        /// <param name="desiredInterestLevel">The interest level desired</param>
        /// <returns>A list of DACL objects associated with the items in the path of the current file</returns>
        public abstract Dictionary<string, Dacl> Dacls(Ldap ldap, int desiredInterestLevel);
    }

    public interface IDaclProvider
    {
        /// <summary>
        /// Get a list of Dacls associated with the file, filtered based on requested interest level
        /// </summary>
        /// <param name="ldap">The network interface responsible for doing network transactions</param>
        /// <param name="desiredInterestLevel">The interest level desired</param>
        /// <returns>A list of DACL objects associated with the items in the path of the current file</returns>
        Dictionary<string, Dacl> Dacls(Ldap ldap, int desiredInterestLevel);
    }

    public class Dacl
    {
        public int Index { get; }
        public string AccessControlType { get; }
        public string DisplayName { get; }
        public bool Inherited { get; }
        public string Rights { get; }

        public Dacl(int index, string accessControlType, string displayName, bool inherited, string rights)
        {
            Index = index;
            AccessControlType = accessControlType;
            DisplayName = displayName;
            Inherited = inherited;
            Rights = rights;
        }
    }
}