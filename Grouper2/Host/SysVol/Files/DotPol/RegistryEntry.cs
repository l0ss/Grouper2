using System.Collections.Generic;

namespace Grouper2.Host.SysVol.Files
{
    public class DotPolFileContents
    {
        public string Signature { get; set; } // the first 4 bytes of the file
        public int Version { get; set; } // the section 4 bytes of the file
        public List<RegistryEntry> Entries { get; set; }
    }

    public class RegValueLength
    {
        public string Type { get; }
        public long Length { get; set; }

        public RegValueLength(string type)
        {
            this.Type = type;
        }
    }
    
    
    public class RegistryEntry
    {
        public string Key { get;  set; }
        public string Value { get;  set; }
        public RegistryEntryCategory ValueType { get; set; }
        public int ValueLength { get; set; }
        public string ValueData { get;  set; }

        public RegistryEntry()
        {
            
        }

        /// <summary>
        /// Enum for converting the int in the file into the registry value type string
        /// </summary>
        public enum RegistryEntryCategory
        {
            // hooray for the internet for knowing these values!!!!
            REG_NONE = 0,	// No value type
            REG_SZ = 1,	// Unicode null terminated string
            REG_EXPAND_SZ = 2,	// Unicode null terminated string (with environmental variable references)
            REG_BINARY = 3,	// Free form binary
            REG_DWORD = 4,	// 32-bit number
            REG_DWORD_BIG_ENDIAN = 5,	// 32-bit number
            REG_LINK = 6,	// Symbolic link (Unicode)
            REG_MULTI_SZ = 7,	// Multiple Unicode strings, delimited by \0, terminated by \0\0
            REG_RESOURCE_LIST = 8,  // Resource list in resource map
            REG_FULL_RESOURCE_DESCRIPTOR = 9,  // Resource list in hardware description
            REG_RESOURCE_REQUIREMENTS_LIST = 10,
            REG_QWORD = 11, // 64-bit number
        }

		
    }
}
