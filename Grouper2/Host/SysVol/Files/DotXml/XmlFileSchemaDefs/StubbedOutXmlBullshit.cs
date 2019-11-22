using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace Grouper2.Host.SysVol.Files
{
    /// <summary>
    /// AAAAAAAAAAAAAHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
    /// </summary>
    public class FakeXmlBullshit
    {
        public static explicit operator Applications(FakeXmlBullshit f)
        {
            Applications @explicit = new Applications();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator DataSources(FakeXmlBullshit f)
        {
            DataSources sources = new DataSources();
            sources.JankyXmlStuff = f.JankyXmlStuff;
            return sources;
        }
        public static explicit operator Devices(FakeXmlBullshit f)
        {
            Devices devices = new Devices();
            devices.JankyXmlStuff = f.JankyXmlStuff;
            return devices;
        }
        public static explicit operator Drives(FakeXmlBullshit f)
        {
            Drives drives = new Drives();
            drives.JankyXmlStuff = f.JankyXmlStuff;
            return drives;
        }
        public static explicit operator EnvironmentVariables(FakeXmlBullshit f)
        {
            EnvironmentVariables @explicit = new EnvironmentVariables();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator Files(FakeXmlBullshit f)
        {
            Files files = new Files();
            files.JankyXmlStuff = f.JankyXmlStuff;
            return files;
        }
        public static explicit operator FolderOptions(FakeXmlBullshit f)
        {
            FolderOptions options = new FolderOptions();
            options.JankyXmlStuff = f.JankyXmlStuff;
            return options;
        }
        public static explicit operator Groups(FakeXmlBullshit f)
        {
            Groups groups = new Groups();
            groups.JankyXmlStuff = f.JankyXmlStuff;
            return groups;
        }
        public static explicit operator IniFiles(FakeXmlBullshit f)
        {
            IniFiles files = new IniFiles();
            files.JankyXmlStuff = f.JankyXmlStuff;
            return files;
        }
        public static explicit operator InternetSettings(FakeXmlBullshit f)
        {
            InternetSettings @explicit = new InternetSettings();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator NetworkOptions(FakeXmlBullshit f)
        {
            NetworkOptions options = new NetworkOptions();
            options.JankyXmlStuff = f.JankyXmlStuff;
            return options;
        }
        public static explicit operator NetworkShareSettings(FakeXmlBullshit f)
        {
            NetworkShareSettings @explicit = new NetworkShareSettings();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator PowerOptions(FakeXmlBullshit f)
        {
            PowerOptions options = new PowerOptions();
            options.JankyXmlStuff = f.JankyXmlStuff;
            return options;
        }
        public static explicit operator Printers(FakeXmlBullshit f)
        {
            Printers @explicit = new Printers();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator Regional(FakeXmlBullshit f)
        {
            Regional @explicit = new Regional();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator Registry(FakeXmlBullshit f)
        {
            Registry @explicit = new Registry();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator ScheduledTasks(FakeXmlBullshit f)
        {
            ScheduledTasks tasks = new ScheduledTasks();
            tasks.JankyXmlStuff = f.JankyXmlStuff;
            return tasks;
        }
        public static explicit operator NtServices(FakeXmlBullshit f)
        {
            NtServices @explicit = new NtServices();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator Shortcuts(FakeXmlBullshit f)
        {
            Shortcuts @explicit = new Shortcuts();
            @explicit.JankyXmlStuff = f.JankyXmlStuff;
            return @explicit;
        }
        public static explicit operator StartMenuTaskbar(FakeXmlBullshit f)
        {
            StartMenuTaskbar taskbar = new StartMenuTaskbar();
            taskbar.JankyXmlStuff = f.JankyXmlStuff;
            return taskbar;
        }

        public FakeXmlBullshit(string xml)
        {
            XmlDocument xmlFileContent = new XmlDocument();
            xmlFileContent.LoadXml(xml);
            string jsonFromXml = JsonConvert.SerializeXmlNode(xmlFileContent.DocumentElement, Formatting.Indented);
            JankyXmlStuff = JObject.Parse(jsonFromXml);
        }
        
        public JObject JankyXmlStuff { get;  }
    }

    public class StubbedOutXmlBullshit : SysvolXmlData
    {
        protected StubbedOutXmlBullshit(string xml)
        {
            // this is just a stub for the future
        }

        protected StubbedOutXmlBullshit()
        {
            // this is just a stub for the future
        }
        
        public JObject JankyXmlStuff { get; protected internal set; }
    }

    public class Applications : StubbedOutXmlBullshit
    {
        internal Applications()
        {
            
        }
        
        public Applications(string xml) : base(xml)
        {
        }
    }

    public class DataSources : StubbedOutXmlBullshit
    {
        public static DataSources FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new DataSources {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal DataSources()
        {
            
        }
        public DataSources(string xml) : base(xml)
        {
        }
    }

    public class Devices : StubbedOutXmlBullshit
    {
        public static Devices FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Devices {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Devices()
        {
            
        }
        public Devices(string xml) : base(xml)
        {
        }
    }

    public class Drives : StubbedOutXmlBullshit
    {
        public static Drives FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Drives {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Drives()
        {
            
        }
        public Drives(string xml) : base(xml)
        {
        }
    }

    public class EnvironmentVariables : StubbedOutXmlBullshit
    {
        public static EnvironmentVariables FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new EnvironmentVariables {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal EnvironmentVariables()
        {
            
        }
        public EnvironmentVariables(string xml) : base(xml)
        {
        }
    }

    public class Files : StubbedOutXmlBullshit
    {
        public static Files FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Files {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Files()
        {
            
        }
        public Files(string xml) : base(xml)
        {
        }
    }

    public class FolderOptions : StubbedOutXmlBullshit
    {
        public static FolderOptions FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new FolderOptions {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal FolderOptions()
        {
            
        }
        public FolderOptions(string xml) : base(xml)
        {
        }
    }

    public class Folders : StubbedOutXmlBullshit
    {
        public static Folders FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Folders {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Folders()
        {
            
        }
        public Folders(string xml) : base(xml)
        {
        }
    }

    public class Groups : StubbedOutXmlBullshit
    {
        public static Groups FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Groups {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Groups()
        {
            
        }
        public Groups(string xml) : base(xml)
        {
        }
    }

    public class IniFiles : StubbedOutXmlBullshit
    {
        public static IniFiles FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new IniFiles {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal IniFiles()
        {
            
        }
        public IniFiles(string xml) : base(xml)
        {
        }
    }

    public class InternetSettings : StubbedOutXmlBullshit
    {
        public static InternetSettings FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new InternetSettings {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal InternetSettings()
        {
            
        }
        public InternetSettings(string xml) : base(xml)
        {
        }
    }

    public class NetworkOptions : StubbedOutXmlBullshit
    {
        public static NetworkOptions FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new NetworkOptions {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal NetworkOptions()
        {
            
        }
        public NetworkOptions(string xml) : base(xml)
        {
        }
    }

    public class NetworkShareSettings : StubbedOutXmlBullshit
    {
        public static NetworkShareSettings FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new NetworkShareSettings {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal NetworkShareSettings()
        {
            
        }
        public NetworkShareSettings(string xml) : base(xml)
        {
        }
    }

    public class PowerOptions : StubbedOutXmlBullshit
    {
        public static PowerOptions FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new PowerOptions {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal PowerOptions()
        {
            
        }
        public PowerOptions(string xml) : base(xml)
        {
        }
    }

    public class Printers : StubbedOutXmlBullshit
    {
        public static Printers FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Printers {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Printers()
        {
            
        }
        public Printers(string xml) : base(xml)
        {
        }
    }

    public class Regional : StubbedOutXmlBullshit
    {
        public static Regional FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Regional {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Regional()
        {
            
        }
        public Regional(string xml) : base(xml)
        {
        }
    }

    public class Registry : StubbedOutXmlBullshit
    {
        public static Registry FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Registry {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Registry()
        {
            
        }
        public Registry(string xml) : base(xml)
        {
        }
    }

    public class ScheduledTasks : StubbedOutXmlBullshit
    {
        public static ScheduledTasks FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new ScheduledTasks {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal ScheduledTasks()
        {
            
        }
        public ScheduledTasks(string xml) : base(xml)
        {
        }
    }

    public class NtServices : StubbedOutXmlBullshit
    {
        public static NtServices FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new NtServices {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal NtServices()
        {
            
        }
        public NtServices(string xml) : base(xml)
        {
        }
    }

    public class Shortcuts : StubbedOutXmlBullshit
    {
        public static Shortcuts FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new Shortcuts {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal Shortcuts()
        {
            
        }
        public Shortcuts(string xml) : base(xml)
        {
        }
    }

    public class StartMenuTaskbar : StubbedOutXmlBullshit
    {
        public static StartMenuTaskbar FromStubbedOutXmlBullshit(StubbedOutXmlBullshit s)
        {
            return new StartMenuTaskbar {JankyXmlStuff = s.JankyXmlStuff};
        }

        internal StartMenuTaskbar()
        {
            
        }
        public StartMenuTaskbar(string xml) : base(xml)
        {
        }
    }
}