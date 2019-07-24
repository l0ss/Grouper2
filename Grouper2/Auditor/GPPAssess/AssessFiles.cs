using System;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private Finding Audit(Files file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedFiles(file.JankyXmlStuff);
        }
        private AuditedGppXmlFiles GetAssessedFiles(JObject input)
        {
            //JObject assessedFiles = new JObject();
            AuditedGppXmlFiles assessedFiles = new AuditedGppXmlFiles();
            var gppCategory = input["Files"];
            if (gppCategory["File"] is JArray)
            {
                foreach (JToken jToken in gppCategory["File"])
                {
                    JObject gppFile = (JObject) jToken;
                    AuditedGppXmlFilesFile assessedFile = GetAssessedFile(gppFile);
                    if (assessedFile != null)
                    {
                        try
                        {
                            assessedFiles.Files.Add(gppFile["@uid"].ToString(), assessedFile);
                        }
                        catch (ArgumentException e)
                        {
                            Log.Degub($"Unable to add an audited files entry with guid {gppFile["@uid"].ToString()}", e, assessedFile);
                        }
                    }
                }
            }
            else
            {
                JObject gppFile = (JObject) JToken.FromObject(gppCategory["File"]);
                AuditedGppXmlFilesFile assessedFile = GetAssessedFile(gppFile);
                if (assessedFile != null)
                {
                    try
                    {
                        assessedFiles.Files.Add(gppFile["@uid"].ToString(), assessedFile);
                    }
                    catch (ArgumentException e)
                    {
                        Log.Degub($"Unable to add an audited files entry with guid {gppFile["@uid"].ToString()}", e, assessedFile);
                    }
                }
            }

            return assessedFiles;
        }

        private AuditedGppXmlFilesFile GetAssessedFile(JObject gppFile)
        {
            int interestLevel = 3;
            AuditedGppXmlFilesFile assessedFile = new AuditedGppXmlFilesFile()
            {
                Name = gppFile["@name"].ToString(),
                Status = gppFile["@status"].ToString(),
                Changed = gppFile["@changed"].ToString(),
                Action = JUtil.GetActionString(gppFile["Properties"]["@action"].ToString()),
            };
            JToken gppFileProps = gppFile["Properties"];
            //assessedFile.Add("Name", gppFile["@name"].ToString());
            //assessedFile.Add("Status", gppFile["@status"].ToString());
            //assessedFile.Add("Changed", gppFile["@changed"].ToString());
            string gppFileAction = JUtil.GetActionString(gppFileProps["@action"].ToString());
            //assessedFile.Add("Action", gppFileAction);
            JToken targetPathJToken = gppFileProps["@targetPath"];
            if (targetPathJToken != null)
            {
                assessedFile.TargetPath = gppFileProps["@targetPath"].ToString();
            }

            JToken fromPathJToken = gppFileProps["@fromPath"];
            if (fromPathJToken != null)
            {
                string fromPath = gppFileProps["@fromPath"].ToString();

                if (fromPath.Length > 0)
                {
                    AuditedPath assessedPath = FileSystem.InvestigatePath(gppFileProps["@fromPath"].ToString());
                    if (assessedPath != null)
                    {
                        assessedFile.FromPath = assessedPath;
                        //assessedFile.Add("From Path", assessedPath);
                        interestLevel = interestLevel + assessedPath.Interest;
                        
                    }
                }

            }

            // if it's too boring to be worth showing, return an empty not jobj.
            return interestLevel <= this.InterestLevel 
                ? null 
                : assessedFile;
        }
    }
}