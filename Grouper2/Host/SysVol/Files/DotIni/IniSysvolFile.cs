using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Host.SysVol.Files
{
    public class IniSection
    {
        public string Name { get; set; }
        public List<IniSubsection> Subsections { get; set; }
    }

    public class IniSubsection
    {
        public string Num { get; set; }

        public List<IniProp> Properties { get; set; }
    }

    public class IniProp
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    
    public class IniSysvolFile : SysvolFile
    {
        public IniSysvolFile(string path) : base(path)
        {
            this.Type = SysvolObjectType.IniFile;
        }
        
        public string GetScriptNum(string scriptLine)
        {
            // setup
            string numStr = string.Empty;
            int parsed = 0;
            
            // iterate the characters in the string
            foreach (char c in scriptLine)
            {
                // if it can be parsed to an int
                if (int.TryParse(c.ToString(), out parsed))
                {
                    // add it to the string to return
                    numStr += c;
                } else break; // otherwise, we are done!
            }

            // return whatever it was we built
            return numStr;
        }

        private IniProp DecomposeIniLine(string line)
        {
            
            // split the line at the first equals
            string[] parts;
            try
            {
                if (!line.Contains("="))
                    throw new ArgumentException($"unable to parse an INI prop because there is no = in {line}");
                parts = line.Split(new[] {'='}, 2);
                if (parts.Length != 2)
                    throw new ArgumentException($"unable to parse an INI prop it wasn't in 2 parts: {line}");
            }
            catch (Exception e)
            {
                Log.Degub("Unable to decompose INI line????", e);
                return null;
            }

            // the parts array should have errored out before getting here if not valid
            return new IniProp()
            {
                Key = parts[0],
                Value = parts[1]
            };
        }

        private List<IniSubsection> GetSubsections(string[] lines)
        {
            // sanity checking
            if (lines == null) 
                throw new ArgumentNullException(nameof(lines));
            if (lines.Length == 0) 
                throw new ArgumentException("Value cannot be an empty collection.", nameof(lines));
            
            // setup
            var ret = new List<IniSubsection>();
            
            // bin the lines by the number they start with
            foreach (string line in lines)
            {
                // get the starting number
                var start = GetScriptNum(line);
                // get the line without the starting number
                var data = line.TrimStart(start.ToCharArray());
                
                // check the list for already created things with the same number start
                if (ret.Any(s => s.Num.Equals(start)))
                {
                    // find the correct subsection
                    foreach (IniSubsection subsection in ret)
                    {
                        // if this is the right subsection
                        if (subsection.Num == start)
                        {
                            // add the properties on this line
                            IniProp iniProp = DecomposeIniLine(data);
                            if (iniProp != null) 
                                subsection.Properties.Add(iniProp);

                        }
                    }
                }
                else // it doesn't already exist
                {
                    // so create it
                    var newsubsec = new IniSubsection()
                    {
                        Num = start,
                        Properties = new List<IniProp>()
                    };
                    
                    // add the properties on this line
                    IniProp iniProp = DecomposeIniLine(data);
                    if (iniProp != null) 
                        newsubsec.Properties.Add(iniProp);
                    
                    // add it to the list if it has stuff in it
                    if (newsubsec.Properties != null && newsubsec.Properties.Count > 0)
                    {
                        ret.Add(newsubsec);
                    }
                }
            }

            return ret.Count > 0 
                ? ret 
                : null;
        }

        public List<IniSection> Parse()
        {
            // get ready
            List<IniSection> sections = new List<IniSection>();
            
            // attempt to read the file
            string[] infArr;
            try
            {
                // do the read
                infArr = File.ReadAllLines(this.Path);
                // check there is enough data to continue parsing
                if (infArr.Length < 1)
                {
                    throw new InvalidOperationException("The INI file length is too short to be a valid sysvol INI");
                }
            }
            catch (Exception e)
            {
                // we know not all files will be readable, so lets just ignore errors here and return a blank list
                Utility.Output.DebugWrite("Something went wrong parsing an INI file.\n" + e.ToString());
                return new List<IniSection>();
            }
            
            // we did a successful data collection, so let's go through the array
            // carve the array into sections
            for (int i = 0; i < infArr.Length; i++)
            {
                // once we find a section, we need to handle it
                if (infArr[i].StartsWith("["))
                {
                    // so let's build a new section
                    var section = new IniSection()
                    {
                        Name = infArr[i].Trim().Trim(new []{'[',']'}),
                        Subsections = new List<IniSubsection>()
                    };
                    
                    // then scan forwards to get the string slice for this section
                    string[] sectionSlices = null;
                    for (int j = i + 1; j < infArr.Length; j++)
                    {
                        // break and include if this is the EOF
                        // break if this is the start of a new section
                        if (j + 1 == infArr.Length || infArr[j + 1].StartsWith("["))
                        {
                            // get the slice
                            sectionSlices = infArr.Slice(i + 1, j + 1);
                            // prepare for the next section run
                            i = j;
                            // end this looping
                            break;
                        }
                    }
                    
                    // we made a slice for this section, but let's sanity check it
                    if (sectionSlices == null || sectionSlices.Length < 1)
                    {
                        // it doesn't seem to be a valid section, so let's keep going
                        continue;
                    }
                    
                    // now we know the slice is valid
                    // send it to be processed
                    var subsecs = GetSubsections(sectionSlices);
                    
                    // check we actually got a thing back before we do something
                    if (subsecs != null && subsecs.Count > 0)
                    {
                        // it's valid, so it makes sense to add it
                        section.Subsections = subsecs;
                        
                        // this should conclude the section, but we only want to add it if there were valid subsections
                        sections.Add(section);
                    }
                }
            }

            return sections;
        }
    }
}