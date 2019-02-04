using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Grouper2.SddlParser
{
    public class Acl
    {
        public string Raw { get; }
        
        public string[] Flags { get; }
        public Ace[] Aces { get; }

        public Acl(string acl, SecurableObjectType type = SecurableObjectType.Unknown)
        {
            Raw = acl;

            int begin = acl.IndexOf(Ace.BeginToken);

            // Flags
            var flags = begin == -1 ? acl : acl.Substring(0, begin);
            var flagsLabels = Match.ManyByPrefix(flags, SdControlsDict, out var reminder);

            if (reminder != null)
                // ERROR Flags part can not be fully parsed.
                flagsLabels.AddLast(Format.Unknown(reminder));

            Flags = flagsLabels.ToArray();

            // Aces
            if (begin != -1)
            {
                LinkedList<Ace> aces = new LinkedList<Ace>();

                // brackets balance: '(' = +1, ')' = -1
                int balance = 0;
                for (int end = begin; end < acl.Length; end++)
                {
                    if (acl[end] == Ace.BeginToken)
                    {
                        if (balance == 0)
                            begin = end;
                            
                        balance += 1;
                    }
                    else if (acl[end] == Ace.EndToken)
                    {
                        balance -= 1;

                        int length = end - begin - 1;
                        if (length < 0)
                        {
                            // ERROR Ace is empty.
                            continue;
                        }

                        if (balance == 0)
                            aces.AddLast(new Ace(acl.Substring(begin + 1, length), type));
                    }
                    else if (balance <= 0)
                    {
                        // ERROR Acl contains unexpected AceEnd characters.
                        balance = 0;
                    }
                }
                
                Aces = aces.ToArray();
            }
        }

        internal static Dictionary<string, string> SdControlsDict = new Dictionary<string, string>
        {
            { "P", "PROTECTED" },
            { "AR", "AUTO_INHERIT_REQ" },
            { "AI", "AUTO_INHERITED" },
            { "NO_ACCESS_CONTROL", "NULL_ACL" },
        };

        public JObject ToJObject()
        {
            bool anyFlags = Flags != null && Flags.Any();
            bool anyAces = Aces != null && Aces.Any();

            JObject parsedAcl = new JObject();

            if (anyFlags)
            {
                //parsedAcl.Add("Flags", JArray.FromObject(Flags));
            }

            if (anyAces)
            {
                int inc = 0;
                foreach (Ace ace in Aces)
                {
                    JObject parsedAce = new JObject();

                    JArray aceFlagsJArray = new JArray();
                    if (ace.AceFlags != null)
                    {
                        aceFlagsJArray = JArray.FromObject(ace.AceFlags);
                    }
                    
                    string aceSidAlias = ace.AceSid.Alias;
                    string aceSidRaw = ace.AceSid.Raw;
                    string aceType;
                    if (ace.AceType == "ACCESS_ALLOWED")
                    {
                        aceType = "Allow";
                    }
                    else if (ace.AceType == "ACCESS_DENIED")
                    {
                        aceType = "Deny";
                    }
                    else
                    {
                        aceType = ace.AceType;
                    }

                    JArray aceRights = JArray.FromObject(ace.Rights);

                    string displayName = LDAPstuff.GetUserFromSid(aceSidRaw);
                    parsedAce.Add("SID", aceSidRaw);
                    parsedAce.Add("Name", displayName);
                    parsedAce.Add("Type", aceType);
                    if (aceRights.Count > 1)
                    {
                        parsedAce.Add("Rights", aceRights);
                    }
                    else if (aceRights.Count == 1)
                    {
                        parsedAce.Add("Rights", aceRights[0].ToString());
                    }
                    if (aceFlagsJArray.Count > 1)
                    {
                        parsedAce.Add("Flags", aceFlagsJArray);
                    }
                    else if (aceFlagsJArray.Count == 1)
                    {
                        parsedAce.Add("Flags", aceFlagsJArray[0].ToString());
                    }
                    
                    parsedAcl.Add(new JProperty(inc.ToString(), parsedAce));
                    inc++;
                }
            }
            return parsedAcl;
        }

        public override string ToString()
        {
            bool anyFlags = Flags != null && Flags.Any();
            bool anyAces = Aces != null && Aces.Any();
            
            StringBuilder sb = new StringBuilder();

            if (anyFlags)
                sb.AppendLineEnv($"{nameof(Flags)}: {string.Join(", ", Flags)}");

            if (anyAces)
            {
                for (int i = 0; i < Aces.Length; ++i)
                {
                    sb.AppendLineEnv($"Ace[{i:00}]");
                    sb.AppendIndentEnv(Aces[i].ToString());
                }
            }

            return sb.ToString();
        }
    }
}