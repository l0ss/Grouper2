using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessSysAccess(JToken sysAccess)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 0;
        JObject sysAccessJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            sysAccessJson = (JObject) JToken.FromObject(sysAccess);
        }

        return sysAccessJson;
    }
}