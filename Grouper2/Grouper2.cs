using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Xml;
using Newtonsoft.Json;

namespace Grouper2
{
    // Create a singleton that contains our big GPO data blob so we can access it without reparsing it.
    public sealed class JankyDB
    {
        static readonly JankyDB _instance = new JankyDB();
        public static JObject Instance
        {
            get
            {
                string JsonDataFile = File.ReadAllText("PolData.Json");
                JObject _instance = JObject.Parse(JsonDataFile);
                return _instance;
            }
        }
        JankyDB()
        {
            // Initialize.
        }
    }

    class Grouper2
    {
        static void Main(string[] args)
        {
            JObject JSonData = JankyDB.Instance;
            JObject JSonData2 = JankyDB.Instance;

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
            
            // create a dict to put all our output goodies in.
            Dictionary<string, JObject> Grouper2OutputDict = new Dictionary<string, JObject>();

            foreach (string GPOPath in GPOPaths)
            {
                // create a dict to put the stuff we find for this GPO into.
                Dictionary<string, JObject> GPOResultDict = new Dictionary<string, JObject>();

                Dictionary<string, string> GPOPropsDict = new Dictionary<string, string>();

                // Get the UID of the GPO from the file path.
                char[] SplitChars = new char[] { '\\' };
                string[] SplitPath = GPOPath.Split(SplitChars);
                string GPOUID = SplitPath[(SplitPath.Length - 1)];

                // Set some properties of the GPO we're looking at in our output file.
                GPOPropsDict.Add("GPO UID", GPOUID);
                GPOPropsDict.Add("GPO Path", GPOPath);
                // TODO (and put in GPOProps)
                // look up the friendly name of the policy
                // get the policy ACLs
                // get the policy owner
                // get whether it's linked and where
                // get whether it's enabled

                // start processing files
                string MachinePolPath = GPOPath + "\\Machine";
                string UserPolPath = GPOPath + "\\User";

                // need to get the shit returned from these
                //Utility.DebugWrite("About to process Machine Policy Inf");
                JObject MachinePolInfResults = ProcessInf(MachinePolPath);
                JObject UserPolInfResults = ProcessInf(UserPolPath);
                JObject MachinePolGPPResults = ProcessGPXml(MachinePolPath);
                JObject UserPolGPPResults = ProcessGPXml(UserPolPath);

                JObject GPOPropsJson = (JObject)JToken.FromObject(GPOPropsDict);

                GPOResultDict.Add("GPOProps", GPOPropsJson);
                GPOResultDict.Add("Machine Policy from GPP XML files", MachinePolGPPResults);
                GPOResultDict.Add("User Policy from GPP XML files", UserPolGPPResults);
                GPOResultDict.Add("Machine Policy from Inf files", MachinePolInfResults);
                GPOResultDict.Add("User Policy from Inf files", MachinePolInfResults);

                JObject GPOResultJson = (JObject)JToken.FromObject(GPOResultDict);

                Grouper2OutputDict.Add(GPOPath, GPOResultJson);

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
            // Final output is happening here:
            Utility.DebugWrite("Final Output:");
            JObject Grouper2OutputJson = (JObject)JToken.FromObject(Grouper2OutputDict);
            Console.WriteLine(Grouper2OutputJson.ToString());
        }

        static JObject ProcessInf(string Path)
        {
            // find all the GptTmpl.inf files
            string[] GptTmplInfFiles = Directory.GetFiles(Path, "GptTmpl.inf", SearchOption.AllDirectories);
            // make a dict for our results
            Dictionary<string, JObject> ProcessedInfsDict = new Dictionary<string, JObject>();
            // iterate over the list of inf files we found
            foreach (string infFile in GptTmplInfFiles)
            {
                //parse the inf file into a manageable format
                JObject ParsedInfFile = Parsers.ParseInf(infFile);
                //send the inf file to be assessed
                JObject AssessedGPTmpl = Assess.AssessGPTmpl(ParsedInfFile);

                //add the result to our results
                ProcessedInfsDict.Add(infFile, AssessedGPTmpl);
            }
            JObject ProcessedInfsJson = (JObject)JToken.FromObject(ProcessedInfsDict);
            return ProcessedInfsJson;
        }

        static JObject ProcessGPXml(string Path)
        {
            // Group Policy Preferences are all XML so those are handled here.
            string[] XmlFiles = Directory.GetFiles(Path, "*.xml", SearchOption.AllDirectories);
            // create a dict for the stuff we find
            Dictionary<string, JObject> ProcessedGPXml = new Dictionary<string, JObject>();
            // if we find any xml files
            if (XmlFiles.Length >= 1)
            {
                // iterate over our list of xml files
                foreach (string XmlFile in XmlFiles)
                {
                    // send each one to get mangled into json
                    JObject ParsedGPPXmlToJson = Parsers.ParseGPPXmlToJson(XmlFile);
                    // then send each one to get assessed for fun things
                    JObject AssessedGPP = Assess.AssessGPPJson(ParsedGPPXmlToJson);
                    ProcessedGPXml.Add(XmlFile, AssessedGPP);
                }
            }
            JObject ProcessedGPXmlJson = (JObject)JToken.FromObject(ProcessedGPXml);
            return ProcessedGPXmlJson;
        }
    }
}