using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
                Log.Degub($"failed to read a pol file {this.Path}", e, this);
                Output.DebugWrite(e.ToString());
                return null;
            }
            return null;
        }

        public DotPolFileContents GetPolFileData()
        {
            /*
             * The general idea here is to extract data from a ridiculous file.
             *
             * The file will have a format like the following (without linebreaks)
             *
             * FILESTART
             * PRef<VERSION INT32>
             * [<UNICODE KEY NAME>;<DWORD INT32: VALUE TYPE>;<DWORD STRING: VALUE LENGTH INTEGER>;<BINARY VALUE OBJECT>]
             * ...... snip .......
             * [<UNICODE KEY NAME>;<DWORD INT32: VALUE TYPE>;<DWORD STRING: VALUE LENGTH INTEGER>;<BINARY VALUE OBJECT>]
             * EOF
             * 
             */
            
            // try to get the data and default to a null return if failed
            byte[] data = this.GetRawFileData();
            
            // make sure what we got is workable
            // anything less than 8 bytes cannot have any meaningful data
            if (data == null || data.Length < 8) return null;
            
            // we need a stringified version to do index lookups
            string rawString = System.Text.Encoding.Default.GetString(data);
            
            // set up the data
            int index = 0;
            DotPolFileContents contents = new DotPolFileContents {Entries = new List<RegistryEntry>()};

            // the first 4 bytes are an ASCII signature
            // the expected value is: 0x67655250 aka "PRef"
            string sign = Encoding.ASCII.GetString(data.PwshRng(0,3));
            contents.Signature = sign;
            if (!sign.Equals("PRef"))
                throw new FormatException("Encountered a malformed .pol file");
            index += 4;
            
            // the second 4 are a contiguous integer
            // the expected value is: 0x00000001 aka "1" but it is possible for the version to increment
            int vers = BitConverter.ToInt32(data, 4);
            contents.Version = vers;
            index += 4;
            
            // policy data starts at index 8
            while(index < data.Length -2)
            {
                // setup an object
                RegistryEntry reg = new RegistryEntry();
                // the real reward is the semicolons we encounter along the way

                # region GetTheKeyName

                string keyName;
                // the first unicode char (2 bytes) from this index should be '['
                if (BitConverter.ToChar(data, index).Equals('['))
                {
                    // set the index to be after the "["
                    index += 2;
                }
                else
                {
                    throw new FormatException("Encountered a malformed .pol file");
                }

                // the unicode text up to a ';' is the key value
                int lastSemicolon = rawString.IndexOf(';', index);
                if (lastSemicolon != 0)
                {
                    // take until 3 bytes before the keyValueEnd to avoid including it and a nullbyte
                    keyName = Encoding.Unicode.GetString(data.PwshRng(index, lastSemicolon - 3));
                }
                else
                {
                    // we didn't find the end of the unicode string
                    throw new FormatException("Encountered a malformed .pol file");
                }

                // make sure we got _something_
                if (!string.IsNullOrWhiteSpace(keyName))
                {
                    // set the value and update the index
                    reg.Key = keyName;
                }
                else
                {
                    throw new FormatException("Encountered a malformed .pol file");
                }
                
                // set the index to be the character after the ';'
                index = lastSemicolon + 2;

                #endregion

                // At the start of this region, the index should be placed 2 bytes after the previously encountered ";"
                #region ValueTypeDWORD

                // set the semicolon tracker to where the next one _should_ be
                // we expect the DWORD here to be 4 bytes
                lastSemicolon = index + 4;
                
                // check that the format is as it is expected to be
                var shouldBeASemicolon = BitConverter.ToChar(data, lastSemicolon);
                if (shouldBeASemicolon.Equals(';'))
                { 
                    // it appears to be formatted correctly. extract the DWORD
                    int valType = BitConverter.ToInt32(data, index);
                    // convert the DWORD into our enum structure for comparison later
                    reg.ValueType = (RegistryEntry.RegistryEntryCategory) valType;
                }
                else
                {
                    throw new FormatException("Encountered a malformed .pol file");
                }
                
                // set the index to be the character after the ';'
                index = lastSemicolon + 2;

                #endregion

                // this region should start with the index placed ready to read another DWORD
                #region ValueLengthDWORD

                // set the semicolon tracker to where the next one _should_ be
                // we expect the DWORD here to be 4 bytes
                lastSemicolon = index + 4;
                
                // check that the format is as it is expected to be
                shouldBeASemicolon = BitConverter.ToChar(data, lastSemicolon);
                if (shouldBeASemicolon.Equals(';'))
                { 
                    // it appears to be formatted correctly. extract the DWORD as a char
                    var valLenStr = GetIntFromDWORDString(data.PwshRng(index, index + 3));


                }
                else
                {
                    throw new FormatException("Encountered a malformed .pol file");
                }

                #endregion



            }
            
            // TODO: parse the fucking thing
            return null;
        }

        private RegValueLength GetIntFromDWORDString(byte[] dword)
        {
            long result = 0;
            RegValueLength regValueLength;
            if (dword.Length < 4)
            {
                regValueLength = new RegValueLength("Int32");
            }
            else if (dword.Length < 8)
            {
                regValueLength = new RegValueLength("Int64");
            }
            else
            {
                throw new FormatException("Encountered a malformed DWORD in a .pol file");
            }
            
            // reverse for loop through the dword
            // from pwsh: for ($i = $ValueString.Length - 1 ; $i -ge 0 ; $i -= 1)
            for (int i = dword.Length - 1; i >= 0; i--)
            {
                // left bitwise shift on result by one byte (from pwsh: $result = $result -shl 8)
                result <<= 8;
                
                // get the byte at this index, cast to char, then to long (from pwsh: $result = $result + ([int][char]$ValueString[$i]))
                result += (long) (char) dword[i];
            }
            
            // finish the result up
            regValueLength.Length = result;
            return regValueLength;
        }
    }
}