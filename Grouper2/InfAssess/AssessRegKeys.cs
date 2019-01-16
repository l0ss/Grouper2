using System.Runtime.Remoting.Channels;
using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessRegKeys(JToken regKeys)
    {
        // These are actually ACLs being set on reg keys using SDDL.

        // The first value is inheritance rules:

        // 2= replace existing permissions on all subkeys with inheritable permissions
        // 1= Do not allow permissions on this key to be replace.
        // 0= Propagate inheritable permissions to all subkeys.


        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 0;
        JObject regKeysJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            regKeys = (JObject) JToken.FromObject(regKeys);
        }

        return regKeysJson;
    }
}