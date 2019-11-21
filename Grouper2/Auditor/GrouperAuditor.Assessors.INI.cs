using System;
using System.Collections.Generic;
using Grouper2.Host.SysVol;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        public Finding Audit(IniSysvolFile file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            try
            {
                var data = file.Parse();
                var assessed = AssessScriptsDotIni(data);
                
                
                AuditedScriptsDotIni findings = new AuditedScriptsDotIni()
                {
                    Classification = SysvolObjectType.ScriptFile,
                    Path = file.Path,
                    Interest = 0,
                    Scripts = assessed
                };

                foreach (AuditedScriptsDotIniType auditedScriptsDotIniType in assessed)
                {
                    findings.TryBumpInterest(auditedScriptsDotIniType);
                }
            
                // only return the findings if there is data in the scripts
                return findings.Scripts == null 
                    ? null 
                    : findings;
            }
            catch (Exception e)
            {
                Log.Degub("unable to assess scripts.ini or to parse it into a json object", e, file);
                throw;
            }
        }

        

        private AuditedPath GetCmdData(string cmdLine)
        {
            if (cmdLine == null)
                return null;
            // add cmdLine to result
            if (cmdLine.Length > 0)
            {
                AuditedPath investigatedCommand = FileSystem.InvestigatePath(cmdLine);
                if (investigatedCommand != null)
                {
                    return investigatedCommand;
                }
            }

            return null;
        }
        
        private AuditedString GetPropData(string props)
        {
            if (props == null)
                return null;
            // add cmdLine to result
            if (props.Length > 0)
            {
                AuditedString propData = FileSystem.InvestigateString(props, this.InterestLevel);
                if (propData != null)
                {
                    return propData;
                }
            }
            return null;
        }
        
        private List<AuditedScriptsDotIniType> AssessScriptsDotIni(List<IniSection> iniData)
        {
            if (iniData == null) 
                throw new ArgumentNullException(nameof(iniData));
            List<AuditedScriptsDotIniType> ret = new List<AuditedScriptsDotIniType>();

            foreach (IniSection section in iniData)
            {
                // a section represents a type
                AuditedScriptsDotIniType sType = new AuditedScriptsDotIniType()
                {
                    Name = section.Name,
                    Interest = 4,
                    Findings = new List<AuditedScriptsDotIniScript>()
                };
                
                // the subsections each represent a script
                foreach (IniSubsection subsection in section.Subsections)
                {
                    AuditedScriptsDotIniScript script = new AuditedScriptsDotIniScript()
                    {
                        Num = subsection.Num,
                        Interest = 4
                    };
                    
                    // we need to parse their properties
                    foreach (IniProp prop in subsection.Properties)
                    {
                        if (prop.Key.Contains("CmdLine"))
                        {
                            try
                            {
                                // audit the cmdline
                                var cmdLine = GetCmdData(prop.Value);

                                if (cmdLine != null && script.Commandline == null)
                                {
                                    // adjust the interest
                                    script.TryBumpInterest(cmdLine);
                                    script.Commandline = cmdLine;
                                }
                            }
                            catch (Exception e)
                            {
                                // only worry about this during debugging. this is an acceptable loss
                                Log.Degub("unable to process a cmdline from a scripts.ini", e);
                            }
                        } else if (prop.Key.Contains("Parameters") && script.Params == null)
                        {
                            try
                            {
                                // audit the params
                                AuditedString investigatedParams = GetPropData(prop.Value);

                                if (investigatedParams != null)
                                {
                                    script.TryBumpInterest(investigatedParams);
                                    script.Params = investigatedParams;
                                }
                            }
                            catch (Exception e)
                            {
                                // only worry about this during debugging. this is an acceptable loss
                                Log.Degub("unable to process a params from a scripts.ini", e);
                            }
                        }
                    }
                    
                    //add the script
                    if (script.Interest >= this.InterestLevel)
                    {
                        sType.TryBumpInterest(script.Interest);
                        sType.Findings.Add(script);
                    }
                    
                }

                if (sType.Findings.Count > 0 && sType.Interest >= this.InterestLevel)
                {
                    ret.Add(sType);
                }
                
            }
            
            return ret;
        }
    }
}