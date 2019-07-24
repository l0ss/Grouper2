namespace Grouper2.Host.DcConnection.Sddl
{
    internal static class Format
    {
        public static string Unknown(string input)
        {
            const string unknownString = "Unknown({0})";
            return string.Format(unknownString, input);
        }
    }
}
