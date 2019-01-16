using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessKerbPolicy(JToken kerbPolicy)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 0;
        JObject kerbPolicyJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            kerbPolicyJson = (JObject) JToken.FromObject(kerbPolicy);
        }

        return kerbPolicyJson;
    }
}