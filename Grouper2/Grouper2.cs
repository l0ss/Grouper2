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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;

namespace Grouper2
{
    // Create a singleton that contains our big GPO data blob so we can access it without reparsing it.
    public sealed class JankyDb
    {
        private static readonly JankyDb _instance = new JankyDb();

        private JankyDb()
        {
            // Initialize.
        }

        public static JObject Instance
        {
            get
            {
                var jsonDataFile = File.ReadAllText("PolData.Json");
                var _instance = JObject.Parse(jsonDataFile);
                return _instance;
            }
        }
    }

    public static class GlobalVar
    {
        public static bool OnlineChecks;
    }

    internal class Grouper2
    {
        private static void Main(string[] args)
        {
            Utility.PrintBanner();
            var sysvolPolDir = "";

            var domainGpos = new JObject();

            // Ask the DC for GPO details
            if (args.Length == 0)
            {
                Console.WriteLine("Trying to figure out what AD domain we're working with.");
                var currentDomain = Domain.GetCurrentDomain();
                var currentDomainString = currentDomain.ToString();
                Console.WriteLine("Current AD Domain is: " + currentDomainString);
                sysvolPolDir = @"\\" + currentDomainString + @"\sysvol\" + currentDomainString + @"\Policies\";
                Utility.DebugWrite("SysvolPolDir is " + sysvolPolDir);
                GlobalVar.OnlineChecks = true;
            }

            // or if the user gives a path argument, just look for policies in there
            if (args.Length == 1)
            {
                Console.WriteLine("OK, I trust you know where you're aiming me.");
                sysvolPolDir = args[0];
            }

            Console.WriteLine("We gonna look at the policies in: " + sysvolPolDir);
            if (GlobalVar.OnlineChecks) domainGpos = LDAPstuff.GetDomainGpos();

            var gpoPaths = Directory.GetDirectories(sysvolPolDir);

            // create a dict to put all our output goodies in.
            var grouper2OutputDict = new Dictionary<string, JObject>();

            foreach (var gpoPath in gpoPaths)
            {
                // create a dict to put the stuff we find for this GPO into.
                var gpoResultDict = new Dictionary<string, JObject>();
                var gpoPropsDict = new Dictionary<string, string>();

                // Get the UID of the GPO from the file path.
                var dirSeparator = Path.DirectorySeparatorChar;
                char[] splitChars = {dirSeparator};
                var splitPath = gpoPath.Split(splitChars);
                var gpoUid = splitPath[splitPath.Length - 1];

                // Set some properties of the GPO we're looking at in our output file.
                gpoPropsDict.Add("GPO UID", gpoUid);
                gpoPropsDict.Add("GPO Path", gpoPath);
                if (GlobalVar.OnlineChecks)
                {
                    var domainGpo = domainGpos[gpoUid];
                    gpoPropsDict.Add("Display Name", domainGpo["DisplayName"].ToString());
                    gpoPropsDict.Add("Distinguished Name", domainGpo["DistinguishedName"].ToString());
                    gpoPropsDict.Add("GPO SDDL", domainGpo["SDDL"].ToString());
                }

                // TODO (and put in GPOProps)
                // look up the friendly name of the policy
                // get the policy ACLs
                // get the policy owner
                // get whether it's linked and where
                // get whether it's enabled

                // start processing files
                string[] machinePolPathArray = {gpoPath, "Machine"};
                string[] userPolPathArray = {gpoPath, "User"};
                var machinePolPath = Path.Combine(machinePolPathArray);
                var userPolPath = Path.Combine(userPolPathArray);

                //JObject machinePolInfResults = ProcessInf(machinePolPath);
                //JObject userPolInfResults = ProcessInf(userPolPath);
                JObject machinePolGppResults = ProcessGpXml(machinePolPath);
                JObject userPolGppResults = ProcessGpXml(userPolPath);

                var gpoPropsJson = (JObject) JToken.FromObject(gpoPropsDict);

                gpoResultDict.Add("GPOProps", gpoPropsJson);
                gpoResultDict.Add("Machine Policy from GPP XML files", machinePolGppResults);
                gpoResultDict.Add("User Policy from GPP XML files", userPolGppResults);
                //gpoResultDict.Add("Machine Policy from Inf files", machinePolInfResults);
                //gpoResultDict.Add("User Policy from Inf files", machinePolInfResults);

                var gpoResultJson = (JObject) JToken.FromObject(gpoResultDict);

                grouper2OutputDict.Add(gpoPath, gpoResultJson);

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
            var grouper2OutputJson = (JObject) JToken.FromObject(grouper2OutputDict);
            Console.WriteLine(grouper2OutputJson.ToString());
            Console.ReadKey();
        }


        private static JObject ProcessInf(string Path)
        {
            // find all the GptTmpl.inf files
            var gpttmplInfFiles = Directory.GetFiles(Path, "GptTmpl.inf", SearchOption.AllDirectories);
            // make a dict for our results
            var processedInfsDict = new Dictionary<string, JObject>();
            // iterate over the list of inf files we found
            foreach (var infFile in gpttmplInfFiles)
            {
                //parse the inf file into a manageable format
                var parsedInfFile = Parsers.ParseInf(infFile);
                //send the inf file to be assessed
                var assessedGpTmpl = AssessHandlers.AssessGptmpl(parsedInfFile);

                //add the result to our results
                processedInfsDict.Add(infFile, assessedGpTmpl);
            }

            var processedInfsJson = (JObject) JToken.FromObject(processedInfsDict);
            return processedInfsJson;
        }

        private static JObject ProcessGpXml(string Path)
        {
            // Group Policy Preferences are all XML so those are handled here.
            var xmlFiles = Directory.GetFiles(Path, "*.xml", SearchOption.AllDirectories);
            // create a dict for the stuff we find
            var processedGpXml = new Dictionary<string, JObject>();
            // if we find any xml files
            if (xmlFiles.Length >= 1)
                foreach (var xmlFile in xmlFiles)
                {
                    // send each one to get mangled into json
                    var parsedGppXmlToJson = Parsers.ParseGppXmlToJson(xmlFile);
                    // then send each one to get assessed for fun things
                    var assessedGpp = AssessHandlers.AssessGppJson(parsedGppXmlToJson);
                    if (assessedGpp != null) processedGpXml.Add(xmlFile, assessedGpp);
                }

            var processedGpXmlJson = (JObject) JToken.FromObject(processedGpXml);
            return processedGpXmlJson;
        }
    }
}