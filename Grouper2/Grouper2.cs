/*
 *      .,-:::::/  :::::::..       ...      ...    :::::::::::::. .,::::::  :::::::..     .:::.  
 *    ,;;-'````'   ;;;;``;;;;   .;;;;;;;.   ;;     ;;; `;;;```.;;;;;;;''''  ;;;;``;;;;   ,;'``;. 
 *    [[[   [[[[[[/ [[[,/[[['  ,[[     \[[,[['     [[[  `]]nnn]]'  [[cccc    [[[,/[[['   ''  ,[['
 *    "$$c.    "$$  $$$$$$c    $$$,     $$$$$      $$$   $$$""     $$""""    $$$$$$c     .c$$P'  
 *     `Y8bo,,,o88o 888b "88bo,"888,_ _,88P88    .d888   888o      888oo,__  888b "88bo,d88 _,oo,
 *       `'YMUP"YMM MMMM   "W"   "YMMMMMP"  "YmmMMMM""   YMMMb     """"YUMMM MMMM   "W" MMMUP*"^^
 *
 *      Beta
 *                        By Mike Loss (@mikeloss)                                                
 */


using System;
using Grouper2.Auditor;
using Grouper2.Utility;

namespace Grouper2
{

    internal class Grouper2
    {
        private static void Main(string[] args)
        {
            // Ingest the execution plan to the state keeper
            GrouperPlan plan = new GrouperPlan(args);

            // get the time it took to do the thing and give to user
            AuditReport report = plan.Execute();
            Console.WriteLine("Grouper2 took " + report.Runtime + " to run.");

            // output the report however was requested
            Output.OutputAuditReport(report, plan);

            // FINISHED!
#if DEBUG
            Console.Error.WriteLine("Press any key to Exit");
            Console.ReadKey();
#endif
        }
    }
}