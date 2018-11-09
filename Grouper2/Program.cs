using System;
using System.IO;
using System.Collections.Generic;

namespace Grouper2
{
    
    class Program
    {
        static void Main(string[] args)
        {

            // All this is a placeholder until I put something in to talk LDAP and SMB
            string[] policyPaths = Directory.GetFiles(@"Z:\\Grouper2\\Grouper2\\bin\\Debug\\TestPolicies");

            IDictionary<string, string> policies = new Dictionary<string, string>
            {
                { "First", "{31B2F340-016D-11D2-945F-00C04FB984F9}" },
                { "Second", "{36FA6290-EFEE-4909-845A-4A33A04D3088}" },
                { "Third", "{6AC1786C-016F-11D2-945F-00C04fB984F9}" }
                //{ "Fourth", "{F1EB7588-E641-4676-B2A5-C706B671368A}" }
            };
                                  
            foreach (KeyValuePair<string, string> policy in policies)
            {
                // Write out the name and path of the policy
                string fullPolicyPath = "Z:\\Grouper2\\Grouper2\\bin\\Debug\\TestPolicies\\" + policy.Value;
                Utility.WriteColor(("Policy Name = " + policy.Key), ConsoleColor.White, ConsoleColor.DarkBlue);
                Console.WriteLine("Policy Path: " + fullPolicyPath);

                string MachinePolPath = fullPolicyPath + "\\Machine";
                string UserPolPath = fullPolicyPath + "\\User";

                // start processing files
                ProcessInf(MachinePolPath);
                //ProcessXml(MachinePolPath);
                ProcessInf(UserPolPath);
                //ProcessXml(UserPolPath);

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

                

            }
        }

        static void ProcessInf(string Path)
        {
            // Only looking at GptTmpl files here
            string[] GptTmplInfFiles = Directory.GetFiles(Path, "GptTmpl.inf", SearchOption.AllDirectories);

            Utility.WriteColor("Found a GptTmpl.inf file. Parsing...", ConsoleColor.White, ConsoleColor.DarkBlue);

            foreach (string infFile in GptTmplInfFiles)
            {
                ParsedInf ParsedInfFile = Parsers.ParseInf(infFile);
                Assess.AssessGPTmpl(ParsedInfFile);
            }
        }

        static void ProcessXml(string Path)
        {
            string[] XmlFiles = Directory.GetFiles(Path, "*.xml", SearchOption.AllDirectories);

            Utility.WriteColor("Found some XML files named:", ConsoleColor.White, ConsoleColor.DarkBlue);

            foreach (string XmlFile in XmlFiles)
            {
                string xmlRelPath = XmlFile.Split('}')[1];
                Console.WriteLine(xmlRelPath);
                //ParsedXml ParsedXmlFile = Parsers.ParseXml(XmlFile);
                //Assess.AssessXml(ParsedXmlFile);
            }


        }

    }

}