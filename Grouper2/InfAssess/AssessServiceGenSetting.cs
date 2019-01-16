using Grouper2;
using Newtonsoft.Json.Linq;

internal static partial class AssessInf
{
    public static JObject AssessServiceGenSetting(JToken svcGenSetting)
    {
        // this bit kind of works backwards on the placeholders. Fix as you fill these out.
        int interestLevel = 0;
        JObject svcGenSettingJson = new JObject();
        if (interestLevel >= GlobalVar.IntLevelToShow)
        {
            //Utility.DebugWrite(svcGenSetting.ToString());
            svcGenSettingJson = (JObject) JToken.FromObject(svcGenSetting);
        }

        return svcGenSettingJson;
    }
}