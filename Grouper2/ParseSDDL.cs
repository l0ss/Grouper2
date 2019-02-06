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

            string[] boringRights = new string[]
            {
                "READ_CONTROL",
                "SYNCHRONIZE",
                "GENERIC_EXECUTE",
                "GENERIC_READ",
                "READ_PROPERTY",
                "LIST_CHILDREN",
                "LIST_OBJECT"
            };

            string[] interestingRights = new string[]
            {
                "KEY_ALL",
                "DELETE_TREE",
                "STANDARD_DELETE",
                "CREATE_CHILD",
                "DELETE_CHILD",
                "WRITE_PROPERTY",
                "GENERIC_ALL",
                "GENERIC_WRITE",
                "WRITE_DAC",
                "WRITE_OWNER",
                "STANDARD_RIGHTS_ALL",
                "STANDARD_RIGHTS_REQUIRED",
                "CONTROL_ACCESS",
                "SELF_WRITE"
            };

            Utility.DebugWrite(sddlJObject.ToString());

            return sddlJObject;
            //return new JObject();
        }
    }
}