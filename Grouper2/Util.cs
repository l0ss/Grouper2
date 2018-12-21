using Grouper2;

static internal class Util
{
    public static string GetActionString(string actionChar)
        // shut up, i know it's not really a char.
    {
        string actionString = "";

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
            default:
                Utility.DebugWrite("oh no this is new");
                actionString = "Broken";
                break;
        }

        return actionString;
    }
}