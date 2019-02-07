using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class FileSystem
    {
        public static JObject InvestigatePath(string pathToInvestigate)
        {
            // general purpose method for returning some information about why a path might be interesting.
            
            // set up all our bools and empty JObjects so everything is boring until proven interesting.
            JArray interestingFileExts = (JArray) JankyDb.Instance["interestingExtensions"];
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
            JArray interestingWordsFromFile = new JArray();
            string dirPath = "";
            // remove quotes
            string inPath = pathToInvestigate.Trim('\'','\"',',',';');
            // and whitespace
            inPath = inPath.Trim();

            if (inPath.Length > 1)
            {
                try {
                    dirPath = Path.GetDirectoryName(inPath);
                    fileExt = Path.GetExtension(inPath);
                }
                catch (ArgumentException)
                {
                    // can happen if "inPath" contains invalid characters (ex. '"') or does not look like a path (ex. "mailto:...")
                    return new JObject(new JProperty("Not a path?", inPath));
                }
            }
            else
            {
                return new JObject(new JProperty("Not a path?", inPath));
            }

            if (inPath.Contains("http://") || inPath.Contains("https://"))
            {
                return new JObject(new JProperty("HTTP/S URL?", inPath));
            }

            if (inPath.Contains("://") && !(inPath.Contains("http://")))
            {
                return new JObject(new JProperty("URI?", inPath));
            }

            if (inPath.Contains('%'))
            {
                return new JObject(new JProperty("Env var found in path", inPath));
            }

            if (inPath.StartsWith("C:") || inPath.StartsWith("D:"))
            {
                return new JObject(new JProperty("Local Drive?", inPath));
            }

            // if it doesn't seem to have any path separators it's probably a single file on sysvol.
            if (!inPath.Contains('\\') && !inPath.Contains('/'))
            {
                return new JObject(new JProperty("No path separators, file in SYSVOL?", inPath));
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
                    fileReadable = CanIRead(inPath);
                    // check if we can write it
                    fileWritable = CanIWrite(inPath);
                    // see what the file extension is and if it's interesting
                    fileExt = Path.GetExtension(inPath);
                    foreach (string intExt in interestingFileExts)
                    {
                        if ((fileExt.ToLower().Trim('.')) == (intExt.ToLower()))
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
                            interestingWordsFromFile = Utility.GetInterestingWordsFromFile(inPath);
                            if (interestingWordsFromFile.Count > 0)
                            {
                                fileContentsInteresting = true;
                            }
                        }
                    }

                    // get the file permissions
                    fileDacls = Utility.GetFileDaclJObject(inPath);
                }
                
            }

            if (isDirPath)
            {
                dirExists = DoesDirExist(inPath);
            }
            else if (!isDirPath && !fileExists)
            {
                dirExists = DoesDirExist(dirPath);
            }

            if (dirExists)
            {
                dirDacls = Utility.GetFileDaclJObject(dirPath);
                dirWritable = CanIWrite(dirPath);
            }
            // if the dir doesn't exist, iterate up the file path checking if any exist and if we can write to any of them.
            if (!dirExists)
            {
                // we want to allow a path like C: but not one like "\"
                if ((dirPath != null) && (dirPath.Length > 1))
                {
                    // get the root of the path
                    try
                    {
                        // ReSharper disable once UnusedVariable
                        string pathRoot = Path.GetPathRoot(dirPath);
                    }
                    catch (ArgumentException e)
                    {
                        Utility.DebugWrite(e.ToString());
                        
                        return new JObject(new JProperty("Not a path?", inPath));
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
                        Utility.DebugWrite(e.ToString());
                        
                        return new JObject(new JProperty("Not a path?", inPath));
                    }

                    // iterate until the path root 
                    while ((dirPathParent != null) && (dirPathParent != "\\\\") && (dirPathParent != "\\"))
                    {
                        // check if the parent dir exists
                        parentDirExists = DoesDirExist(dirPathParent);
                        // if it does
                        if (parentDirExists)
                        {
                            // get the dir dacls
                            parentDirDacls = Utility.GetFileDaclJObject(dirPathParent);
                            // check if it's writable
                            parentDirWritable = CanIWrite(dirPathParent);
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
            JObject filePathAssessment = new JObject();
            int interestLevel = 1;
            filePathAssessment.Add("Path assessed", inPath);
            if (isFilePath)
            {
                if (fileExists)
                {
                    filePathAssessment.Add("File exists", true);
                    if (extIsInteresting)
                    {
                        interestLevel = interestLevel + 2;
                        filePathAssessment.Add("File extension interesting", extIsInteresting);
                    }
                    filePathAssessment.Add("File readable", fileReadable);
                    if (fileContentsInteresting)
                    {
                        filePathAssessment.Add("File contents interesting", "True");
                        filePathAssessment.Add("Interesting strings found", interestingWordsFromFile );
                        interestLevel = interestLevel + 2;
                    }
                    filePathAssessment.Add("File writable", fileWritable);
                    if (fileWritable) interestLevel = interestLevel + 10;
                    if ((fileDacls != null) && fileDacls.HasValues)
                    {
                        filePathAssessment.Add("File DACLs", fileDacls);
                    }
                }
                else
                {
                    filePathAssessment.Add("File exists", false);
                    filePathAssessment.Add("Directory exists", dirExists);
                    if (dirExists)
                    {
                        filePathAssessment.Add("Directory writable", dirWritable);
                        if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                        {
                            if (dirWritable) interestLevel = interestLevel + 10;
                        }

                        if ((dirDacls != null) && dirDacls.HasValues)
                        {
                            filePathAssessment.Add("Directory DACL", dirDacls);
                        }
                    }
                    else if (parentDirExists)
                    {
                        filePathAssessment.Add("Parent dir exists", true);
                        if (parentDirWritable)
                        {
                            filePathAssessment.Add("Parent dir writable", "True");
                            if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                            {
                                interestLevel = interestLevel + 10;
                            }
                            filePathAssessment.Add("Writable parent dir", writableParentDir);
                        }
                        else
                        {
                            filePathAssessment.Add("Extant parent dir", extantParentDir);
                            filePathAssessment.Add("Parent dir DACLs", parentDirDacls);
                        }
                    }
                }
            }
            else if (isDirPath)
            {
                filePathAssessment.Add("Directory exists", dirExists);
                if (dirExists)
                {
                    filePathAssessment.Add("Directory is writable", dirWritable);
                    // quick n dirty way of excluding local drives while keeping mapped network drives.
                    if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                    {
                        if (dirWritable) interestLevel = interestLevel + 10;
                    }
                    filePathAssessment.Add("Directory DACLs", dirDacls);
                }
                else if (parentDirExists)
                {
                    filePathAssessment.Add("Parent dir exists", true);
                    if (parentDirWritable)
                    {
                        filePathAssessment.Add("Parent dir writable", "True");
                        if (!(inPath.StartsWith("C:") || inPath.StartsWith("D:")))
                        {
                            interestLevel = interestLevel + 10;
                        }
                        filePathAssessment.Add("Writable parent dir", writableParentDir);
                    }
                    else
                    {
                        filePathAssessment.Add("Extant parent dir", extantParentDir);
                        filePathAssessment.Add("Parent dir DACLs", parentDirDacls);
                    }
                }
            }
            filePathAssessment.Add("InterestLevel", interestLevel.ToString());
            return filePathAssessment;
        }

        public static string GetParentDirPath(string dirPath)
        {
            int count = dirPath.Length - dirPath.Replace("\\", "").Length;

            if (count < 1)
            {
                return null;
            }

            int lastDirSepIndex = Utility.IndexOfNth(dirPath, "\\", count);
            
            string parentPath = dirPath.Remove(lastDirSepIndex);
            
            return parentPath;
        }

        public static bool DoesFileExist(string inPath)
        {
            if (!GlobalVar.OnlineChecks)
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
                Utility.DebugWrite("Checked if file " + inPath +
                                       " exists but it doesn't seem to be a valid file path.");
            }
            catch (UnauthorizedAccessException)
            {
                Utility.DebugWrite("Tried to check if file " + inPath +
                                       " exists but I'm not allowed.");
            }
            return fileExists;
        }

        public static bool DoesDirExist(string inPath)
        {
            if (!GlobalVar.OnlineChecks)
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
                Utility.DebugWrite("Checked if directory " + inPath + " exists but it doesn't seem to be a valid file path.");
            }
            return dirExists;
        }

        public static bool CanIRead(string inPath)
        {
            bool canRead = false;
            if (!GlobalVar.OnlineChecks)
            {
                return false;
            }
            try
            {
                FileStream stream = File.OpenRead(inPath);
                canRead = stream.CanRead;
                stream.Close();
            }
            catch (UnauthorizedAccessException)
            {
                Utility.DebugWrite("Tested read perms for " + inPath + " and couldn't read.");
            }
            catch (ArgumentException)
            {
                Utility.DebugWrite("Tested read perms for " + inPath + " but it doesn't seem to be a valid file path.");
            }
            catch (Exception e)
            {
                Utility.DebugWrite(e.ToString());
            }
            return canRead;
        }

        public static bool CanIWrite(string inPath)
        {
            // this will return true if write or modify or take ownership or any of those other good perms are available.
            
            CurrentUserSecurity currentUserSecurity = new CurrentUserSecurity();

            FileSystemRights[] fsRights = {
                FileSystemRights.Write,
                FileSystemRights.Modify,
                FileSystemRights.FullControl,
                FileSystemRights.TakeOwnership,
                FileSystemRights.ChangePermissions,
                FileSystemRights.AppendData,
                FileSystemRights.CreateFiles,
                FileSystemRights.CreateDirectories,
                FileSystemRights.WriteData
            };

            try
            {
                FileAttributes attr = File.GetAttributes(inPath);
                foreach (FileSystemRights fsRight in fsRights)
                {
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(inPath);
                        return currentUserSecurity.HasAccess(dirInfo, fsRight);
                    }

                    FileInfo fileInfo = new FileInfo(inPath);
                    return currentUserSecurity.HasAccess(fileInfo, fsRight);
                }
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            return false;
        }

        public static JObject InvestigateFileContents(string inString)
        {
            string fileString;
            JObject investigatedFileContents = new JObject();
            try
            {
                fileString = File.ReadAllText(inString).ToLower();

                // feed the whole thing through Utility.InvestigateString
                investigatedFileContents = Utility.InvestigateString(fileString);
            }
            catch (UnauthorizedAccessException e)
            {
                Utility.DebugWrite(e.ToString());
            }
            
            if (investigatedFileContents["InterestLevel"] != null)
            {
                if (((int)investigatedFileContents["InterestLevel"]) >= GlobalVar.IntLevelToShow)
                {
                    investigatedFileContents.Remove("Value");
                    investigatedFileContents.AddFirst(new JProperty("File Path", inString));
                    return investigatedFileContents;
                }
            }

            return null;
        }
    }
}
