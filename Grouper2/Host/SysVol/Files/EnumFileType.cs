namespace Grouper2.Host.SysVol.Files
{
    public enum FileType
    {
        // XML filetypes
        XmlUnknown, // the default for XML that is not classified
        XmlApplications,
        XmlDataSources,
        XmlDevices,
        XmlDrives,
        XmlEnvironmentVars,
        XmlFiles,
        XmlFolderOptions,
        XmlFolders,
        XmlGroups,
        XmlIniFiles,
        XmlInternetSettings,
        XmlNetworkOptions,
        XmlNetworkShares,
        XmlPowerOptions,
        XmlPrinters,
        XmlRegionalOptions,
        XmlRegistry,
        XmlScheduledTasks,
        XmlServices,
        XmlShortcuts,
        XmlStartMenu,

        // ini filetypes
        Ini,

        // inf filetypes
        Inf,

        // aas filetypes
        Aas,

        // pol filetypes
        Pol,
        
        // others
        Unwanted,

    }
}