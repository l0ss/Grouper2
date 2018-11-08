using System;
using System.IO;
using System.Collections.Generic;

namespace Grouper2
{
    
    class Program
    {
        static void Main(string[] args)
        {

            // All this is a placeholder until i put something in to talk LDAP and SMB
            string[] policyPaths = Directory.GetFiles(@"C:\\Users\\mike.TESTING2016\\Desktop\\Policies");

            IDictionary<string, string> policies = new Dictionary<string, string>
            {
                { "First", "{31B2F340-016D-11D2-945F-00C04FB984F9}" },
                { "Second", "{36FA6290-EFEE-4909-845A-4A33A04D3088}" },
                { "Third", "{6AC1786C-016F-11D2-945F-00C04fB984F9}" }
                //{ "Fourth", "{F1EB7588-E641-4676-B2A5-C706B671368A}" }
            };

            //string PolData = File.ReadAllText("PolData.json");

            foreach (KeyValuePair<string, string> policy in policies)
            {
                // Write out the name and path of the policy
                string fullPolicyPath = "C:\\Users\\mike.TESTING2016\\Desktop\\Policies\\" + policy.Value;
                Utility.WriteColor(("Policy Name = " + policy.Key), ConsoleColor.White, ConsoleColor.DarkBlue);
                Console.WriteLine("Policy Path: " + fullPolicyPath);


                // Only looking at GptTmpl files here
                string[] GptTmplInfFiles = Directory.GetFiles(fullPolicyPath, "GptTmpl.inf", SearchOption.AllDirectories);

                Utility.WriteColor("Found these GptTmpl.inf files: ", ConsoleColor.White, ConsoleColor.DarkBlue);
                foreach (string infFile in GptTmplInfFiles)
                {
                    Console.WriteLine(infFile);
                }

                foreach (string infFile in GptTmplInfFiles)
                {
                    ParsedInf ParsedInfFile = Parsers.ParseInf(infFile);
                    Assess.AssessGPTmpl(ParsedInfFile);
                }

                //TODO
                //Parse other inf sections:
                //  System Access
                //  Kerberos Policy
                //  Event Audit
                //  Registry Values
                //  Registry Keys
                //  Group Membership
                //  Service General Setting
                //Parse XML files
                //Parse ini files
                //Grep scripts for creds.

            };
        }

    }
}