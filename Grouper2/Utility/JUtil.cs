using Newtonsoft.Json.Linq;
using System;

namespace Grouper2.Utility
{
    class JUtil
    {

        public static JProperty GetSafeJProp(string propName, JToken inToken, string inString)
        {
            if (inToken[inString] != null)
            {
                if (!string.IsNullOrEmpty(inToken[inString].ToString()))
                {
                    JProperty safeJProp = new JProperty(propName, inToken[inString].ToString());
                    return safeJProp;
                }
            }
            return null;
        }

        public static string GetSafeString(JToken json, string inString)
        {
            string stringOut;
            try
            {
                stringOut = json[inString].ToString();
            }
            catch (NullReferenceException)
            {
                stringOut = "";
            }
            return stringOut;
        }

        public static string GetActionString(string actionChar)
            // shut up, i know it's not really a char.
        {
            string actionString;

            switch (actionChar)
            {
                case "U":
                    actionString = "Update";
                    break;
                case "A":
                    actionString = "Add";
                    break;
                case "D":
                    actionString = "Delete";
                    break;
                case "C":
                    actionString = "Create";
                    break;
                case "R":
                    actionString = "Replace";
                    break;
                default:
                    Utility.Output.DebugWrite("oh no this is new");
                    Utility.Output.DebugWrite(actionChar);
                    actionString = "Broken";
                    break;
            }

            return actionString;
        }
    }
}
