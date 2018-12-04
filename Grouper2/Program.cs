using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Xml;
using Newtonsoft.Json;

namespace Grouper2
{
    class Program
    {
        static readonly string JsonDataFile = File.ReadAllText("PolData.Json");
        static readonly JObject JsonData = JObject.Parse(JsonDataFile);

        static void Main(string[] args)
        {

            // All this is a placeholder until I put something in to talk LDAP and SMB

            IDictionary<string, string> policies = new Dictionary<string, string>
            {
                { "First", "{31B2F340-016D-11D2-945F-00C04FB984F9}" },
                { "Second", "{36FA6290-EFEE-4909-845A-4A33A04D3088}" },
                { "Third", "{6AC1786C-016F-11D2-945F-00C04fB984F9}" },
                { "Fourth", "{F1EB7588-E641-4676-B2A5-C706B671368A}" }
            };

            // end placeholding.

            string[]GPOPaths = Directory.GetDirectories(@"Z:\Grouper2\Grouper2\bin\Debug\TestPolicies");

            //string OutputPath = @"Z:\Grouper2\Grouper2\bin\Debug";

                        

            foreach (string GPOPath in GPOPaths)
            {
                // Set up the ghetto-struct JObject that we're going to put all our results into:
                JObject GPOOutput = (JObject)JsonData["Output Skeleton"];

                // Get the UID of the GPO from the file path.
                char[] SplitChars = new char[] { '\\' };
                string[] SplitPath = GPOPath.Split(SplitChars);
                string GPOUID = SplitPath[(SplitPath.Length - 1)];

                // Set some properties of the GPO we're looking at in our output file.
                GPOOutput["GPO UID"] = GPOUID;
                GPOOutput["GPO File Path"] = GPOPath;

                //Console.WriteLine(GPOOutput);

                //TODO
                // look up the friendly name of the policy
                // get the policy permissions
                // get the policy owner
                // get whether it's linked and where
                // get whether it's enabled

                // start processing files
                string MachinePolPath = GPOPath + "\\Machine";
                string UserPolPath = GPOPath + "\\User";

                // need to get the shit returned from these
                //Utility.DebugWrite("About to process Machine Policy Inf");
                JObject MachinePolInfResults = ProcessInf(MachinePolPath);
                //Utility.DebugWrite("Trying to add findings to output");
                //ProcessXml(MachinePolPath);
                //ProcessInf(UserPolPath);
                //ProcessXml(UserPolPath);

                string OutputString = JsonConvert.SerializeObject(GPOOutput, Newtonsoft.Json.Formatting.Indented);

                // and put it into a file
                //using (System.IO.StreamWriter file =
                //new System.IO.StreamWriter(OutputPath, true))
                //{
                //    file.WriteLine(GPOOutput);
                //}
                
                //  TODO
                //  Parse other inf sections:
                //  System Access
                //  Kerberos Policy
                //  Event Audit
                //  Registry Values
                //  Registry Keys
                //  Group Membership
                //  Service General Setting
                //  Parse XML files
                //  Parse ini files
                //  Grep scripts for creds.
            }
        }

        static JObject ProcessInf(string Path)
        {
            // find all the GptTmpl.inf files
            string[] GptTmplInfFiles = Directory.GetFiles(Path, "GptTmpl.inf", SearchOption.AllDirectories);

            
            JObject InfResults = new JObject();
            // iterate over the list of inf files we found
            foreach (string infFile in GptTmplInfFiles)
            {
                //parse the inf file into a manageable format
                JObject ParsedInfFile = Parsers.ParseInf(infFile);
                //Console.WriteLine(ParsedInfFile);
                //send the inf file to be assessed
                //JObject InfResult = 
                Assess.AssessGPTmpl(ParsedInfFile);
                //add the result to our results
                //InfResults.Add(InfResult);
            }

            return InfResults;
        }

        static void ProcessXml(string Path)
        {
            // Group Policy Preferences are all XML so those are handled here.
            string[] XmlFiles = Directory.GetFiles(Path, "*.xml", SearchOption.AllDirectories);
            
            if (XmlFiles.Length >= 1)
            {
                //Utility.WriteColor("Found some XML files named:", ConsoleColor.White, ConsoleColor.DarkBlue);
                
                foreach (string XmlFile in XmlFiles)
                {
                    JObject ParsedXmlToJson = Parsers.ParseXmlToJson(XmlFile);
                    Assess.AssessGPPXml(ParsedXmlToJson);
                }
            }
        }
    }
}