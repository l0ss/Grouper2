using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessSysAccess(JToken sysAccess)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 0;
        JObject sysAccessJson = JObject.FromObject(sysAccess);
        if (interestLevel <= GlobalVar.IntLevelToShow)
        {
            sysAccessJson = null;
        }

        return sysAccessJson;
    }
}