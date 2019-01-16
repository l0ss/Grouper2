using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessRegKeys(JToken regKeys)
    {
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