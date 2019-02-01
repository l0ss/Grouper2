using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class FileSystem
    {
        public static JObject InvestigatePath(string inPath)
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
            //bool extIsInteresting = false;
            string fileExt = "";
            string extantParentDir = "";
            string writableParentDir = "";
            JObject parentDirDacls = new JObject();
            JObject fileDacls = new JObject();
            JObject dirDacls = new JObject();
            JArray interestingWordsFromFile = new JArray();

            string fileName = "";
            string dirPath = "";


            if (inPath.Length > 1)
            {
                fileName = Path.GetFileName(inPath);
                dirPath = Path.GetDirectoryName(inPath);
                fileExt = Path.GetExtension(inPath);
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

            // if it doesn't seem to have any path separators it's probably a single file on sysvol.
            if (!inPath.Contains('\\') && !inPath.Contains('/'))
            {
                return new JObject(new JProperty("No path separators, file in SYSVOL?", inPath));
            }
            // figure out if it's a file path or just a directory
            if (fileName == "")
            {
                isDirPath = true;
            }
            else
            {
                isFilePath = true;
            }

            if (isDirPath)
            {
                dirExists = FileSystem.DoesDirExist(inPath);
            }

            if (isFilePath)
            {
                fileExt = Path.GetExtension(inPath);
                // check if the file exists
                fileExists = FileSystem.DoesFileExist(inPath);
                
                if (fileExists)
                {
                    // if it does, the parent Dir must exist.
                    dirExists = true;
                    // check if we can read it
                    fileReadable = FileSystem.CanIRead(inPath);
                    // check if we can write it
                    fileWritable = FileSystem.CanIWrite(inPath);
                    // if we can read it, have a look if it has interesting strings in it.
                    if (fileReadable)
                    {
                        // make sure the file isn't massive so we don't waste ages grepping whole disk images over the network
                        long fileSize = new System.IO.FileInfo(inPath).Length;

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
                else
                {
                    dirExists = FileSystem.DoesDirExist(dirPath);
                    if (dirExists)
                    {
                        dirDacls = Utility.GetFileDaclJObject(dirPath);
                        string dirWriteTestPath = Path.Combine(dirPath, "testFileFromGrouper2Assessment.txt");
                        //TODO this is fucking gross and messy but I can't think of a better way of doing it. ideally I want to delete these if i create them but putting File.Delete anywhere in this gives me the willies.
                        dirWritable = FileSystem.CanIWrite(dirWriteTestPath);
                    }
                }
            }
            // if the dir doesn't exist, iterate up the file path checking if any exist and if we can write to any of them.
            if (!dirExists)
            {
                if (dirPath != null)
                {
                    // get the root of the path
                    try
                    {
                        string pathRoot = Path.GetPathRoot(dirPath);
                    }
                    catch (ArgumentException e)
                    {
                        if (GlobalVar.DebugMode)
                        {
                            Utility.DebugWrite(e.ToString());
                        }
                        return new JObject(new JProperty("Not a path?", inPath));
                    }

                    // get the first parent dir
                    string dirPathParent = "";

                    try
                    {
                        if (FileSystem.GetParentDirPath(dirPath) != null)
                        {
                            dirPathParent = FileSystem.GetParentDirPath(dirPath);
                        }
                    }
                    catch (ArgumentException e)
                    {
                        if (GlobalVar.DebugMode)
                        {
                            Utility.DebugWrite(e.ToString());
                        }
                        return new JObject(new JProperty("Not a path?", inPath));
                    }

                    // iterate until the path root 
                    while (dirPathParent != null)
                    {
                        // check if the parent dir exists
                        parentDirExists = FileSystem.DoesDirExist(dirPathParent);
                        // if it does
                        if (parentDirExists)
                        {
                            // get the dir dacls
                            parentDirDacls = Utility.GetFileDaclJObject(dirPathParent);
                            // set up a path for us to try and write to
                            string parentDirWriteTestPath =
                                Path.Combine(dirPathParent, "testFileFromGrouper2Assessment.txt");
                            // this is fucking gross and messy but I can't think of a better way of doing it. ideally I want to delete these if i create them but putting File.Delete anywhere in this gives me the willies.
                            // try to write to it
                            parentDirWritable = FileSystem.CanIWrite(parentDirWriteTestPath);
                            if (parentDirWritable)
                            {
                                writableParentDir = dirPathParent;
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            //prepare for next iteration by aiming at the parent dir
                            if (FileSystem.GetParentDirPath(dirPathParent) != null)
                            {
                                dirPathParent = FileSystem.GetParentDirPath(dirPathParent);
                            }
                            else break;
                        }
                    }
                }
            }
            
            // put all the values we just collected into a jobject for reporting and calculate how interesting it is.
            JObject filePathAssessment = new JObject();
            int interestLevel = 0;
            filePathAssessment.Add("Path assessed", inPath);
            if (isFilePath)
            {
                if (fileExists)
                {
                    filePathAssessment.Add("File exists", true);
                    // TODO check if file extensions are interesting.
                    //filePathAssessment.Add("File extension interesting", extIsInteresting);
                    filePathAssessment.Add("File readable", fileReadable);
                    if (fileContentsInteresting)
                    {
                        filePathAssessment.Add("File contents interesting", "True");
                        filePathAssessment.Add("Interesting strings found", interestingWordsFromFile );
                        interestLevel = interestLevel + 2;
                    }
                    filePathAssessment.Add("File writable", fileWritable);
                    if (fileWritable) interestLevel = interestLevel + 10;
                    // TODO see if I can do anything more with these.
                    filePathAssessment.Add("File DACLs", fileDacls);
                }
                else
                {
                    filePathAssessment.Add("File exists", false);
                    filePathAssessment.Add("Directory exists", dirExists);
                    if (dirExists)
                    {
                        filePathAssessment.Add("Directory writable", dirWritable);
                        if (dirWritable) interestLevel = interestLevel + 10;
                        filePathAssessment.Add("Directory DACL", dirDacls);
                    }
                    else if (parentDirExists)
                    {
                        filePathAssessment.Add("Parent dir exists", true);
                        if (parentDirWritable)
                        {
                            filePathAssessment.Add("Parent dir writable", "True");
                            interestLevel = interestLevel + 10;
                            filePathAssessment.Add("Writable parent dir", writableParentDir);
                        }
                        else
                        {
                            filePathAssessment.Add("Extant parent dir", extantParentDir);
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
                    if (dirWritable) interestLevel = interestLevel + 10;
                    filePathAssessment.Add("Directory DACLs", dirDacls);
                }
                else if (parentDirExists)
                {
                    filePathAssessment.Add("Parent dir exists", true);
                    if (parentDirWritable)
                    {
                        filePathAssessment.Add("Parent dir writable", "True");
                        interestLevel = interestLevel + 10;
                        filePathAssessment.Add("Writable parent dir", writableParentDir);
                    }
                    else
                    {
                        filePathAssessment.Add("Extant parent dir", extantParentDir);
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
            catch (System.ArgumentException)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Checked if file " + inPath +
                                       " exists but it doesn't seem to be a valid file path.");
                }
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
            catch (System.ArgumentException)
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
            catch (System.UnauthorizedAccessException)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Tested read perms for " + inPath + " and couldn't read.");
                }
            }
            catch (System.ArgumentException)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Tested read perms for " + inPath + " but it doesn't seem to be a valid file path.");
                }
            }
            catch (Exception e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite(e.ToString());
                }
            }
            return canRead;
        }

        public static bool CanIWrite(string inPath)
        {
            bool canWrite = false;
            try
            {
                if (GlobalVar.NoMess || !GlobalVar.OnlineChecks)
                {
                    return false;
                }
                else
                {
                    GlobalVar.CleanupList.Add(inPath);
                    FileStream stream = File.OpenWrite(inPath);
                    canWrite = stream.CanWrite;
                    stream.Close();
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Tested write perms for " + inPath + " and couldn't write.");
                }
            }
            catch (System.ArgumentException)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Tested write perms for " + inPath +
                                       " but it doesn't seem to be a valid file path.");
                }
            }
            catch (Exception e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite(e.ToString());
                }
            }
            return canWrite;
        }

        public static JObject InvestigateFileContents(string inString)
        {
            string fileString = File.ReadAllText(inString).ToLower();
            
            // feed the whole thing through Utility.InvestigateString
            JObject investigatedFileContents = Utility.InvestigateString(fileString);
            
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