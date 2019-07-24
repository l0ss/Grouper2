using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using Grouper2.Auditor;
using Grouper2.Host;
using Grouper2.Host.DcConnection;
using Grouper2.Host.SysVol;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;
using Gpo = Grouper2.Host.SysVol.Gpo;

namespace Grouper2
{
    public partial class GrouperPlan
    {
        public string SysVolDir { get; private set; }
        public bool OnlineChecks { get; private set; }
        public int MaxThreads { get; private set; }
        public bool PrettyOutput { get; private set; }
        public bool NoMess { get; private set; }
        public bool NoNtfrs { get; private set; }
        public bool NoGrepScripts { get; private set; }
        public bool HtmlOut { get; private set; }
        public string HtmlOutPath { get; private set; }
        public string Domain { get; private set; }
        public int IntLevelToShow { get; set; }
        public bool DebugMode { get; set; }
        public string UserDefinedDomain { get; set; }
        public string UserDefinedDomainDn { get; set; }
        public string UserDefinedUsername { get; set; }
        public string UserDefinedPassword { get; set; }
        public List<string> CleanupList { get; set; }

        // hold the objects the plan operate on
        //public Host.Host Host { get; private set; }

        private GrouperAuditor Auditor { get; set; }

        // hold the results of plan execution
        public JObject Results { get; private set; }
        
        public GrouperPlan(string[] args)
        {
            // set a few defaults
            this.SysVolDir = string.Empty;
            this.OnlineChecks = true;
            this.MaxThreads = 10;
            this.PrettyOutput = false;
            this.NoMess = false;
            this.NoNtfrs = false;
            this.NoGrepScripts = false;
            this.Domain = string.Empty;
            this.HtmlOut = false;
            this.HtmlOutPath = string.Empty;
            this.CleanupList = new List<string>();
            this.IntLevelToShow = 1;
            this.DebugMode = false;
            this.UserDefinedDomain = string.Empty;
            this.UserDefinedDomainDn = string.Empty;
            this.UserDefinedPassword = string.Empty;
            this.UserDefinedUsername = string.Empty;

            // ingest the commandline to populate the plan properties and override defaults with user input
            ParseCommandLineArguments(args);
        }
        
        private void ParseCommandLineArguments(string[] args)
        {
            // Set up commandline parser
            DateTime grouper2StartTime = DateTime.Now;
            CommandLineParser.CommandLineParser parser = new CommandLineParser.CommandLineParser();
            ValueArgument<string> htmlArg = new ValueArgument<string>('f', "html", "Path for html output file.");
            SwitchArgument debugArg = new SwitchArgument('v', "verbose",
                "Enables debug mode. Probably quite noisy and rarely necessary. Will also show you the names of any categories of policies" +
                " that Grouper saw but didn't have any means of processing. I eagerly await your pull request.", false);
            SwitchArgument offlineArg = new SwitchArgument('o', "offline",
                "Disables checks that require LDAP comms with a DC or SMB comms with file shares found in policy settings. Requires that you define a value for -s.",
                false);
            ValueArgument<string> sysvolArg =
                new ValueArgument<string>('s', "sysvol", "Set the path to a domain SYSVOL directory.");
            ValueArgument<int> intlevArg = new ValueArgument<int>('i', "interestlevel",
                "The minimum interest level to display. i.e. findings with an interest level lower than x will not be seen in output. Defaults to 1, i.e. show " +
                "everything except some extremely dull defaults. If you want to see those too, do -i 0.");
            ValueArgument<int> threadsArg =
                new ValueArgument<int>('t', "threads", "Max number of threads. Defaults to 10.");
            ValueArgument<string> domainArg =
                new ValueArgument<string>('d', "domain", "Domain to query for Group Policy Goodies.");
            ValueArgument<string> passwordArg =
                new ValueArgument<string>('p', "password", "Password to use for LDAP operations.");
            ValueArgument<string> usernameArg =
                new ValueArgument<string>('u', "username", "Username to use for LDAP operations.");
            SwitchArgument helpArg = new SwitchArgument('h', "help", "Displays this help.", false);
            SwitchArgument prettyArg = new SwitchArgument('g', "pretty",
                "Switches output from the raw Json to a prettier format.", false);
            SwitchArgument noMessArg = new SwitchArgument('m', "nomess",
                "Avoids file writes at all costs. May find less stuff.", false);
            SwitchArgument currentPolOnlyArg = new SwitchArgument('c', "currentonly",
                "Only checks current policies, ignoring stuff in those " +
                "Policies_NTFRS_* directories that result from replication failures.", false);
            SwitchArgument noGrepScriptsArg = new SwitchArgument('n', "nogrepscripts",
                "Don't grep through the files in the \"Scripts\" subdirectory", false);

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
            parser.Arguments.Add(htmlArg);

            // Parse the input
            try
            {
                parser.ParseCommandLine(args);
                // Print help and exit here on help arg
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

                // Detect a request to run offline
                if (offlineArg.Parsed && offlineArg.Value)
                {
                    // handle someone trying to run in offline mode without giving a value for sysvol
                    if (!sysvolArg.Parsed)
                    {
                        Console.Error.WriteLine(
                            "\nOffline mode requires you to provide a value for -s, the path where Grouper2 can find the domain SYSVOL share, or a copy of it at least.");
                        Environment.Exit(1);
                    }
                    else
                    {
                        // args config for valid offline run.
                        this.OnlineChecks = false;
                        this.SysVolDir = sysvolArg.Value;
                    }
                }

                // handle interest level parsing
                if (intlevArg.Parsed)
                {
                    // If a level was specified, set
                    Console.Error.WriteLine("\nRoger. Everything with an Interest Level lower than " +
                                            intlevArg.Value.ToString() + " is getting thrown on the floor.");
                    this.IntLevelToShow = intlevArg.Value;
                }

                // Check if the HTML output arg exists
                if (htmlArg.Parsed)
                {
                    // check the path is valid before we trust it blindly and exit
                    if (!Utility.FileSystem.IsValidPath(htmlArg.Value, true))
                    {
                        Console.Error.WriteLine(
                            "\nIt seems like " + htmlArg.Value + " might not be a valid filepath...");
                        Environment.Exit(1);
                    }

                    // probably valid if we are here...
                    this.HtmlOut = true;
                    this.HtmlOutPath = htmlArg.Value;
                }

                // check for debug mode
                if (debugArg.Parsed)
                {
                    Console.Error.WriteLine("\nVerbose debug mode enabled. Hope you like yellow.");
                    this.DebugMode = true;
                    JankyDb.DebugMode = true;
                }

                // check for a threads argument
                if (threadsArg.Parsed)
                {
                    Console.Error.WriteLine("\nMaximum threads set to: " + threadsArg.Value);
                    this.MaxThreads = threadsArg.Value;
                }

                // check for the sysvol argument
                if (sysvolArg.Parsed)
                {
                    // check the path is valid before we trust it blindly and exit if not
                    if (!Utility.FileSystem.IsValidPath(sysvolArg.Value, true))
                    {
                        Console.Error.WriteLine("\nIt seems like " + sysvolArg.Value +
                                                " might not be a valid filepath...");
                        Environment.Exit(1);
                    }

                    // probably valid if we are here...
                    Console.Error.WriteLine("\nYou specified that I should assume SYSVOL is here: " + sysvolArg.Value);
                    this.SysVolDir = sysvolArg.Value;
                }

                // check for the prettyprint option
                if (prettyArg.Parsed)
                {
                    Console.Error.WriteLine("\nSwitching output to pretty mode. Nice.");
                    this.PrettyOutput = true;
                }

                // check for the no mess arg
                // TODO: this doesn't appear to be actually used... Is this still needed if the SecurityDescriptor is tested instead of the file? Would that create any of the mess that is to be avoided? https://docs.microsoft.com/en-us/windows/desktop/fileio/file-security-and-access-rights
                if (noMessArg.Parsed)
                {
                    Console.Error.WriteLine(
                        "\nNo Mess mode enabled. Good for OPSEC, maybe bad for finding all the vulns? All \"Directory Is Writable\" checks will return false.");
                    this.NoMess = true;
                }

                // detect a policy-only run
                if (currentPolOnlyArg.Parsed)
                {
                    Console.Error.WriteLine(
                        "\nOnly looking at current policies and scripts, not checking any of those weird old NTFRS dirs.");
                    this.NoNtfrs = true;
                }

                // check for a supplied domain argument
                if (domainArg.Parsed)
                {
                    Console.Error.Write("\nYou told me to talk to domain " + domainArg.Value +
                                        " so I'm gonna do that.");
                    // check for the username and user pass
                    if (!usernameArg.Parsed || !passwordArg.Parsed)
                    {
                        Console.Error.Write(
                            "\nIf you specify a domain you need to specify a username and password too using -u and -p.");
                        Environment.Exit(1);
                    }

                    // set the  value and build out the DC string
                    this.UserDefinedDomain = domainArg.Value;
                    string[] splitDomain = this.UserDefinedDomain.Split('.');
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

                    // pass the values to the state singleton
                    this.UserDefinedDomainDn = sb.ToString();
                    this.UserDefinedPassword = passwordArg.Value;
                    this.UserDefinedUsername = usernameArg.Value;
                }

                // detect the switch for no grep
                if (noGrepScriptsArg.Parsed)
                {
                    Console.Error.WriteLine("\nNot gonna look through scripts in SYSVOL for goodies.");
                    this.NoGrepScripts = true;
                }
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.Message);
            }


            // report the user and domain which will be used for this run
            if (this.UserDefinedDomain != null)
            {
                Console.Error.WriteLine(
                    "\nRunning as user: " + this.UserDefinedDomain + "\\" + this.UserDefinedUsername);
            }
            else
            {
                Console.Error.WriteLine(
                    "\nRunning as user: " + Environment.UserDomainName + "\\" + Environment.UserName);
            }

            Console.Error.WriteLine("\nAll online checks will be performed in the context of this user.");

            // If this is not in pretty mode, print the banner
            if (!this.PrettyOutput) Output.PrintBanner();

            // Ask the DC for GPO details
            string currentDomainString = "";
            if (this.OnlineChecks)
            {
                // if a domain was supplied, use it for the current domain
                if (!string.IsNullOrEmpty(this.UserDefinedDomain))
                {
                    currentDomainString = this.UserDefinedDomain;
                }
                else
                {
                    // no domain was supplied, so try to find it
                    Console.Error.WriteLine("\nTrying to figure out what AD domain we're working with.");
                    try
                    {
                        currentDomainString = System.DirectoryServices.ActiveDirectory.Domain
                            .GetCurrentDomain().ToString();
                    }
                    catch (ActiveDirectoryOperationException e)
                    {
                        Console.Error.WriteLine(
                            "\nCouldn't talk to the domain properly. If you're trying to run offline you should use the -o switch. Failing that, try rerunning with -d to specify a domain or -v to get more information about the error.");
                        Utility.Output.DebugWrite(e.ToString());

                        Environment.Exit(1);
                    }
                }

                // report current AD domain
                Console.WriteLine("\nCurrent AD Domain is: " + currentDomainString);

                // this functionality has been moved to the instantiation of the Network class
                // if we're online, get a bunch of metadata about the GPOs via LDAP
//                if (this.OnlineChecks)
//                {
//                    // we don't want an object back, just to populate the singleton
//                    _ = GetDomainGpoData.DomainGpoData;
//                }

                Console.WriteLine("");

                // build the sysvol dir if it wasn't supplied
                if (string.IsNullOrEmpty(this.SysVolDir))
                {
                    this.SysVolDir = @"\\" + currentDomainString + @"\sysvol\" + currentDomainString + @"\";
                    Console.Error.WriteLine("Targeting SYSVOL at: " + this.SysVolDir);
                }
            }
            // if we are offline and a sysvol path was supplied
            else if (this.OnlineChecks == false && this.SysVolDir.Length > 1)
            {
                Console.Error.WriteLine("\nTargeting SYSVOL at: " + this.SysVolDir);
            }
            else
            {
                Console.Error.WriteLine("\nSomething went wrong with parsing the path to sysvol and I gave up.");
                Environment.Exit(1);
            }
            
            // push some vars into the jankydb for singleton bullshit
            JankyDb.Vars = new SingletonVars(
                this.SysVolDir, 
                this.IntLevelToShow, 
                this.OnlineChecks, 
                this.NoNtfrs, 
                this.NoGrepScripts, 
                this.DebugMode, 
                this.Domain);
        }
        
        public AuditReport Execute()
        {
            // start the clock
            DateTime start = DateTime.Now;

            // print out some data
            List<string> sysvolTopLevelDirectories = Sysvol.GetImmediateChildDirs(this.SysVolDir);
            Console.Error.WriteLine("\nI found all these directories in SYSVOL...");
            Console.Error.WriteLine("#########################################");
            foreach (string line in sysvolTopLevelDirectories) { Console.Error.WriteLine(line); }
            Console.Error.WriteLine("#########################################");
            Console.Error.WriteLine(NoNtfrs
                ? "... but I'm not going to look in any of them except .\\Policies and .\\Scripts because you told me not to."
                : "... and I'm going to find all the goodies I can in all of them.");

            //////////////////////////
            // Singletons
            // init ldap functionality.
            _ = Ldap.Use();
            // init the current user query object
            _ = CurrentUser.Query;
            // init the sysvol mapping features
            // this will cause the share to be mapped
            Sysvol sysvol = Sysvol.GetMap();

            //////////////////////////
            // other shit
            // build out the map of the host used to collect data
            // instantiate the auditor
            this.Auditor = new GrouperAuditor();

            // prep the GPOs for analysis
            Console.Error.WriteLine(Environment.NewLine + sysvol.map.Children.Where(n => n.Data.Type == SysvolObjectType.GpoDirectory) + " GPOs to process.");
            Console.Error.WriteLine(Environment.NewLine + "Starting processing GPOs with " +
                                    this.MaxThreads.ToString() + " threads.");
            // conduct the audit
            this.Audit(sysvol);
            
            // get the report
            AuditReport report = this.Auditor.GetReport(GetRuntimeString(start, DateTime.Now));

            // return
            return report;
        }

        private string GetRuntimeString(DateTime start, DateTime end)
        {
            TimeSpan grouper2RunTime = end.Subtract(start);
            return $"{grouper2RunTime.Hours}:{grouper2RunTime.Minutes}:{grouper2RunTime.Seconds}:{grouper2RunTime.Milliseconds}";
        }

        private void Audit(Sysvol sysvol)
        {
            // so for each uid directory (including ones with that dumb broken domain replication condition)
            // we're going to gather up all our goodies and put them into that dict we just created.
            // Create a TaskScheduler
            LimitedConcurrencyLevelTaskScheduler scheduler = new LimitedConcurrencyLevelTaskScheduler(this.MaxThreads);
            List<Task> gpoTasks = new List<Task>();

            // create a TaskFactory
            TaskFactory taskFactory = new TaskFactory(scheduler);
            CancellationTokenSource gpocts = new CancellationTokenSource();


            ConcurrentBag<string> taskErrors = new ConcurrentBag<string>();
            List<Task> gpoWaitGroup = new List<Task>();
            // iterate the gpos in the tree
            foreach (SysvolMapper.TreeNode<DaclProvider> gpoNode in sysvol.map.Where(n =>
                n.Data.Type == SysvolObjectType.GpoDirectory))
            {
                // add a task to populate gpo data
                // this is mainly a collection of data, and should be lightweight with no networking
                // but it means we will have the objects in place to add to
                gpoWaitGroup.Add(taskFactory.StartNew(() => AuditGpo(gpoNode.Data as Gpo, ref taskErrors), gpocts.Token));
            }

            // wait for that to finish cause we need its data
            try
            {
                Task.WaitAll(gpoWaitGroup.ToArray(), gpocts.Token);
            }
            catch (Exception e)
            {
                // well shit, I guess this whole thing is fucked
                Output.DebugWrite(e.ToString());
                throw;
            }

            // reset the wait group
            gpoWaitGroup = new List<Task>();

            // get a list of all the machine folder files and make tasks
            foreach (SysvolMapper.TreeNode<DaclProvider> node in sysvol.map.Where(n => n.IsRoot != true &&
                n.Parent.Data.Type == SysvolObjectType.GpoDirectory))
            {
                SysvolObjectType nType = node.Data.Type;
                
                List<DaclProvider> descendants = new List<DaclProvider>();
                foreach (SysvolMapper.TreeNode<DaclProvider> descendant in node.Descendants.ToList())
                {
                    if (descendant.IsLeaf)
                    {
                        descendants.Add(descendant.Data);
                    }
                }
                
                // iterate the leaves that are known files
                foreach (DaclProvider leaf in descendants)
                {
                    if (leaf.MajorType == SysvolMajorType.Dir)
                        continue;

                    switch (node.Data.Type)
                    {
                        case SysvolObjectType.MachineDirectory:
                            gpoWaitGroup.Add(taskFactory.StartNew(() =>
                                AuditFile(Gpo.UidFromPath(node.Data.Path),
                                    leaf, ref taskErrors,
                                    SysvolObjectType.MachineDirectory)));
                            break;
                        case SysvolObjectType.UserDirectory:
                            gpoWaitGroup.Add(taskFactory.StartNew(() =>
                                AuditFile(Gpo.UidFromPath(node.Data.Path),
                                    leaf, ref taskErrors,
                                    SysvolObjectType.UserDirectory)));
                            break;
                        default:
                            // UHM.... what?
                            continue;
                    }
                }
            }

            // if the map contains data about scripts
            if (!this.NoGrepScripts)
            {
                Console.Error.Write("\n\nProcessing SYSVOL script dirs.\n\n");
                // iterate the folders we  marked as script directories
                foreach (SysvolMapper.TreeNode<DaclProvider> scriptdirNode
                    in sysvol.map.Children.Where(n => n.Data.Type == SysvolObjectType.ScriptDirectory))
                {
                    // then iterate the leaves which are files
                    foreach (SysvolMapper.TreeNode<DaclProvider> fileNode
                        in scriptdirNode.Children.Where(n => n.IsLeaf
                                                             && n.Data.MajorType == SysvolMajorType.File))
                    {
                        // and send them to the auditor
                        gpoWaitGroup.Add(taskFactory.StartNew(() =>
                            AuditFile(Gpo.UidFromPath(fileNode.Data.Path),
                                fileNode.Data, ref taskErrors, SysvolObjectType.MachineDirectory)));
                    }
                }
            }


            // wait for tasks to finish
            // put 'em all in a happy little array
            ReportingLoop(gpoWaitGroup.ToArray());

            // make double sure tasks all finished
            Task.WaitAll(gpoWaitGroup.ToArray());
            gpocts.Dispose();

            // TODO: take the tasks from the bag and put them in the correct places

            Console.Error.WriteLine("Errors in processing GPOs:");
            Console.Error.WriteLine(taskErrors.ToString());
        }

        private void ReportingLoop(Task[] gpoTaskArray)
        {
            // create a little counter to provide status updates
            int totalGpoTasksCount = gpoTaskArray.Length;
            int remainingTaskCount = gpoTaskArray.Length;

            while (remainingTaskCount > 0)
            {
                Task[] incompleteTasks =
                    Array.FindAll(gpoTaskArray, element => element.Status != TaskStatus.RanToCompletion);
                int incompleteTaskCount = incompleteTasks.Length;
                Task[] faultedTasks = Array.FindAll(gpoTaskArray,
                    element => element.Status == TaskStatus.Faulted);
                int faultedTaskCount = faultedTasks.Length;
                int completeTaskCount = totalGpoTasksCount - incompleteTaskCount - faultedTaskCount;
                Log.Progress(completeTaskCount, totalGpoTasksCount, faultedTaskCount);
                remainingTaskCount = gpoTaskArray.Length;
            }
        }
    }
}