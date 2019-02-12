using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Grouper2.SddlParser
{
    public class Sddl
    {
        public string Raw { get; }

        public Sid Owner { get; }
        public Sid Group { get; }
        public Acl Dacl { get; }
        public Acl Sacl { get; }

        public Sddl(string sddl, SecurableObjectType type = SecurableObjectType.Unknown)
        {
            Raw = sddl;

            Dictionary<char, string> components = new Dictionary<char, string>();

            int i = 0;
            int idx = 0;
            int len = 0;

            while (i != -1)
            {
                i = sddl.IndexOf(DelimiterToken, idx + 1);

                if (idx > 0)
                {
                    len = i > 0
                        ? i - idx - 2
                        : sddl.Length - (idx + 1);
                    components.Add(sddl[idx - 1], sddl.Substring(idx + 1, len));
                }

                idx = i;
            }

            if (components.TryGetValue(OwnerToken, out string owner))
            {
                Owner = new Sid(owner);
                components.Remove(OwnerToken);
            }

            if (components.TryGetValue(GroupToken, out string group))
            {
                Group = new Sid(group);
                components.Remove(GroupToken);
            }

            if (components.TryGetValue(DaclToken, out string dacl))
            {
                Dacl = new Acl(dacl, type);
                components.Remove(DaclToken);
            }

            if (components.TryGetValue(SaclToken, out string sacl))
            {
                Sacl = new Acl(sacl, type);
                components.Remove(SaclToken);
            }

            if (components.Any())
            {
                Utility.Output.DebugWrite("encountered some weird extra data in Sddl.Parse");
                Utility.Output.DebugWrite(components.ToString());
            }
        }

        internal const char DelimiterToken = ':';
        internal const char OwnerToken = 'O';
        internal const char GroupToken = 'G';
        internal const char DaclToken = 'D';
        internal const char SaclToken = 'S';

        public JObject ToJObject()
        {
            JObject parsedSddl = new JObject();

            if (Owner != null)
            {
                parsedSddl.Add("Owner", Owner.Alias);
            }

            if (Group != null)
            {
                parsedSddl.Add("Group", Group.Alias);
            }

            if (Dacl != null)
            {
                parsedSddl.Add("DACL", Dacl.ToJObject());
            }
            /*
            if (Sacl != null)
            {
                parsedSDDL.Add("SACL", Sacl.ToString());
            }*/

            return parsedSddl;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (Owner != null)
                sb.AppendLineEnv($"{nameof(Owner)}: {Owner.ToString()}");
            
            if (Group != null)
                sb.AppendLineEnv($"{nameof(Group)}: {Group.ToString()}");

            if (Dacl != null)
            {
                sb.AppendLineEnv($"{nameof(Dacl)}:");
                sb.AppendIndentEnv(Dacl.ToString());
            }

            if (Sacl != null)
            {
                sb.AppendLineEnv($"{nameof(Sacl)}:");
                sb.AppendIndentEnv(Sacl.ToString());
            }

            return sb.ToString();
        }
    }
}