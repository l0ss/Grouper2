using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class AssessScriptsIni
    {
        public static JObject GetAssessedScriptsIni(JObject parsedScriptsIni)
        {
            JObject assessedScriptsIni = new JObject();
            //Utility.DebugWrite(parsedScriptsIni.ToString());

            foreach (KeyValuePair<string, JToken> parsedScriptIniType in parsedScriptsIni)
            {
                // JObject for everything from the 'type' to go into
                JObject assessedScriptIniType = new JObject();
                // get the type of scripts we're looking at i.e. Startup, Shutdown, etc.
                string scriptType = parsedScriptIniType.Key;
                // cast the JToken to a JObj
                JObject parsedScriptIniTypeJObject = (JObject) parsedScriptIniType.Value;
                // iterate over individual scripts
                foreach (KeyValuePair<string, JToken> parsedScript in parsedScriptIniTypeJObject)
                {
                    int interestLevel = 5;
                    // set up script results object
                    JObject assessedScriptIni = new JObject();
                    // get the unique ID of this script
                    string scriptNum = parsedScript.Key;
                    string parameters = "";
                    string cmdLine = parsedScript.Value["CmdLine"].ToString();
                    // params are optional, handle it if it's missing.
                    try
                    {
                        parameters = parsedScript.Value["Parameters"].ToString();
                    }
                    catch (System.NullReferenceException e)
                    {
                        //Utility.DebugWrite(e.ToString());
                    }

                    // add cmdLine to result
                    assessedScriptIni.Add("Command Line", cmdLine);
                    if (parameters.Length > 0)
                    {
                        assessedScriptIni.Add("Parameters", parameters);
                    }
                    // check if the target file path is vulnerable
                    //TODO some logic to enumerate file ACLS

                    if (GlobalVar.OnlineChecks && (cmdLine.Length > 0))
                    {
                        if (Utility.DoesFileExist(cmdLine))
                        {
                            assessedScriptIni.Add("Source file exists", "True");
                            bool writable = false;
                            // get the file permissions
                            JObject fileDacls = Utility.GetFileDaclJObject(cmdLine);
                            if (fileDacls.HasValues)
                            {
                                interestLevel = 8;
                                assessedScriptIni.Add("File Permissions", fileDacls);
                            }
                            // check if the file is writable
                            writable = Utility.CanIWrite(cmdLine);
                            if (writable)
                            {
                                interestLevel = 10;
                                assessedScriptIni.Add("Source file writable", "True");
                            }
                            else
                            {
                                assessedScriptIni.Add("Source file writable", "False");
                            }

                        }
                        else
                        {
                            assessedScriptIni.Add("Source file exists", "False");
                            string directoryName = Path.GetDirectoryName(cmdLine);
                            JObject directoryDacls = Utility.GetFileDaclJObject(directoryName);
                            interestLevel = 7;
                            assessedScriptIni.Add("Directory Permissions", directoryDacls);
                        }

                    }

                    if (interestLevel >= GlobalVar.IntLevelToShow)
                    {
                        assessedScriptIniType.Add(scriptNum, assessedScriptIni);
                    }
                }
                
                // add all the results from the type to the object being returned
                if (assessedScriptIniType.HasValues)
                {
                    assessedScriptsIni.Add(scriptType, assessedScriptIniType);
                }
            }

            if (assessedScriptsIni.HasValues)
            {
                JObject scriptsIniResults = new JObject()
                {
                    {"Scripts", assessedScriptsIni }
                };
                return scriptsIniResults;
            }
            else
            {
                return null;
            }
 
        }
    }
}
