using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Grouper2
{
    class Assess
    {

        //remove once json parsing is in
        static Dictionary<String, String> intPrivRights = new Dictionary<string, string>
        {
            { "SeTrustedCredManAccessPrivilege","Description of this Privilege" },
            { "SeTcbPrivilege","Description of this Privilege" },
            { "SeMachineAccountPrivilege","Description of this Privilege" },
            { "SeBackupPrivilege","Description of this Privilege" },
            { "SeCreateTokenPrivilege","Description of this Privilege" },
            { "SeAssignPrimaryTokenPrivilege","Description of this Privilege" },
            { "SeRestorePrivilege","Description of this Privilege" },
            { "SeDebugPrivilege","Description of this Privilege" },
            { "SeTakeOwnershipPrivilege","Description of this Privilege" },
            { "SeLoadDriverPrivilege","Description of this Privilege" },
            { "SeRemoteInteractiveLogonRight","Description of this Privilege" }
        };


        public static void AssessGPTmpl(ParsedInf InfToAssess)
        {
            // Checks for interesting priv assignments.
            if (InfToAssess.ContainsKey("[Privilege Rights]")) {
                Dictionary<string, string[]> privRights = InfToAssess["[Privilege Rights]"];
                //look at each value
                foreach (KeyValuePair<string, string[]> privRight in privRights)
                {
                    if (intPrivRights.Keys.Contains(privRight.Key))
                    {
                        Console.WriteLine("Interesting privilege " + privRight.Key + " is granted to:");
                        Utility.PrintIndexAndValues(privRight.Value);
                    }
                }
            }

            if (InfToAssess.ContainsKey("[Registry Values]")) {
                Dictionary<string, string[]> RegVals = InfToAssess["[Registry Values]"];
                foreach (KeyValuePair<string, string[]> RegVal in RegVals)
                {
                    string PrintKey = RegVal.Key;
                    Utility.DebugWrite("Key: ");
                    Console.WriteLine(PrintKey);
                    Utility.DebugWrite("Values: ");
                    // the first value in these looks like a 'type' code.
                    // looks like they work like this:
                    // 4 = Int, but where it's 1 or 0 they use it as a bool
                    // 1 = String in double quotes, some of which are numbers
                    // 7 = Array
                    foreach (string value in RegVals[RegVal.Key])
                    {
                        Console.WriteLine(value);
                    }
                }
            }

            /* skeleton to just read out all the values
            if (InfToAssess.ContainsKey("[Registry Values]"))
            {
                Dictionary<string, string[]> RegVals = InfToAssess["[Registry Values]"];
                foreach (KeyValuePair<string, string[]> RegVal in RegVals)
                {
                    string PrintKey = RegVal.Key;
                    Utility.DebugWrite("Key: ");
                    Console.WriteLine(PrintKey);
                    Utility.DebugWrite("Values: ");
                    foreach (string value in RegVals[RegVal.Key])
                    {
                        Console.WriteLine(value);
                    }
                }
            }*/

            //catch any stuff that falls through the cracks
            string[] KeysInInf = (InfToAssess.Keys.ToArray());

            string[] KnownKeys = new string[]
            {
                "[Privilege Rights]",
                "[Unicode]",
                "[Version]"
            };

            //  System Access +
            //  Kerberos Policy -
            //  Event Audit -
            //  Registry Values +
            //  Registry Keys |
            //  Group Membership +
            //  Service General Setting +


            var SlippedThrough = KeysInInf.Except(KnownKeys);

            if (SlippedThrough.Count() > 0)
            {
                Utility.DebugWrite("We didn't parse any of these sections:");
                foreach (var UnparsedHeader in SlippedThrough)
                {
                    Console.WriteLine(UnparsedHeader);
                }
            }
        }
    }
}