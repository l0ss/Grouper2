using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Grouper2.Auditor;
using Grouper2.Host;
using Grouper2.Host.DcConnection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Grouper2.Utility
{
    class FileSystem
    {
        

        public static List<string> FindFilePathsInString(string inString)
        {
            if (inString == null) throw new ArgumentNullException(nameof(inString));

            return inString.Split(' ').Select(s => s.Trim('\'', '\"')).ToList();
        }


        public static AuditedString InvestigateString(string inString, int desiredInterestLevel)
        // general purpose method for returning some information about why a string might be interesting.
        {
            AuditedString investigated = new AuditedString()
            {
                Value = inString,
                Interest = 0
            };

            // make a list to put any interesting words we find in it
            // refer to our master list of interesting words
            List<string> interestingWords = JankyDb.Db.InterestingWords.ToList();
            foreach (string interestingWord in interestingWords)
            {
                if (inString.ToLower().Contains(interestingWord))
                {
                    investigated.InterestingWords.Add(interestingWord);
                    investigated.Interest = 4;
                }
            }

            List<string> foundFilePaths = FindFilePathsInString(inString);

            foreach (string foundFilePath in foundFilePaths)
            {
                AuditedPath investigatedPath = InvestigatePath(foundFilePath);

                if (investigatedPath != null)
                {
                    if (investigatedPath.Interest >= desiredInterestLevel)
                    {
                        investigated.InterestingPaths.Add(investigatedPath);
                    }
                }
            }

            return investigated;

        }


        public static bool IsValidPath(string path, bool allowRelativePaths = false)
        {
            // lifted from Dao Seeker on stackoverflow.com https://stackoverflow.com/questions/6198392/check-whether-a-path-is-valid
            bool isValid;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (allowRelativePaths)
                {
                    isValid = Path.IsPathRooted(path);
                }
                else
                {
                    string root = Path.GetPathRoot(path);
                    isValid = String.IsNullOrEmpty(root.Trim('\\', '/')) == false;
                }
            }
            catch (Exception)
            {
                isValid = false;
            }

            return isValid;
        }

        public static JObject GetFileDaclJObject(string filePathString)
        {
            int inc = 0;
            int interest = JankyDb.Vars.Interest;
            string[] interestingTrustees = new string[] { "Everyone", "BUILTIN\\Users", "Authenticated Users", "Domain Users", "INTERACTIVE", };
            string[] boringTrustees = new string[] { "TrustedInstaller", "Administrators", "NT AUTHORITY\\SYSTEM", "Domain Admins", "Enterprise Admins", "Domain Controllers" };
            string[] interestingRights = new string[] { "FullControl", "Modify", "Write", "AppendData", "TakeOwnership" };
            string[] boringRights = new string[] { "Synchronize", "ReadAndExecute" };

            if (!JankyDb.Vars.OnlineMode)
            {
                return null;
            }
            // object for result
            JObject fileDaclsJObject = new JObject();

            FileSecurity filePathSecObj;
            try
            {
                filePathSecObj = File.GetAccessControl(filePathString);
            }
            catch (ArgumentException e)
            {
                Utility.Output.DebugWrite("Tried to check file permissions on invalid path: " + filePathString);
                Utility.Output.DebugWrite(e.ToString());
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                Utility.Output.DebugWrite(e.ToString());

                return null;
            }

            AuthorizationRuleCollection fileAccessRules =
                filePathSecObj.GetAccessRules(true, true, typeof(SecurityIdentifier));

            foreach (FileSystemAccessRule fileAccessRule in fileAccessRules)
            {
                // get inheritance and access control type values
                string isInheritedString = "False";
                if (fileAccessRule.IsInherited) isInheritedString = "True";
                string accessControlTypeString = "Allow";
                if (fileAccessRule.AccessControlType == AccessControlType.Deny) accessControlTypeString = "Deny";

                // get the user's SID
                string sid = fileAccessRule.IdentityReference.ToString();
                string displayNameString = Ldap.Use().GetUserFromSid(sid);
                // do some interest level analysis
                bool trusteeBoring = false;
                bool trusteeInteresting = false;
                // check if our trustee is boring
                foreach (string boringTrustee in boringTrustees)
                {
                    // if we're showing everything that's fine, keep going
                    if (interest == 0)
                    {
                        break;
                    }
                    // otherwise if the trustee is boring, set the interest level to 0
                    if (displayNameString.ToLower().EndsWith(boringTrustee.ToLower()))
                    {
                        trusteeBoring = true;
                        // and don't bother comparing rest of array
                        break;
                    }
                }
                // skip rest of access rule if trustee is boring and we're not showing int level 0
                if (interest != 0 && trusteeBoring)
                {
                    continue;
                }
                // see if the trustee is interesting
                foreach (string interestingTrustee in interestingTrustees)
                {
                    if (displayNameString.ToLower().EndsWith(interestingTrustee.ToLower()))
                    {
                        trusteeInteresting = true;
                        break;
                    }
                }
                // get the rights
                string fileSystemRightsString = fileAccessRule.FileSystemRights.ToString();
                // strip spaces
                fileSystemRightsString = fileSystemRightsString.Replace(" ", "");
                // turn them into an array
                string[] fileSystemRightsArray = fileSystemRightsString.Split(',');
                // then do some 'interest level' analysis
                // JArray for output
                JArray fileSystemRightsJArray = new JArray();
                foreach (string right in fileSystemRightsArray)
                {
                    bool rightInteresting = false;
                    bool rightBoring = false;

                    foreach (string boringRight in boringRights)
                    {
                        if (right.ToLower() == boringRight.ToLower())
                        {
                            rightBoring = true;
                            break;
                        }
                    }

                    foreach (string interestingRight in interestingRights)
                    {
                        if (right.ToLower() == interestingRight.ToLower())
                        {
                            rightInteresting = true;
                            break;
                        }
                    }

                    // if we're showing defaults, just add it to the result and move on
                    if (interest == 0)
                    {
                        fileSystemRightsJArray.Add(right);
                        continue;
                    }
                    // if we aren't, and it's boring, skip it and move on.
                    if (rightBoring)
                    {
                        continue;
                    }
                    // if it's interesting, add it and move on.
                    if (rightInteresting)
                    {
                        fileSystemRightsJArray.Add(right);
                        continue;
                    }
                    // if it's neither boring nor interesting, add it if the 'interestlevel to show' value is low enough
                    else if (interest < 3)
                    {
                        Utility.Output.DebugWrite(right + " was not labelled as boring or interesting.");
                        fileSystemRightsJArray.Add(right);
                    }
                    else
                    {
                        Utility.Output.DebugWrite("Shouldn't hit here, label FS right as boring or interesting." + right);
                    }
                }

                // no point continuing if no rights to show
                if (fileSystemRightsJArray.HasValues)
                {
                    // if the trustee isn't interesting and we're excluding low-level findings, bail out
                    if (!trusteeInteresting && interest > 4)
                    {
                        return null;
                    }
                    // build the object
                    string rightsString = fileSystemRightsJArray.ToString().Trim('[', ']').Trim().Replace("\"", "");

                    JObject fileDaclJObject = new JObject
                    {
                        {accessControlTypeString, displayNameString},
                        {"Inherited?", isInheritedString},
                        {"Rights", rightsString}
                    };
                    // add the object to the array.
                    fileDaclsJObject.Add(inc.ToString(), fileDaclJObject);

                    inc++;
                }
            }
            //DebugWrite(fileDaclsJObject.ToString());
            return fileDaclsJObject;
        }

        // TODO: move this to use the tree structure to make things easier
        public static AuditedPath InvestigatePath(string pathToInvestigate)
        {
            AuditedPath filePathAssessment;
            int interestLevel = 1;
            try
            {
                // general purpose method for returning some information about why a path might be interesting.

                // set up all our bools and empty JObjects so everything is boring until proven interesting.
                JArray interestingFileExts = JArray.FromObject(JankyDb.Db.InterestingExtensions);
                bool fileExists = false;
                bool fileWritable = false;
                bool fileReadable = false;
                bool dirExists = false;
                bool dirWritable = false;
                bool fileContentsInteresting = false;
                bool isFilePath = false;
                bool isDirPath = false;
                bool parentDirExists = false;
                bool parentDirWritable = false;
                bool extIsInteresting = false;
                string fileExt = "";
                string extantParentDir = "";
                string writableParentDir = "";
                JObject parentDirDacls = new JObject();
                JObject fileDacls = new JObject();
                JObject dirDacls = new JObject();
                List<string> interestingWordsFromFile = new List<string>();
                string dirPath = "";
                // remove quotes
                string inPath = pathToInvestigate.Trim('\'', '\"', ',', ';');
                // and whitespace
                inPath = inPath.Trim();

                // return obj
                filePathAssessment = new AuditedPath();

                if (inPath.Length > 1)
                {
                    try
                    {
                        dirPath = Path.GetDirectoryName(inPath);
                        fileExt = Path.GetExtension(inPath);
                    }
                    catch (ArgumentException)
                    {
                        // can happen if "inPath" contains invalid characters (ex. '"') or does not look like a path (ex. "mailto:...")
                        return new AuditedPath() {NotAPath = inPath};
                    }
                }
                else
                {
                    return new AuditedPath() {NotAPath = inPath};
                }

                if (inPath.Contains("http://") || inPath.Contains("https://"))
                {
                    return new AuditedPath() {NotAPathHttps = inPath};
                }

                if (inPath.Contains("://") && !inPath.Contains("http://"))
                {
                    return new AuditedPath() {NotAPathUri = inPath};
                }

                if (inPath.Contains('%'))
                {
                    return new AuditedPath() {NotAPathEnv = inPath};
                }

                if (inPath.StartsWith("C:") || inPath.StartsWith("D:"))
                {
                    return new AuditedPath() {NotAPathDrive = inPath};
                }

                // if it doesn't seem to have any path separators it's probably a single file on sysvol.
                if (!inPath.Contains('\\') && !inPath.Contains('/'))
                {
                    return new AuditedPath() {NoSep = inPath};
                }
                // figure out if it's a file path or just a directory even if the file doesn't exist

                string pathFileComponent = Path.GetFileName(inPath);

                if (pathFileComponent == "")
                {
                    isDirPath = true;
                    isFilePath = false;
                }
                else
                {
                    isDirPath = false;
                    isFilePath = true;
                }

                if (isFilePath)
                {
                    // check if the file exists
                    fileExists = DoesFileExist(inPath);

                    if (fileExists)
                    {
                        // if it does, the parent Dir must exist.
                        dirExists = true;
                        // check if we can read it
                        fileReadable = CurrentUser.Query.CanReadFrom(inPath);
                        // check if we can write it
                        fileWritable = CurrentUser.Query.CanWriteTo(inPath);
                        // see what the file extension is and if it's interesting
                        fileExt = Path.GetExtension(inPath);
                        foreach (string intExt in interestingFileExts)
                        {
                            if (fileExt.ToLower().Trim('.') == intExt.ToLower())
                            {
                                extIsInteresting = true;
                            }
                        }

                        // if we can read it, have a look if it has interesting strings in it.
                        if (fileReadable)
                        {
                            // make sure the file isn't massive so we don't waste ages grepping whole disk images over the network
                            long fileSize = new FileInfo(inPath).Length;

                            if (fileSize < 1048576) // 1MB for now. Can tune if too slow.
                            {
                                interestingWordsFromFile = GetInterestingWordsFromFile(inPath);
                                if (interestingWordsFromFile.Count > 0)
                                {
                                    fileContentsInteresting = true;
                                }
                            }
                        }

                        // get the file permissions
                        fileDacls = GetFileDaclJObject(inPath);
                    }

                }

                if (isDirPath)
                {
                    dirExists = DoesDirExist(inPath);
                }
                else if (!fileExists)
                {
                    dirExists = DoesDirExist(dirPath);
                }

                if (dirExists)
                {
                    dirDacls = GetFileDaclJObject(dirPath);
                    dirWritable = CurrentUser.Query.CanWriteTo(dirPath);
                }

                // if the dir doesn't exist, iterate up the file path checking if any exist and if we can write to any of them.
                if (!dirExists)
                {
                    // we want to allow a path like C: but not one like "\"
                    if (dirPath != null && dirPath.Length > 1)
                    {
                        // get the root of the path
                        try
                        {
                            // ReSharper disable once UnusedVariable
                            string pathRoot = Path.GetPathRoot(dirPath);
                        }
                        catch (ArgumentException e)
                        {
                            Utility.Output.DebugWrite(e.ToString());

                            return new AuditedPath() {NotAPath = inPath};
                        }

                        // get the first parent dir
                        string dirPathParent = "";

                        try
                        {
                            if (GetParentDirPath(dirPath) != null)
                            {
                                dirPathParent = GetParentDirPath(dirPath);
                            }
                        }
                        catch (ArgumentException e)
                        {
                            Utility.Output.DebugWrite(e.ToString());

                            return new AuditedPath() {NotAPath = inPath};
                        }

                        // iterate until the path root 
                        while (dirPathParent != null && dirPathParent != "\\\\" && dirPathParent != "\\")
                        {
                            // check if the parent dir exists
                            parentDirExists = DoesDirExist(dirPathParent);
                            // if it does
                            if (parentDirExists)
                            {
                                // get the dir dacls
                                parentDirDacls = GetFileDaclJObject(dirPathParent);
                                // check if it's writable
                                parentDirWritable = CurrentUser.Query.CanWriteTo(dirPathParent);
                                if (parentDirWritable)
                                {
                                    writableParentDir = dirPathParent;
                                }

                                break;
                            }

                            //prepare for next iteration by aiming at the parent dir
                            if (GetParentDirPath(dirPathParent) != null)
                            {
                                dirPathParent = GetParentDirPath(dirPathParent);
                            }
                            else break;
                        }
                    }
                }

                // put all the values we just collected into a jobject for reporting and calculate how interesting it is.
                //JObject filePathAssessment = new JObject();

                //filePathAssessment.Add("Path assessed", inPath);
                if (isFilePath)
                {
                    // prepare an audited file path to return
                    filePathAssessment.FileData = new AuditedPathFile {FileExists = fileExists};
                    if (fileExists)
                    {

                        //filePathAssessment.Add("File exists", true);

                        // extension stuff
                        filePathAssessment.FileData.ExtIsInteresting = extIsInteresting;
                        if (extIsInteresting)
                        {
                            interestLevel = interestLevel + 2;
                            //filePathAssessment.Add("File extension interesting", extIsInteresting);
                        }

                        // contents stuff
                        filePathAssessment.FileData.Readable = fileReadable;
                        //filePathAssessment.Add("File readable", fileReadable);
                        filePathAssessment.FileData.ContentsInteresting = fileContentsInteresting;
                        if (fileContentsInteresting)
                        {
                            interestLevel = interestLevel + 2;
                            //filePathAssessment.Add("File contents interesting", "True");
                            filePathAssessment.FileData.ContentsStringsOfInterest = interestingWordsFromFile;
                            //filePathAssessment.Add("Interesting strings found", interestingWordsFromFile);
                        }

                        // writable stuff !!!!
                        filePathAssessment.FileData.Writable = fileWritable;
                        //filePathAssessment.Add("File writable", fileWritable);
                        if (fileWritable) interestLevel = interestLevel + 10;
                        if (fileDacls != null && fileDacls.HasValues)
                        {
                            filePathAssessment.FileData.FileDacls = fileDacls;
                            //filePathAssessment.Add("File DACLs", fileDacls);
                        }
                    }
                    else
                    {
                        //filePathAssessment.Add("File exists", false);
                        //filePathAssessment.Add("Directory exists", dirExists);
                        filePathAssessment.DirData = new AuditedPathDir() {DirExists = dirExists};
                        if (dirExists)
                        {
                            filePathAssessment.DirData.Writable = dirWritable;
                            //filePathAssessment.Add("Directory writable", dirWritable);
                            if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                            {
                                if (dirWritable) interestLevel = interestLevel + 10;
                            }

                            if (dirDacls != null && dirDacls.HasValues)
                            {
                                filePathAssessment.DirData.Dacls = dirDacls;
                                //filePathAssessment.Add("Directory DACL", dirDacls);
                            }
                        }
                        else if (parentDirExists)
                        {
                            filePathAssessment.DirData.Parent = new AuditedPathDir {DirExists = parentDirExists};
                            //filePathAssessment.Add("Parent dir exists", true);
                            if (parentDirWritable)
                            {
                                filePathAssessment.DirData.Parent.Writable = parentDirWritable;
                                //filePathAssessment.Add("Parent dir writable", "True");

                                if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                                {
                                    interestLevel = interestLevel + 10;
                                }

                                filePathAssessment.DirData.Parent.Path = writableParentDir;
                                //filePathAssessment.Add("Writable parent dir", writableParentDir);
                            }
                            else
                            {
                                filePathAssessment.DirData.Parent.ExtantParentDir = extantParentDir;
                                filePathAssessment.DirData.Parent.Dacls = parentDirDacls;
                                //filePathAssessment.Add("Extant parent dir", extantParentDir);
                                //filePathAssessment.Add("Parent dir DACLs", parentDirDacls);
                            }
                        }
                    }
                }
                else if (isDirPath)
                {
                    filePathAssessment.DirData = new AuditedPathDir() {DirExists = dirExists};
                    //filePathAssessment.Add("Directory exists", dirExists);
                    if (dirExists)
                    {
                        filePathAssessment.DirData.Writable = dirWritable;
                        //filePathAssessment.Add("Directory is writable", dirWritable);
                        // quick n dirty way of excluding local drives while keeping mapped network drives.
                        if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                        {
                            if (dirWritable) interestLevel = interestLevel + 10;
                        }

                        filePathAssessment.DirData.Dacls = dirDacls;
                        //filePathAssessment.Add("Directory DACLs", dirDacls);
                    }
                    else if (parentDirExists)
                    {
                        filePathAssessment.DirData.Parent = new AuditedPathDir() {DirExists = parentDirExists};
                        //filePathAssessment.Add("Parent dir exists", true);
                        if (parentDirWritable)
                        {
                            filePathAssessment.DirData.Parent.Writable = parentDirWritable;
                            //filePathAssessment.Add("Parent dir writable", "True");
                            if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                            {
                                interestLevel = interestLevel + 10;
                            }

                            filePathAssessment.DirData.Parent.Path = writableParentDir;
                            //filePathAssessment.Add("Writable parent dir", writableParentDir);
                        }
                        else
                        {
                            filePathAssessment.DirData.Parent.ExtantParentDir = extantParentDir;
                            filePathAssessment.DirData.Parent.Dacls = parentDirDacls;
                            //filePathAssessment.Add("Extant parent dir", extantParentDir);
                            //filePathAssessment.Add("Parent dir DACLs", parentDirDacls);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Log.Degub("unable to build a filepath assessment");
                filePathAssessment = null;
            }

            if (filePathAssessment != null)
            {
                filePathAssessment.Interest = interestLevel;
                //filePathAssessment.Add("InterestLevel", interestLevel.ToString());
                return filePathAssessment;
            }

            return null;

        }


        public static List<string> GetInterestingWordsFromFile(string inPath)
        {
            // validate if the file exists
            bool fileExists = FileSystem.DoesFileExist(inPath);
            if (!fileExists)
            {
                return null;
            }

            // get our list of interesting words
            JArray interestingWords = JArray.FromObject(JankyDb.Db.InterestingWords);

            // get contents of the file and smash case
            string fileContents = "";
            try
            {
                fileContents = File.ReadAllText(inPath).ToLower();
            }
            catch (UnauthorizedAccessException e)
            {
                Utility.Output.DebugWrite(e.ToString());
            }

            // set up output object
            List<string> interestingWordsFound = new List<string>();

            foreach (string word in interestingWords)
            {
                if (fileContents.Contains(word))
                {
                    interestingWordsFound.Add(word);
                }
            }

            return interestingWordsFound;
        }

        public static string GetParentDirPath(string dirPath)
        {
            int count = dirPath.Length - dirPath.Replace("\\", "").Length;

            if (count < 1)
            {
                return null;
            }

            int lastDirSepIndex = Util.IndexOfNth(dirPath, "\\", count);
            
            string parentPath = dirPath.Remove(lastDirSepIndex);
            
            return parentPath;
        }

        public static bool DoesFileExist(string inPath)
        {
            if (!JankyDb.Vars.OnlineMode)
            {
                return false;
            }
            bool fileExists = false;
            try
            {
                fileExists = File.Exists(inPath);
            }
            catch (ArgumentException)
            {
                Utility.Output.DebugWrite("Checked if file " + inPath +
                                       " exists but it doesn't seem to be a valid file path.");
            }
            catch (UnauthorizedAccessException)
            {
                Utility.Output.DebugWrite("Tried to check if file " + inPath +
                                       " exists but I'm not allowed.");
            }
            return fileExists;
        }

        public static bool DoesDirExist(string inPath)
        {
            if (!JankyDb.Vars.OnlineMode)
            {
                return false;
            }
            bool dirExists = false;
            try
            {
                dirExists = Directory.Exists(inPath);
            }
            catch (ArgumentException)
            {
                Utility.Output.DebugWrite("Checked if directory " + inPath + " exists but it doesn't seem to be a valid file path.");
            }
            return dirExists;
        }

        public static AuditedFileContents InvestigateFileContents(string inString, int desiredInterestLevel)
        {
            AuditedFileContents investigated = new AuditedFileContents();

            string fileString;
            //JObject investigatedFileContents = new JObject();
            try
            {
                fileString = File.ReadAllText(inString).ToLower();

                // feed the whole thing through FileSystem.InvestigateString
                investigated.AuditedString = FileSystem.InvestigateString(fileString, desiredInterestLevel);
            }
            catch (UnauthorizedAccessException e)
            {
                Utility.Output.DebugWrite(e.ToString());
            }
            
            if (investigated.AuditedString != null)
            {
                if (investigated.Interest >= desiredInterestLevel)
                {
                    investigated.Path = inString;
                    return investigated;
                }
            }

            return null;
        }
    }
}
