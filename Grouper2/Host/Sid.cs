using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Newtonsoft.Json.Linq;

namespace Grouper2.Host
{
    public enum SidType
    {
        User,
        Group
    }
    [ComVisible(false)]
    public partial class Sid
    {
        public bool DomainSid { get; private set; }
        public string CanonicalPrivLevel { get; private set; } = String.Empty;
        public string ComparisonString { get; private set; }
        public string RelativeId { get; private set; }
        public string DomainOrLocalComputerIdString { get; private set; }
        public string Name { get; private set; }
        public string DistinguishedName { get; private set; }
        public string Description { get; private set; }
        public string Raw { get; private set; }
        public SecurityIdentifier FullSID { get; private set; }
        public  SidType SidType { get; }

        private void DecomposeSidString(string sidString)
        {
            // set the raw val
            this.Raw = sidString;

            // is it a domain sid?
            this.DomainSid = IsDomainSid(sidString);

            // split to decompose
            string[] splitSid = sidString.Split('-');

            // rebuild the "comparison string" part
            if (splitSid.Length >=4)
                this.ComparisonString = $"{splitSid[0]}-{splitSid[1]}-{splitSid[2]}-{splitSid[3]}";

            // get the first segment of the identifier portion of the SID
            if (splitSid.Length > 4)
                this.DomainOrLocalComputerIdString = splitSid[4];

            // the last segment of the sid is the relativeID
            this.RelativeId = splitSid[splitSid.Length - 1];
        }

        public Sid(string sid)
        {
            if (IsShoddilyValidatedSid(sid))
            {
                this.DecomposeSidString(sid);
                this.FullSID = new SecurityIdentifier(sid);
            }
            else
            {
                // wtaf are you doing with your life, sid?
                return;
            }
            
        }

        public Sid(SecurityIdentifier sid)
        {
            this.FullSID = sid;
            this.DecomposeSidString(sid.Value);
        }

        public Sid(SecurityIdentifier sid, string name, string distinguishedName, string description, SidType type)
        {
            this.FullSID = sid;
            this.DecomposeSidString(sid.Value);
            this.Name = name;
            this.DistinguishedName = distinguishedName;
            this.Description = description;
            this.SidType = type;
        }

        public Sid(string sidString, string displayName, string description, string canonicalPrivLevel)
        {
            this.DecomposeSidString(sidString);
            this.Name = displayName;
            this.Description = description;
            this.CanonicalPrivLevel = canonicalPrivLevel;
        }
    }
}