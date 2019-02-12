using Grouper2.SddlParser;
using Newtonsoft.Json.Linq;

namespace Grouper2
{

    public class ParseSddl
    {
        public static JObject ParseSddlString(string rawSddl, SecurableObjectType type)
        {
            Sddl sddl = new Sddl(rawSddl, type);

            JObject sddlJObject = sddl.ToJObject();

            return sddlJObject;
        }
    }
}