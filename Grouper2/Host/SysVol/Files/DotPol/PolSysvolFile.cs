using System;
using System.Collections.Generic;
using System.IO;
using Grouper2.Auditor;
using Grouper2.Utility;

namespace Grouper2.Host.SysVol.Files
{
    public class PolSysvolFile : SysvolFile
    {
        private List<RegistryEntry> RegistryEntries { get; set; }
        
        public PolSysvolFile(string path) : base(path)
        {
            this.Type = SysvolObjectType.PolFile;
        }
        
        private byte[] GetRawFileData()
        {
            byte[] data;
            try
            {
                // try to get a byte array
                data = File.ReadAllBytes(this.Path);
                if (data.Length > 0)
                {
                    return data;
                }
            }
            catch (Exception e)
            {
                Output.DebugWrite(e.ToString());
                return null;
            }
            return null;
        }

        public List<RegistryEntry> GetFileData()
        {
            // try to get the data and default to a null return if failed
            byte[] data = this.GetRawFileData();
            if (data == null) return null;
            
            // TODO: parse the fucking thing
            return null;
        }
    }
}