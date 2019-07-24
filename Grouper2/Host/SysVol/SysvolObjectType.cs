namespace Grouper2.Host.SysVol
{
    public enum SysvolObjectType
    {
        PolicyFolder,
        ScriptDirectory,
        GpoDirectory,
        MachineDirectory,
        UserDirectory,
        UselessFluffDirectory,
        UselessFluffFile,
        AasFile,
        InfFile,
        IniFile,
        PolFile,
        XmlFile,
        ScriptFile,
        RootFolder,
        Unclassified
    }

    public enum SysvolMajorType
    {
        Dir,
        File
    }
}