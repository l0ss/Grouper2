using System;
using System.Collections.Generic;
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
                    if (GlobalVar.OnlineChecks)
                    {
                        if (Utility.DoesFileExist(cmdLine))
                        {
                            bool cmdLineWritable = Utility.CanIWrite(cmdLine);
                            if (cmdLineWritable)
                            {
                                interestLevel = 10;
                                assessedScriptIni.Add("Path is writable", "True");
                                //Utility.DebugWrite("writable path " + cmdLine);
                            }
                            assessedScriptIni.Add("Target file exists", "True");
                        }
                        else
                        {
                            assessedScriptIni.Add("Target file exists", "False");
                            interestLevel = 7;
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
