using System;
using System.IO;
using System.Linq;
using Grouper2.Host.DcConnection;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Host.SysVol
{
    public partial class Gpo : SysvolDirectory
    {
        private string _uid;

        public string Uid
        {
            get
            {
                // lazy get this to reduce strops at runtime because it may not always used
                if (string.IsNullOrEmpty(_uid))
                {
                    _uid = Gpo.UidFromPath(this.Path);
                }

                return _uid;
            }
            internal set => _uid = value;
        }

        public Gpo(string location) : base(location, SysvolObjectType.GpoDirectory)
        {
            this.Type = SysvolObjectType.GpoDirectory;
        }


        /// <summary>
        /// Returns the first encountered UID in a path if it exists
        /// </summary>
        /// <param name="path">A path with a UID enclosed by squiggly dudes {}</param>
        /// <returns>null if no UID, UID in string form if it exists</returns>
        public static string UidFromPath(string path)
        {
            // if there is no UID to return, return a null
            return !path.Contains('{') ? null : path.Split('{', '}')[1];
        }
    }
}
