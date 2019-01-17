using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Sddl.Parser
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
                parsedAcl.Add("Flags", JArray.FromObject(Flags));
            }

            if (anyAces)
            {
                foreach (Ace ace in Aces)
                {
                    JObject parsedAce = new JObject();


                    JArray aceFlagsJArray = JArray.FromObject(ace.AceFlags);
                    string aceSidAlias = ace.AceSid.Alias;
                    string aceSidRaw = ace.AceSid.Raw;
                    string aceType = "";
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

                    parsedAce.Add("Rights", aceRights);
                    parsedAce.Add("Flags", aceFlagsJArray);

                    parsedAcl.Add((aceType + " - " + aceSidAlias + " - " + aceSidRaw), parsedAce);
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