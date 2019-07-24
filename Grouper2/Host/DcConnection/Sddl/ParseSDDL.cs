namespace Grouper2.Host.DcConnection.Sddl
{

    public class ParseSddl
    {
        public static Sddl ParseSddlString(string rawSddl, SecurableObjectType type)
        {
            return new Sddl(rawSddl, type);
        }
    }
}