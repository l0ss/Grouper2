/*
 *      .,-:::::/  :::::::..       ...      ...    :::::::::::::. .,::::::  :::::::..     .:::.  
 *    ,;;-'````'   ;;;;``;;;;   .;;;;;;;.   ;;     ;;; `;;;```.;;;;;;;''''  ;;;;``;;;;   ,;'``;. 
 *    [[[   [[[[[[/ [[[,/[[['  ,[[     \[[,[['     [[[  `]]nnn]]'  [[cccc    [[[,/[[['   ''  ,[['
 *    "$$c.    "$$  $$$$$$c    $$$,     $$$$$      $$$   $$$""     $$""""    $$$$$$c     .c$$P'  
 *     `Y8bo,,,o88o 888b "88bo,"888,_ _,88P88    .d888   888o      888oo,__  888b "88bo,d88 _,oo,
 *       `'YMUP"YMM MMMM   "W"   "YMMMMMP"  "YmmMMMM""   YMMMb     """"YUMMM MMMM   "W" MMMUP*"^^
 *
 *      Alpha
 *                        By Mike Loss (@mikeloss)                                                
 */


using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using Grouper2.Properties;
using System.Threading;
using System.Threading.Tasks;

namespace Grouper2
{
    // Create a singleton that contains our big GPO data blob so we can access it without reparsing it.
    public static class JankyDb
    {
        private static JObject _instance;

        public static JObject Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = JObject.Parse(Resources.PolData);
                }

                return _instance;
            }
        }
    }

    public static class GetDomainGpoData
    {
        private static JObject _domainGpoData;

        public static JObject DomainGpoData
        {
            get
            {
                if (_domainGpoData == null)
                {
                    try
                    {
                        _domainGpoData = LDAPstuff.GetDomainGpos();
                    }
                    catch (Exception e)
                    {
                        Utility.DebugWrite("Failed to get all the GPO Data from DC.");
                        Utility.DebugWrite(e.ToString());
                        _domainGpoData = new JObject();
                    }
                }
                return _domainGpoData;
            }
        }
    }


public class GlobalVar
    {
        public static bool OnlineChecks;
        public static int IntLevelToShow;
        public static bool DebugMode;
        public static bool NoMess;
        public static string UserDefinedDomain;
        public static string UserDefinedDomainDn;
        public static string UserDefinedUsername;
        public static string UserDefinedPassword;
        public static List<String> CleanupList = new List<string>();
    }

    internal class Grouper2
    {
        private static void Main(string[] args)
        {
            DateTime grouper2StartTime = DateTime.Now;
            Utility.PrintBanner();
            
            CommandLineParser.CommandLineParser parser = new CommandLineParser.CommandLineParser();
            SwitchArgument debugArg = new SwitchArgument('v', "verbose", "Enables verbose debug mode. Will also show you the names of any categories of policies that Grouper saw but didn't have any means of processing. I eagerly await your pull request.", false);
            SwitchArgument offlineArg = new SwitchArgument('o', "offline",
                "Disables checks that require LDAP comms with a DC or SMB comms with file shares found in policy settings. Requires that you define a value for --sysvol.",
                false);
            ValueArgument<string> sysvolArg =
                new ValueArgument<string>('s', "sysvol", "Set the path to a domain SYSVOL directory.");
            ValueArgument<int> intlevArg = new ValueArgument<int>('i', "interestlevel",
                "The minimum interest level to display. i.e. findings with an interest level lower than x will not be seen in output. Defaults to 1, i.e. show everything except some extremely dull defaults. If you want to see those too, do -i 0.");
            ValueArgument<int> threadsArg = new ValueArgument<int>('t',"threads", "Max number of threads. Defaults to 10.");
            ValueArgument<string> domainArg =
                new ValueArgument<string>('d', "domain", "Domain to query for Group Policy Goodies.");
            ValueArgument<string> passwordArg = new ValueArgument<string>('p', "password", "Password to use for LDAP operations.");
            ValueArgument<string> usernameArg =
                new ValueArgument<string>('u', "username", "Username to use for LDAP operations.");
            SwitchArgument helpArg = new SwitchArgument('h', "help", "Displays this help.", false);
            SwitchArgument prettyArg = new SwitchArgument('g', "pretty", "Switches output from the raw Json to a prettier format.", false);
            SwitchArgument noMessArg = new SwitchArgument('m', "nomess", "Avoids file writes at all costs. May find less stuff.", false);
            SwitchArgument currentPolOnlyArg = new SwitchArgument('c', "currentonly", "Only checks current policies, ignoring stuff in those Policies_NTFRS_* directories that result from replication failures.", false);
            SwitchArgument noGrepScriptsArg = new SwitchArgument('n', "nogrepscripts", "Don't grep through the files in the \"Scripts\" subdirectory", false);
        
            parser.Arguments.Add(usernameArg);
            parser.Arguments.Add(passwordArg);
            parser.Arguments.Add(debugArg);
            parser.Arguments.Add(intlevArg);
            parser.Arguments.Add(sysvolArg);
            parser.Arguments.Add(offlineArg);
            parser.Arguments.Add(threadsArg);
            parser.Arguments.Add(helpArg);
            parser.Arguments.Add(prettyArg);
            parser.Arguments.Add(noMessArg);
            parser.Arguments.Add(currentPolOnlyArg);
            parser.Arguments.Add(noGrepScriptsArg);
            parser.Arguments.Add(domainArg);

            // set a few defaults
            string sysvolDir = "";
            GlobalVar.OnlineChecks = true;
            int maxThreads = 10;
            bool prettyOutput = false;
            GlobalVar.NoMess = false;
            bool noNtfrs = false;
            bool noGrepScripts = false;
            string userDefinedDomain = "";

            try
            {
                parser.ParseCommandLine(args);
                if (helpArg.Parsed)
                {
                    foreach (Argument arg in parser.Arguments)
                    {
                        Console.Error.Write("-");
                        Console.Error.Write(arg.ShortName);
                        Console.Error.Write(" " + arg.LongName);
                        Console.Error.WriteLine(" - " + arg.Description);
                    }

                    Environment.Exit(0);
                }
                if (offlineArg.Parsed && offlineArg.Value && sysvolArg.Parsed)
                {
                    // args config for valid offline run.
                    GlobalVar.OnlineChecks = false;
                    sysvolDir = sysvolArg.Value;
                }

                if (offlineArg.Parsed && offlineArg.Value && !sysvolArg.Parsed)
                {
                    // handle someone trying to run in offline mode without giving a value for sysvol
                    Console.Error.WriteLine(
                        "\nOffline mode requires you to provide a value for -s, the path where Grouper2 can find the domain SYSVOL share, or a copy of it at least.");
                    Environment.Exit(1);
                }

                if (intlevArg.Parsed)
                {
                    // handle interest level parsing
                    Console.Error.WriteLine("\nRoger. Everything with an Interest Level lower than " +
                                      intlevArg.Value.ToString() + " is getting thrown on the floor.");
                    GlobalVar.IntLevelToShow = intlevArg.Value;
                }
                else
                {
                    GlobalVar.IntLevelToShow = 1;
                }

                if (debugArg.Parsed)
                {
                    Console.Error.WriteLine("\nVerbose debug mode enabled. Hope you like yellow.");
                    GlobalVar.DebugMode = true;
                }

                if (threadsArg.Parsed)
                {
                    Console.Error.WriteLine("\nMaximum threads set to: " + threadsArg.Value);
                    maxThreads = threadsArg.Value;
                }

                if (sysvolArg.Parsed)
                {
                    Console.Error.WriteLine("\nYou specified that I should assume SYSVOL is here: " + sysvolArg.Value);
                    sysvolDir = sysvolArg.Value;
                }

                if (prettyArg.Parsed)
                {
                    Console.Error.WriteLine("\nSwitching output to pretty mode. Nice.");
                    prettyOutput = true;
                }

                if (noMessArg.Parsed)
                {
                    Console.Error.WriteLine("\nNo Mess mode enabled. Good for OPSEC, maybe bad for finding all the vulns? All \"Directory Is Writable\" checks will return false.");

                    GlobalVar.NoMess = true;
                }

                if (currentPolOnlyArg.Parsed)
                {
                    Console.Error.WriteLine("\nOnly looking at current policies and scripts, not checking any of those weird old NTFRS dirs.");
                    noNtfrs = true;
                }

                if (domainArg.Parsed)
                {
                    Console.Error.Write("\nYou told me to talk to domain " + domainArg.Value + " so I'm gonna do that.");
                    if (!(usernameArg.Parsed) || !(passwordArg.Parsed))
                    {
                        Console.Error.Write("\nIf you specify a domain you need to specify a username and password too using -u and -p.");
                    };
                    userDefinedDomain = domainArg.Value;
                    string[] splitDomain = userDefinedDomain.Split('.');
                    StringBuilder sb = new StringBuilder();
                    int pi = splitDomain.Length;
                    int ind = 1;
                    foreach (string piece in splitDomain)
                    {
                        sb.Append("DC=" + piece);
                        if (pi != ind)
                        {
                            sb.Append(",");
                        }
                        ind++;
                    }

                    GlobalVar.UserDefinedDomain = userDefinedDomain;
                    GlobalVar.UserDefinedDomainDn = sb.ToString();
                    GlobalVar.UserDefinedPassword = passwordArg.Value;
                    GlobalVar.UserDefinedUsername = usernameArg.Value;
                }

                if (noGrepScriptsArg.Parsed)
                {
                    Console.Error.WriteLine("\nNot gonna look through scripts in SYSVOL for goodies.");
                    noGrepScripts = true;
                }
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.Message);
            }

            if (GlobalVar.UserDefinedDomain != null)
            {
                Console.Error.WriteLine("\nRunning as user: " + GlobalVar.UserDefinedDomain + "\\" + GlobalVar.UserDefinedUsername);
            }
            else
            {
                Console.Error.WriteLine("\nRunning as user: " + Environment.UserDomainName + "\\" + Environment.UserName);
            }
            Console.Error.WriteLine("\nAll online checks will be performed in the context of this user.");

            // Ask the DC for GPO details
            string currentDomainString = "";
            if (GlobalVar.OnlineChecks)
            {
                if (userDefinedDomain != "")
                {
                    currentDomainString = userDefinedDomain;
                }
                else
                {
                    Console.WriteLine("\nTrying to figure out what AD domain we're working with.");
                    try
                    {
                        currentDomainString = Domain.GetCurrentDomain().ToString();
                    }
                    catch (ActiveDirectoryOperationException e)
                    {
                        Console.WriteLine("\nCouldn't talk to the domain properly. If you're trying to run offline you should use the -o switch. Failing that, try rerunning with -d to specify a domain or -v to get more information about the error.");
                        if (GlobalVar.DebugMode)
                        {
                            Utility.DebugWrite(e.ToString());
                        }

                        Environment.Exit(1);
                    }
                }

                Console.WriteLine("\nCurrent AD Domain is: " + currentDomainString);
                
                // if we're online, get a bunch of metadata about the GPOs via LDAP
                JObject domainGpos = new JObject();

                if (GlobalVar.OnlineChecks)
                {
                    domainGpos = GetDomainGpoData.DomainGpoData;
                }

                Console.WriteLine("");

                if (sysvolDir == "")
                {
                    sysvolDir = @"\\" + currentDomainString + @"\sysvol\" + currentDomainString + @"\";
                    Console.WriteLine("Targeting SYSVOL at: " + sysvolDir);
                }
            }
            else if ((GlobalVar.OnlineChecks == false) && sysvolDir.Length > 1)
            {
                Console.WriteLine("\nTargeting SYSVOL at: " + sysvolDir);
            }
            else
            {
                Console.Error.WriteLine("\nSomething went wrong with parsing the path to sysvol and I gave up.");
                Environment.Exit(1);
            }

            // get all the dirs with Policies and scripts in an array.
            string[] sysvolDirs =
                Directory.GetDirectories(sysvolDir);

            Console.WriteLine(
                "\nI found all these directories in SYSVOL...");
            Console.WriteLine("#########################################");
            foreach (string line in sysvolDirs)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine("#########################################");

            List<string> sysvolPolDirs = new List<string>();
            List<string> sysvolScriptDirs = new List<string>();

            if (noNtfrs)
            {
                Console.WriteLine("... but I'm not going to look in any of them except .\\Policies and .\\Scripts because you told me not to.");
                sysvolPolDirs.Add(sysvolDir + "Policies\\");
                sysvolScriptDirs.Add(sysvolDir + "Scripts\\");
            }
            else
            {
                Console.Error.WriteLine("... and I'm going to find all the goodies I can in all of them.");
                foreach (string dir in sysvolDirs)
                {
                    if (dir.ToLower().Contains("scripts"))
                    {
                        sysvolScriptDirs.Add(dir);
                    }

                    if (dir.ToLower().Contains("policies"))
                    {
                        sysvolPolDirs.Add(dir);
                    }
                }
            }


            // get all the policy dirs
            List<string> gpoPaths = new List<string>();
            foreach (string policyPath in sysvolPolDirs)
            {
                try
                {
                    gpoPaths = Directory.GetDirectories(policyPath).ToList();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("I had a problem with " + policyPath +
                                            ". I guess you could try to fix it?");
                    if (GlobalVar.DebugMode)
                    {
                        Utility.DebugWrite(e.ToString());
                    }
                }
            }

            // create a JObject to put all our output goodies in.
            JObject grouper2Output = new JObject();
            // so for each uid directory (including ones with that dumb broken domain replication condition)
            // we're going to gather up all our goodies and put them into that dict we just created.

            // Create a TaskScheduler
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(maxThreads);
            List<Task> gpoTasks = new List<Task>();
            
            // create a TaskFactory
            TaskFactory gpoFactory = new TaskFactory(lcts);
            CancellationTokenSource gpocts = new CancellationTokenSource();
            
            Console.Error.WriteLine("\n" + gpoPaths.Count.ToString() + " GPOs to process.");
            Console.Error.WriteLine("\nStarting processing GPOs with " + maxThreads.ToString() + " threads.");
            
            // Create a task for each GPO
            foreach (string gpoPath in gpoPaths)
            {
                Task t = gpoFactory.StartNew(() =>
                {
                    JObject gpoFindings = ProcessGpo(gpoPath);
                    if (gpoFindings != null)
                    {
                        if (gpoFindings.HasValues)
                        {
                            lock (grouper2Output)
                            {
                                if (!(gpoPath.Contains("NTFRS")))
                                {
                                    grouper2Output.Add(("Current Policy - " + gpoPath), gpoFindings);
                                }
                                else
                                {
                                    grouper2Output.Add(gpoPath, gpoFindings);
                                }
                            }
                        }
                    }
                }, gpocts.Token);
                gpoTasks.Add(t);
            }
            
            // put 'em all in a happy little array
            Task[] gpoTaskArray = gpoTasks.ToArray();
            
            // create a little counter to provide status updates
            int totalGPOTasksCount = gpoTaskArray.Length;
            int incompleteTaskCount = gpoTaskArray.Length;
            Console.WriteLine("");
            while (incompleteTaskCount > 0)
            {
                Task[] incompleteTasks =
                    Array.FindAll(gpoTaskArray, element => element.Status != TaskStatus.RanToCompletion);
                incompleteTaskCount = incompleteTasks.Length;
            
                int completeTaskCount = totalGPOTasksCount - incompleteTaskCount;
                int percentage = (int) Math.Round((double) (100 * completeTaskCount) / totalGPOTasksCount);
                string percentageString = percentage.ToString();
                Console.Error.Write("");
            
                Console.Error.Write("\r" + completeTaskCount.ToString() + "/" + totalGPOTasksCount.ToString() +
                              " GPOs processed. " + percentageString + "% complete.");
            }


            // make double sure tasks all finished
            Task.WaitAll(gpoTasks.ToArray());
            gpocts.Dispose();

            // do the script grepping
            if (!(noGrepScripts))
            {
                Console.Error.Write("Processing SYSVOL script dirs.");
                JObject processedScriptDirs = ProcessScripts(sysvolScriptDirs);
                if ((processedScriptDirs != null) && (processedScriptDirs.HasValues))
                {
                    grouper2Output.Add("Scripts", processedScriptDirs);
                }
            }


            // Final output is finally happening finally here:

            if (prettyOutput)
            {
                foreach (KeyValuePair<string, JToken> gpo in grouper2Output)
                {
                    Console.Error.WriteLine("\n");
                    Output.GetAssessedGPOOutput(gpo);
                }
            }
            else
            {
                Console.WriteLine(grouper2Output);
            }
            

            // get the time it took to do the thing and give to user
            DateTime grouper2EndTime = DateTime.Now;
            TimeSpan grouper2RunTime = grouper2EndTime.Subtract(grouper2StartTime);
            string grouper2RunTimeString =
                $"{grouper2RunTime.Hours}:{grouper2RunTime.Minutes}:{grouper2RunTime.Seconds}:{grouper2RunTime.Milliseconds}";

            Console.WriteLine("Grouper2 took " + grouper2RunTimeString + " to run.");

            if (GlobalVar.CleanupList != null)
            {
                List<string> cleanupList = Utility.DedupeList(GlobalVar.CleanupList);
                if (cleanupList.Count >= 1)
                {
                    Console.WriteLine("\n\nGrouper2 tried to create these files. It probably failed, but just in case it didn't, you might want to check and clean them up.\n");
                    foreach (string path in cleanupList)
                    {
                        Console.WriteLine(path);
                    }
                }
            }

            Console.WriteLine("\n\nPress any key to exit.");
            // wait for 'anykey'
            Console.ReadKey();
        }

        private static JObject ProcessScripts(List<string> scriptDirs)
        {
            // output object
            JObject processedScripts = new JObject();

            foreach (string scriptDir in scriptDirs)
            {
                try
                {
                    // get all the files in this dir
                    string[] scriptDirFiles = Directory.GetFiles(scriptDir, "*", SearchOption.AllDirectories);
                    // add them all to the master list of files
                    foreach (string scriptDirFile in scriptDirFiles)
                    {
                        // get the file info so we can check size
                        FileInfo scriptFileInfo = new FileInfo(scriptDirFile);
                        // if it's not too big
                        if (scriptFileInfo.Length < 200000)
                        {
                            // feed the whole thing through Utility.InvestigateFileContents
                            JObject investigatedScript = Utility.InvestigateFileContents(scriptDirFile);
                            // if we got anything good, add the result to processedScripts
                            if (investigatedScript != null)
                            {
                                if (((int)investigatedScript["InterestLevel"]) >= GlobalVar.IntLevelToShow)
                                {
                                    processedScripts.Add(
                                        new JProperty(scriptDirFile, investigatedScript)
                                    );
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Utility.DebugWrite(e.ToString());
                }
            }
            

            if (processedScripts.HasValues)
            {
                return processedScripts;
            }
            else
            {
                return null;
            }
        }

        private static JObject ProcessGpo(string gpoPath)
        {
            try
            {
                // create a dict to put the stuff we find for this GPO into.
                JObject gpoResult = new JObject();
                // Get the UID of the GPO from the file path.
                string[] splitPath = gpoPath.Split(Path.DirectorySeparatorChar);
                string gpoUid = splitPath[splitPath.Length - 1];

                // Make a JObject for GPO metadata
                JObject gpoProps = new JObject();
                // If we're online and talking to the domain, just use that data
                if (GlobalVar.OnlineChecks)
                {
                    try
                    {
                        // select the GPO's details from the gpo data we got
                        JToken domainGpo = GetDomainGpoData.DomainGpoData[gpoUid];
                        gpoProps = (JObject) JToken.FromObject(domainGpo);
                        gpoProps.Add("gpoPath", gpoPath);
                    }
                    catch (ArgumentNullException e)
                    {
                        
                        if (GlobalVar.DebugMode)
                        {
                            Utility.DebugWrite("Couldn't get GPO Properties from the domain for the following GPO: " +
                                               gpoUid);
                            Utility.DebugWrite(e.ToString());
                        }

                        // if we weren't able to select the GPO's details, do what we can with what we have.
                        gpoProps = new JObject()
                        {
                            {"UID", gpoUid},
                            {"gpoPath", gpoPath}
                        };
                    }
                }
                // otherwise do what we can with what we have
                else
                {
                    gpoProps = new JObject()
                    {
                        {"UID", gpoUid},
                        {"gpoPath", gpoPath}
                    };
                }


                // Add all this crap into a dict, if we found anything of interest.
                gpoResult.Add("GPOProps", gpoProps);
                // turn dict of data for this gpo into jobj
                JObject gpoResultJson = (JObject) JToken.FromObject(gpoResult);

                // if I were smarter I would have done this shit with the machine and user dirs inside the Process methods instead of calling each one twice out here.
                // @liamosaur you reckon you can see how to clean it up after the fact?
                // Get the paths for the machine policy and user policy dirs
                string machinePolPath = Path.Combine(gpoPath, "Machine");
                string userPolPath = Path.Combine(gpoPath, "User");

                // Process Inf and Xml Policy data for machine and user
                JArray machinePolInfResults = ProcessInf(machinePolPath);
                JArray userPolInfResults = ProcessInf(userPolPath);
                JArray machinePolGppResults = ProcessGpXml(machinePolPath);
                JArray userPolGppResults = ProcessGpXml(userPolPath);
                JArray machinePolScriptResults = ProcessScriptsIni(machinePolPath);
                JArray userPolScriptResults = ProcessScriptsIni(userPolPath);
                JArray machinePolAasResults = ProcessAas(machinePolPath);
                JArray userPolAasResults = ProcessAas(userPolPath);

                // add all our findings to a JArray in what seems a very inefficient manner but it's the only way i could see to avoid having a JArray of JArrays of Findings.
                JArray userFindings = new JArray();
                JArray machineFindings = new JArray();

                JArray[] allMachineGpoResults =
                {
                    machinePolInfResults,
                    machinePolGppResults,
                    machinePolScriptResults,
                    machinePolAasResults
                };

                JArray[] allUserGpoResults =
                {
                    userPolInfResults,
                    userPolGppResults,
                    userPolScriptResults,
                    userPolAasResults
                };

                foreach (JArray machineGpoResult in allMachineGpoResults)
                {
                    if (machineGpoResult != null && machineGpoResult.HasValues)
                    {
                        foreach (JObject finding in machineGpoResult)
                        {
                            machineFindings.Add(finding);
                        }
                    }
                }

                foreach (JArray userGpoResult in allUserGpoResults)
                {
                    if (userGpoResult != null && userGpoResult.HasValues)
                    {
                        foreach (JObject finding in userGpoResult)
                        {
                            userFindings.Add(finding);
                        }
                    }
                }
                
                JObject allFindings = new JObject();

                // if there are any Findings, add it to the final output.
                if (userFindings.HasValues)
                {
                    JProperty userFindingsJProp = new JProperty("User Policy", userFindings);
                    allFindings.Add(userFindingsJProp);
                }

                if (machineFindings.HasValues)
                {
                    JProperty machineFindingsJProp = new JProperty("Machine Policy", machineFindings);
                    allFindings.Add(machineFindingsJProp);
                }

                if (allFindings.HasValues)
                {
                    gpoResultJson.Add("Findings", allFindings);
                    return gpoResultJson;
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Utility.DebugWrite(e.ToString());
            }

            return null;
        }

        private static JArray ProcessInf(string Path)
        {
            // find all the GptTmpl.inf files
            List<string> gpttmplInfFiles = new List<string>();
            try
            {
                gpttmplInfFiles = Directory.GetFiles(Path, "GptTmpl.inf", SearchOption.AllDirectories).ToList();
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite(e.ToString());
                }

                return null;
            }
            catch (System.UnauthorizedAccessException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite(e.ToString());
                }

                return null;
            }

            // make a JArray for our results
            JArray processedInfs = new JArray();
            // iterate over the list of inf files we found
            foreach (string infFile in gpttmplInfFiles)
            {
                //parse the inf file into a manageable format
                JObject parsedInfFile = Parsers.ParseInf(infFile);
                //send the inf file to be assessed
                JObject assessedGpTmpl = AssessHandlers.AssessGptmpl(parsedInfFile);

                //add the result to our results
                if (assessedGpTmpl.HasValues)
                {
                    processedInfs.Add(assessedGpTmpl);
                }
            }

            return processedInfs;
        }

        private static JArray ProcessScriptsIni(string Path)
        {
            List<string> scriptsIniFiles = new List<string>();

            try
            {
                scriptsIniFiles = Directory.GetFiles(Path, "Scripts.ini", SearchOption.AllDirectories).ToList();

            }
            catch (System.IO.DirectoryNotFoundException)
            {
                return null;
            }

            JArray processedScriptsIniFiles = new JArray();

            foreach (string iniFile in scriptsIniFiles)
            {
                JObject preParsedScriptsIniFile =
                    Parsers.ParseInf(iniFile); // Not a typo, the formats are almost the same.
                if (preParsedScriptsIniFile != null)
                {
                    JObject parsedScriptsIniFile = Parsers.ParseScriptsIniJson(preParsedScriptsIniFile);
                    JObject assessedScriptsIniFile = AssessScriptsIni.GetAssessedScriptsIni(parsedScriptsIniFile);
                    if (assessedScriptsIniFile != null)
                    {
                        processedScriptsIniFiles.Add(assessedScriptsIniFile);
                    }
                }
            }

            return processedScriptsIniFiles;
        }

        private static JArray ProcessAas(string Path)
        {
            List<string> aasFiles = new List<string>();

            try
            {
                aasFiles = Directory.GetFiles(Path, "*.aas", SearchOption.AllDirectories).ToList();
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite(e.ToString());
                }

                return null;
            }
            catch (System.UnauthorizedAccessException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite(e.ToString());
                }

                return null;
            }
            JObject processedAases = new JObject();
            foreach (string aasFile in aasFiles)
            {
                JObject parsedAasFile = Parsers.ParseAASFile(aasFile);
                JObject assessedAasFile = AasAssess.AssessAasFile(parsedAasFile);
                if (assessedAasFile != null && assessedAasFile.HasValues)
                {
                    processedAases.Add(aasFile, assessedAasFile);
                }
            }
            
            JArray aasResult = new JArray();

            if ((processedAases != null) && (processedAases.HasValues))
            {
                aasResult.Add(new JObject(new JProperty("Assigned Applications", processedAases)));
                return aasResult;
            }

            return null;
        }

        private static JArray ProcessGpXml(string Path)
        {
            if (!Directory.Exists(Path))
            {
                return null;
            }

            // Group Policy Preferences are all XML so those are handled here.
            string[] xmlFiles = Directory.GetFiles(Path, "*.xml", SearchOption.AllDirectories);
            // create a dict for the stuff we find
            JArray processedGpXml = new JArray();
            // if we find any xml files
            if (xmlFiles.Length >= 1)
                foreach (var xmlFile in xmlFiles)
                {
                    // send each one to get mangled into json
                    JObject parsedGppXmlToJson = Parsers.ParseGppXmlToJson(xmlFile);
                    // then send each one to get assessed for fun things
                    JObject assessedGpp = AssessHandlers.AssessGppJson(parsedGppXmlToJson);
                    if (assessedGpp.HasValues) processedGpXml.Add(assessedGpp);
                }

            return processedGpXml;
        }
    }

    //////////////////////////////////
    // Threading guff stolen from https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=netframework-4.0
    /////////////////////////////////

    // Provides a task scheduler that ensures a maximum concurrency level while 
    // running on top of the thread pool.
    public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        // Indicates whether the current thread is processing work items.
        [ThreadStatic] private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed 
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

        // The maximum concurrency level allowed by this scheduler. 
        private readonly int _maxDegreeOfParallelism;

        // Indicates whether the scheduler is currently processing work items. 
        private int _delegatesQueuedOrRunning = 0;

        // Creates a new instance with the specified degree of parallelism. 
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        // Queues a task to the scheduler. 
        protected sealed override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough 
            // delegates currently queued or running to process tasks, schedule another. 
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler. 
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue
                        base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread
                finally
                {
                    _currentThreadIsProcessingItems = false;
                }
            }, null);
        }

        // Attempts to execute the specified task on the current thread. 
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued)
                // Try to run the task. 
                if (TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        // Attempt to remove a previously scheduled task from the scheduler. 
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        // Gets the maximum concurrency level supported by this scheduler. 
        public sealed override int MaximumConcurrencyLevel
        {
            get { return _maxDegreeOfParallelism; }
        }

        // Gets an enumerable of the tasks currently scheduled on this scheduler. 
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }
}