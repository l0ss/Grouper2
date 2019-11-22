using System;
using System.Collections.Concurrent;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        private ConcurrentBag<string> UnopenedFiles { get; set; } = new ConcurrentBag<string>();
        internal Finding Audit(XmlSysvolFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            
            // determine what type of whatever is in the thing
            switch (file.FileSubType)
            {
                case FileType.XmlDataSources:
                    return Audit((DataSources) file.ReadData());
                case FileType.XmlDrives:
                    return Audit((Drives) file.ReadData());
                case FileType.XmlEnvironmentVars:
                    return Audit((EnvironmentVariables) file.ReadData());
                case FileType.XmlFiles:
                    return Audit((Files) file.ReadData());
                case FileType.XmlGroups:
                    return Audit((Groups) file.ReadData());
                case FileType.XmlIniFiles:
                    return Audit((IniFiles) file.ReadData());
                case FileType.XmlNetworkOptions:
                    return Audit((NetworkOptions) file.ReadData());
                case FileType.XmlNetworkShares:
                    return Audit((NetworkShareSettings) file.ReadData());
                case FileType.XmlPrinters:
                    return Audit((Printers) file.ReadData());
                case FileType.XmlRegistry:
                    return Audit((Registry) file.ReadData());
                case FileType.XmlScheduledTasks:
                    return Audit((ScheduledTasks) file.ReadData());
                case FileType.XmlServices:
                    return Audit((NtServices) file.ReadData());
                case FileType.XmlShortcuts:
                    return Audit((Shortcuts) file.ReadData());
                default:
                    UnopenedFiles.Add(file.Path);
                    return null;
            }
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(Devices file)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(Applications file)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(FolderOptions file)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(Folders file)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(InternetSettings file)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(Regional file)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(StartMenuTaskbar file)
        {
            throw new NotImplementedException();
        }
    }
}