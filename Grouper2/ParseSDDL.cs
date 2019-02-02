using System;
using Newtonsoft.Json.Linq;
using Sddl.Parser;

namespace Grouper2
{
    
    public class ParseSDDL
    {
        public static JObject ParseSddlString(string rawSddl, SecurableObjectType type)
        {
            var sddl = new Sddl.Parser.Sddl(rawSddl, type);
            return sddl.ToJObject();
            //return new JObject();
        }
    }
}