# Grouper

## Major Components

### Grouper2.GrouperPlan

Reponsible for parsing commandline arguments, creating and informing other objects, and managing the resulting execution strategy.

### Grouper2.Host.SysVol.Sysvol

Creates a tree data structure representing a sysvol share. The tree holds descendants of `DaclProvider : IDaclProvider`

Directory and File objects are created within this structure as the mapping takes place.

File objects are capable of reading, parsing, and handing out their contents out on request.

### Grouper2.Host.DcConnection.Ldap

Manages the LDAP connection to a domain controller.
Contains a killswitch to prevent connection on request, and allows other objects to be "Online-Mode" agnostic.

### Grouper2.Auditor.GrouperAuditor

Conducts interest analysis on collected file and domain data, and compiles a report.

The `Audit()` method is overloaded to handle the analysis of the different file and content types descended from `DaclProvider` found in the mapped out Sysvol.

## General Flow of Execution

### Main Thread

These are the steps the execution takes on the main thread

1. Parse User Input

    `Grouper2.GrouperPlan.ParseCommandLineArguments(string[] args)`

    This creates an execution strategy based on user input.
    Some inputs required by singletons are stored in the JankyDB singleton.

1. Init Singletons

    `Grouper2.GrouperPlan.Execute()`

    The `Ldap` and `CurrentUser` singletons require an init to ensure they are aware of the execution strategy and available to later processes. The returned values are discarded in the main thread, but stored within the singletons.

1. Map Sysvol (technically still a singleton init, but more interesting)

    `Grouper2.GrouperPlan.Execute()` calls `Grouper2.Host.SysVol.Sysvol.GetMap()` calls `Grouper2.Host.SysVol.SysvolMapper.MapSysvol()`

    Sysvol is mapped to prevent the use of `FileInfo` and `DirectoryInfo` classes.
    This reduces the amount of interaction with SMB shares.
    All nodes in the map inherit from `DaclProvider`.

    The data or dacl information from files or directories is made available through the objects themselves in a "just-in-time" fashion.
    Only file contents that are of some interest are ever read.
    Methods to access the parsed file contents are made available by the files (`Grouper2.Host.SysVol.Files.SysvolFile`). See, for example `Grouper2.Host.SysVol.Files.IniSysvolFile.Parse()` which reads the file, parses it, and returns usable information about the file contents.

1. Init the Auditor Object

    Occurs within `Grouper2.GrouperPlan.Execute()`
    It is made available to methods within the `GrouperPlan` as a private auto-property.

    On instantiation, the `GrouperAuditor` object collects some data it needs, and prepares threadsafe collections for the results it obtains.

1. Start executing the plan

    `Grouper2.GrouperPlan.Audit()`

1. Audit the GPOs

    `Grouper2.GrouperPlan.Audit()` calls `Grouper2.GrouperPlan.AuditGpo()` over multiple threads and adds them to a wait group:

        gpoWaitGroup.Add(taskFactory.StartNew(() => AuditGpo(gpoNode.Data as Gpo, ref taskErrors), gpocts.Token));

    `Grouper2.GrouperPlan.AuditGpo()` wraps `Grouper2.Auditor.GrouperAuditor.AuditGpo()` to provide threading support.

    For each GPO present in the map of sysvol, some basic data about it will be added to the audit report.
    If possible, the basic data will be supplemented by data from the DomainController.

1. Wait for the GPOs to finish

    The data from the GPOs is required for processing the files, so wait for all the tasks to report back

1. Audit the files

    `Grouper2.GrouperPlan.Audit()` calls `Grouper2.GrouperPlan.AuditFile()` over multiple threads and adds them to a wait group. Based on what the files were categorised as, the files will be sent to processing with a machine/user directory designation:

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

    `Grouper2.GrouperPlan.AuditFile()` wraps `Grouper2.Auditor.GrouperAuditor.AuditFile()` to provide threading support.

    The steps in the file audit are:

    - determine the type of the file, and send it to the Audit() overload that matches the type
      - if it's an XML file, determine the type of xml file it is and send it to _that_ Audit() overload
    - read the parsed file contents in
    - conduct interest analysis on the file contents
    - add the analysis results to a threadsafe collection

1. Wait for all file tasks to finish

1. Add the audited file results to each GPO in the report

1. Send the report to the output
