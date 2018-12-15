/***
 *      .,-:::::/  :::::::..       ...      ...    :::::::::::::. .,::::::  :::::::..     .:::.  
 *    ,;;-'````'   ;;;;``;;;;   .;;;;;;;.   ;;     ;;; `;;;```.;;;;;;;''''  ;;;;``;;;;   ,;'``;. 
 *    [[[   [[[[[[/ [[[,/[[['  ,[[     \[[,[['     [[[  `]]nnn]]'  [[cccc    [[[,/[[['   ''  ,[['
 *    "$$c.    "$$  $$$$$$c    $$$,     $$$$$      $$$   $$$""     $$""""    $$$$$$c     .c$$P'  
 *     `Y8bo,,,o88o 888b "88bo,"888,_ _,88P88    .d888   888o      888oo,__  888b "88bo,d88 _,oo,
 *       `'YMUP"YMM MMMM   "W"   "YMMMMMP"  "YmmMMMM""   YMMMb     """"YUMMM MMMM   "W" MMMUP*"^^
 *                                                                                               
 *                        By Mike Loss (@mikeloss)                                                
 */
 using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.DirectoryServices.ActiveDirectory;

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

    public static class GlobalVar
    {
        public static bool OnlineChecks;
        public static int Verbosity;
    }

    class Grouper2
    {
        static void Main(string[] args)
        {
            Utility.PrintBanner();
            JObject JSonData = JankyDB.Instance;
            JObject JSonData2 = JankyDB.Instance;
            string SysvolPolDir = "";

            JObject DomainGPOs = new JObject();

            // Ask the DC for GPO details
            if (args.Length == 0)
            {
                    Console.WriteLine("Trying to figure out what AD domain we're workin with.");
                    Domain CurrentDomain = Domain.GetCurrentDomain();
                    string CurrentDomainString = CurrentDomain.ToString();
                    Console.WriteLine("Current AD Domain is: " + CurrentDomainString);
                    SysvolPolDir = @"\\" + CurrentDomainString + @"\sysvol\" + CurrentDomainString + @"\Policies\";
                    Utility.DebugWrite("SysvolPolDir is " + SysvolPolDir);
                    GlobalVar.OnlineChecks = true;
            }
            if (args.Length == 1)
            {
                Console.WriteLine("OK, I trust you know where you're aiming me.");
                SysvolPolDir = args[0];
            }

            Console.WriteLine("We gonna look at the policies in: " + SysvolPolDir);
            if (GlobalVar.OnlineChecks)
            {
                DomainGPOs = LDAPstuff.GetDomainGPOs();
            }

            string[]GPOPaths = Directory.GetDirectories(SysvolPolDir);
            
            // create a dict to put all our output goodies in.
            Dictionary<string, JObject> Grouper2OutputDict = new Dictionary<string, JObject>();

            foreach (string GPOPath in GPOPaths)
            {
                // create a dict to put the stuff we find for this GPO into.
                Dictionary<string, JObject> GPOResultDict = new Dictionary<string, JObject>();
                Dictionary<string, string> GPOPropsDict = new Dictionary<string, string>();

                // Get the UID of the GPO from the file path.
                char DirSeparator = Path.DirectorySeparatorChar;
                char[] SplitChars = new char[] { DirSeparator };
                string[] SplitPath = GPOPath.Split(SplitChars);
                string GPOUID = SplitPath[(SplitPath.Length - 1)];

                // Set some properties of the GPO we're looking at in our output file.
                GPOPropsDict.Add("GPO UID", GPOUID);
                GPOPropsDict.Add("GPO Path", GPOPath);
                if (GlobalVar.OnlineChecks)
                {
                    JToken DomainGPO = DomainGPOs[GPOUID];
                    GPOPropsDict.Add("Display Name", DomainGPO["DisplayName"].ToString());
                    GPOPropsDict.Add("Distinguished Name", DomainGPO["DistinguishedName"].ToString());
                    GPOPropsDict.Add("GPO SDDL", DomainGPO["SDDL"].ToString());
                }

                // TODO (and put in GPOProps)
                // look up the friendly name of the policy
                // get the policy ACLs
                // get the policy owner
                // get whether it's linked and where
                // get whether it's enabled

                // start processing files
                string[] MachinePolPathArray = { GPOPath, "Machine" };
                string[] UserPolPathArray = { GPOPath, "User" };
                string MachinePolPath = Path.Combine(MachinePolPathArray);
                string UserPolPath = Path.Combine(UserPolPathArray);

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
                // File permissions for referenced files.
            }

            // Final output is finally happening here:
            Utility.DebugWrite("Final Output:");
            JObject Grouper2OutputJson = (JObject)JToken.FromObject(Grouper2OutputDict);
            Console.WriteLine(Grouper2OutputJson.ToString());
            Console.ReadKey();
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
                JObject AssessedGPTmpl = AssessHandlers.AssessGPTmpl(ParsedInfFile);
               
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
                    JObject AssessedGPP = AssessHandlers.AssessGPPJson(ParsedGPPXmlToJson);
                    if (AssessedGPP.HasValues)
                    {
                        ProcessedGPXml.Add(XmlFile, AssessedGPP);
                    }
                }
            }
            JObject ProcessedGPXmlJson = (JObject)JToken.FromObject(ProcessedGPXml);
            return ProcessedGPXmlJson;
        }
    }
}