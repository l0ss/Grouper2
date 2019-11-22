using System.IO;
using Grouper2.Host.SysVol.Files.Unwanted;

namespace Grouper2.Host.SysVol.Files
{
    public static class FileFactories
    {
        private static readonly InfFactory _inf = new InfFactory();
        private static readonly IniFactory _ini = new IniFactory();
        private static readonly PolFactory _pol = new PolFactory();
        private static readonly XmlFactory _xml = new XmlFactory();
        private static readonly GenericFileFactory _ick = new GenericFileFactory();

        public static SysvolFile Manufacture(string fileLocation)
        {

            if (fileLocation.ToLower().EndsWith("gpttmpl.inf"))
            {
                return _inf.GetFile(fileLocation);
            } else if (fileLocation.ToLower().EndsWith("scripts.ini"))
            {
                return _ini.GetFile(fileLocation);
            }
            else if (fileLocation.ToLower().EndsWith(".xml"))
            {
                return _xml.GetFile(fileLocation);
            }
            else if (fileLocation.ToLower().EndsWith("registry.pol"))
            {
                return _pol.GetFile(fileLocation);
            }
            else
            {
                return _ick.GetFile(fileLocation);
            }
        }
    }



    internal abstract class FileFactory
    {
        public abstract SysvolFile GetFile(string path);
    }
    
    internal partial class GenericFileFactory : FileFactory
    {
        public override SysvolFile GetFile(string path)
        {
            return new UnwantedFile(path){FileSubType = FileType.Unwanted, MajorType = SysvolMajorType.File};
        }
    }

    internal partial class InfFactory : FileFactory
    {
        public override SysvolFile GetFile(string path)
        {
            return new InfSysvolFile(path){FileSubType = FileType.Inf, MajorType = SysvolMajorType.File};
        }
    }

    internal partial class PolFactory : FileFactory
    {
        public override SysvolFile GetFile(string path)
        {
            return new PolSysvolFile(path){FileSubType = FileType.Pol, MajorType = SysvolMajorType.File};
        }
    }

    internal partial class IniFactory : FileFactory
    {
        public override SysvolFile GetFile(string path)
        {
            return new IniSysvolFile(path){FileSubType = FileType.Ini, MajorType = SysvolMajorType.File};
        }
    }

    internal partial class XmlFactory : FileFactory
    {
        public override SysvolFile GetFile(string path)
        {
            XmlSysvolFile file = new XmlSysvolFile(path){MajorType = SysvolMajorType.File};

            switch (file.FilenameWithoutExt.ToLower())
            {
                case "environmentvariables":
                    file.FileSubType = FileType.XmlEnvironmentVars;
                    break;
                case "applications":
                    file.FileSubType = FileType.XmlApplications;
                    break;
                case "datasources":
                    file.FileSubType = FileType.XmlDataSources;
                    break;
                case "devices":
                    file.FileSubType = FileType.XmlDevices;
                    break;
                case "drives":
                    file.FileSubType = FileType.XmlDrives;
                    break;
                case "files":
                    file.FileSubType = FileType.XmlFiles;
                    break;
                case "folders":
                    file.FileSubType = FileType.XmlFolders;
                    break;
                case "groups":
                    file.FileSubType = FileType.XmlGroups;
                    break;
                case "inifiles":
                    file.FileSubType = FileType.XmlIniFiles;
                    break;
                case "internetsettings":
                    file.FileSubType = FileType.XmlInternetSettings;
                    break;
                case "networkoptions":
                    file.FileSubType = FileType.XmlNetworkOptions;
                    break;
                case "networkshares":
                    file.FileSubType = FileType.XmlNetworkShares;
                    break;
                case "poweroptions":
                    file.FileSubType = FileType.XmlPowerOptions;
                    break;
                case "registry":
                    file.FileSubType = FileType.XmlRegistry;
                    break;
                case "printers":
                    file.FileSubType = FileType.XmlPrinters;
                    break;
                case "regionaloptions":
                    file.FileSubType = FileType.XmlRegionalOptions;
                    break;
                case "scheduledtasks":
                    file.FileSubType = FileType.XmlScheduledTasks;
                    break;
                case "services":
                    file.FileSubType = FileType.XmlServices;
                    break;
                case "shortcuts":
                    file.FileSubType = FileType.XmlShortcuts;
                    break;
                case "startmenu":
                    file.FileSubType = FileType.XmlStartMenu;
                    break;
                default:
                    file.FileSubType = FileType.XmlUnknown;
                    break;
            }

            return file;
        }
    }
}