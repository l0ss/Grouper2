using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class Utility
    {
        public static bool IsEmptyOrWhiteSpace(string inString)
        {
           return inString.All(char.IsWhiteSpace);
        }

        public static JArray GetInterestingWordsFromFile(string inPath)
        {
            // validate if the file exists
            bool fileExists = Utility.DoesFileExist(inPath);
            if (!fileExists)
            {
                return null;
            }

            // get our list of interesting words
            JArray interestingWords = (JArray) JankyDb.Instance["interestingWords"];
            
            // get contents of the file
            string fileContents = File.ReadAllText(inPath);

            // set up output object
            JArray interestingWordsFound = new JArray();

            foreach (string word in interestingWords)
            {
                if (fileContents.Contains(word))
                {
                    interestingWordsFound.Add(word);
                }
            }

            return interestingWordsFound;
        }

        public static JObject InvestigatePath(string inPath)
        {
            // general purpose method for returning some information about why a path might be interesting.
            // if it's a file, does it exist?
            // if it exists, is it modifiable?
            // if it exists, is it readable?
            // if it's readable, does it contain anything interesting?
            // if it doesn't exist, is the parent dir writable?
            
            // set up all our bools and empty JObjects so everything is boring until proven interesting.
            JArray interestingFileExts = (JArray) JankyDb.Instance["interestingExtensions"];
            bool fileExists = false;
            bool fileWritable = false;
            bool fileReadable = false;
            bool dirExists = false;
            bool dirWritable = false;
            bool fileContentsInteresting = false;
            bool fileTooBig = false;
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
                return new JObject(new JProperty("I don't think there's a path here", inPath));
            }

            if (fileName.Contains("http://"))
            {
                return new JObject(new JProperty("I think this is a URL not a file path", inPath));
            }

            if (fileName.Contains('%'))
            {
                return new JObject(new JProperty("I think this path contains an environment variable so I can't assess it properly", inPath));
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
                dirExists = Utility.DoesDirExist(inPath);
            }

            if (isFilePath)
            {
                fileExt = Path.GetExtension(inPath);
                // check if the file exists
                fileExists = Utility.DoesFileExist(inPath);
                
                if (fileExists)
                {
                    // if it does, the parent Dir must exist.
                    dirExists = true;
                    // check if we can read it
                    fileReadable = Utility.CanIRead(inPath);
                    // check if we can write it
                    fileWritable = Utility.CanIWrite(inPath);
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
                    dirExists = Utility.DoesDirExist(dirPath);
                    if (dirExists)
                    {
                        dirDacls = Utility.GetFileDaclJObject(dirPath);
                        string dirWriteTestPath = Path.Combine(dirPath, "testFileFromGrouper2Assessment.txt");
                        //TODO this is fucking gross and messy but I can't think of a better way of doing it. ideally I want to delete these if i create them but putting File.Delete anywhere in this gives me the willies.
                        dirWritable = Utility.CanIWrite(dirWriteTestPath);
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
                        return new JObject(new JProperty("This doesn't seem to be a file path", dirPath));
                    }

                    // get the first parent dir
                    string parentDirTestPath = "";

                    try
                    {
                        if (Directory.GetParent(dirPath) != null)
                        {
                            parentDirTestPath = Directory.GetParent(dirPath).FullName;
                        }
                    }
                    catch (ArgumentException e)
                    {
                        if (GlobalVar.DebugMode)
                        {
                            Utility.DebugWrite(e.ToString());
                        }
                        return new JObject(new JProperty("I don't think this is a file path", inPath));
                    }

                    // iterate until the path root 
                    while (parentDirTestPath.Length > 1)
                    {
                        // check if the parent dir exists
                        parentDirExists = Utility.DoesDirExist(parentDirTestPath);
                        // if it does
                        if (parentDirExists)
                        {
                            // get the dir dacls
                            parentDirDacls = Utility.GetFileDaclJObject(parentDirTestPath);
                            // set up a path for us to try and write to
                            string parentDirWriteTestPath =
                                Path.Combine(parentDirTestPath, "testFileFromGrouper2Assessment.txt");
                            // this is fucking gross and messy but I can't think of a better way of doing it. ideally I want to delete these if i create them but putting File.Delete anywhere in this gives me the willies.
                            // try to write to it
                            parentDirWritable = Utility.CanIWrite(parentDirWriteTestPath);
                            if (parentDirWritable)
                            {
                                writableParentDir = parentDirWriteTestPath;
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }

                        //prepare for next iteration by aiming at the parent dir
                        if (Directory.GetParent(dirPath) != null)
                        {
                            parentDirTestPath = Directory.GetParent(dirPath).FullName;
                        }
                        else break;
                    }
                }
            }
            
            // put all the values we just collected into a jobject for reporting.
            JObject filePathAssessment = new JObject();
            filePathAssessment.Add("Path assessed", inPath);
            if (isFilePath)
            {
                if (fileExists)
                {
                    filePathAssessment.Add("File exists", true);
                    // TODO check if file extensions are interesting.
                    filePathAssessment.Add("File extension is interesting", extIsInteresting);
                    filePathAssessment.Add("File is readable", fileReadable);
                    if (fileContentsInteresting)
                    {
                        //TODO check for interesting strings in files
                        filePathAssessment.Add("File contains interesting strings", fileContentsInteresting);
                        filePathAssessment.Add("Interesting strings found", interestingWordsFromFile );
                    }
                    filePathAssessment.Add("File is writable", fileWritable);
                    filePathAssessment.Add("File DACL", fileDacls);
                }
                else
                {
                    filePathAssessment.Add("File exists", false);
                    filePathAssessment.Add("Directory exists", dirExists);
                    if (dirExists)
                    {
                        filePathAssessment.Add("Directory is writable", dirWritable);
                        filePathAssessment.Add("Directory DACL", dirDacls);
                    }
                    else if (parentDirExists)
                    {
                        filePathAssessment.Add("Parent dir exists", true);
                        if (parentDirWritable)
                        {
                            filePathAssessment.Add("Parent dir writable", parentDirWritable);
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
                    filePathAssessment.Add("Directory DACL", dirDacls);
                }
                else if (parentDirExists)
                {
                    filePathAssessment.Add("Parent dir exists", true);
                    if (parentDirWritable)
                    {
                        filePathAssessment.Add("Parent dir writable", parentDirWritable);
                        filePathAssessment.Add("Writable parent dir", writableParentDir);
                    }
                    else
                    {
                        filePathAssessment.Add("Extant parent dir", extantParentDir);
                    }
                }
            }
            return filePathAssessment;
        }


        public static bool DoesFileExist(string inPath)
        {
            bool fileExists = false;
            try
            {
                fileExists = File.Exists(inPath);
            }
            catch (System.ArgumentException e)
            {
                Utility.DebugWrite("Checked if file " + inPath + " exists but it doesn't seem to be a valid file path.");
            }
            return fileExists;
        }

        public static bool DoesDirExist(string inPath)
        {
            bool dirExists = false;
            try
            {
                dirExists = Directory.Exists(inPath);
            }
            catch (System.ArgumentException e)
            {
                Utility.DebugWrite("Checked if directory " + inPath + " exists but it doesn't seem to be a valid file path.");
            }
            return dirExists;
        }

        public static bool CanIRead(string inPath)
        {
            bool canRead = false;
            try
            {
                FileStream stream = File.OpenRead(inPath);
                canRead = stream.CanRead;
                stream.Close();
            }
            catch (System.UnauthorizedAccessException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Tested read perms for " + inPath + " and couldn't read.");
                }
            }
            catch (System.ArgumentException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Tested read perms for " + inPath +
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
            return canRead;
        }

        public static bool CanIWrite(string inPath)
        {
            bool canWrite = false;
            try
            {
                FileStream stream = File.OpenWrite(inPath);
                canWrite = stream.CanWrite;
                stream.Close();
            }
            catch (System.UnauthorizedAccessException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite("Tested write perms for " + inPath + " and couldn't write.");
                }
            }
            catch (System.ArgumentException e)
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

        public static JObject InvestigateString(string inString)
        // general purpose method for returning some information about why a string might be interesting.
        // TODO finish this
        {

            JObject investigationResults = new JObject();

            investigationResults.Add("Value", inString);
            // make a list to put any interesting words we find in it
            JArray interestingWordsFound = new JArray();
            // refer to our master list of interesting words
            JArray interestingWords = (JArray) JankyDb.Instance["interestingWords"];
            foreach (string interestingWord in interestingWords)
            {
                if (inString.ToLower().Contains(interestingWord))
                {
                    interestingWordsFound.Add(interestingWord);
                }
            }

            if (interestingWordsFound.Count > 0)
            {
                investigationResults.Add("String contained interesting key words", interestingWordsFound);
            }

            // TODO for each of these I need to separate out the interesting part of the string from the rest of it.

            if (inString.ToLower().Contains("\\\\"))
            {
                investigationResults.Add("Possible UNC path", inString);
                //Utility.DebugWrite("Think I found a UNC path: " + inString);
                //TODO do something here to investigate the path.
            }

            if (inString.ToLower().Contains(":\\"))
            {
                investigationResults.Add("Possible file path", inString);
                //Utility.DebugWrite("Maybe this is a path with a drive letter?");
            }

            if (inString.ToLower().Contains("http"))
            {
                investigationResults.Add("Possible URL", inString);
            }

            
            
            return investigationResults;
        }

        public static JObject GetFileDaclJObject(string filePathString)
        {
            JObject fileDaclsJObject = new JObject();
            FileSecurity filePathSecObj = new FileSecurity();
            try
            {
                filePathSecObj = File.GetAccessControl(filePathString);
            }
            catch (System.ArgumentException e)
            {
                Console.WriteLine("Tried to check file permissions on invalid path: " + filePathString.ToString());
                return fileDaclsJObject;
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
                string identityReferenceString = fileAccessRule.IdentityReference.ToString();
                string displayNameString = LDAPstuff.GetUserFromSid(identityReferenceString);
                // get the rights
                string fileSystemRightsString = fileAccessRule.FileSystemRights.ToString();
                // strip spaces
                fileSystemRightsString = fileSystemRightsString.Replace(" ", "");
                // turn them into an array
                string[] fileSystemRightsArray = fileSystemRightsString.Split(',');
                // then into a JArray
                JArray fileSystemRightsJArray = new JArray();
                foreach (string x in fileSystemRightsArray)
                {
                    fileSystemRightsJArray.Add(x);
                }

                JObject fileDaclJObject = new JObject();
                fileDaclJObject.Add("Display Name", displayNameString);
                fileDaclJObject.Add("Allow or Deny?", accessControlTypeString);
                fileDaclJObject.Add("Inherited?", isInheritedString);
                fileDaclJObject.Add("Rights", fileSystemRightsJArray);
                fileDaclsJObject.Add(identityReferenceString, fileDaclJObject);
            }

            return fileDaclsJObject;
        }

        public static string SidPrivHighOrLow(string sid)
        // checks if a Sid belongs to a user who is canonically 'high' or 'low' priv.
        // by canonically, I mean 'I Reckon'.
        {
            string highOrLow = null;
            JToken checkedSid = Utility.CheckSid(sid);
            if (checkedSid != null)
            {
                string highPriv = checkedSid["highPriv"].ToString();
                string lowPriv = checkedSid["lowPriv"].ToString();
                if (highPriv == "True")
                {
                    highOrLow = "High";
                }

                if (lowPriv == "True")
                {
                    highOrLow = "Low";
                }
            }
            
            return highOrLow;
        }

    public static string DecryptCpassword(string cpassword)
        {
            // reimplemented based on @obscuresec's Get-GPPPassword PowerShell
            int cpassMod = cpassword.Length % 4;
            string padding = "";
            switch (cpassMod)
            {
                case 1:
                    padding = "=";
                    break;
                case 2:
                    padding = "==";
                    break;
                case 3:
                    padding = "=";
                    break;
            }
            string cpasswordPadded = cpassword + padding;
            byte[] decodedCpassword = Convert.FromBase64String(cpasswordPadded);
            AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider();
            byte[] aesKey = {0x4e, 0x99, 0x06, 0xe8, 0xfc, 0xb6, 0x6c, 0xc9, 0xfa, 0xf4, 0x93, 0x10, 0x62, 0x0f, 0xfe, 0xe8,
                                 0xf4, 0x96, 0xe8, 0x06, 0xcc, 0x05, 0x79, 0x90, 0x20, 0x9b, 0x09, 0xa4, 0x33, 0xb6, 0x6c, 0x1b };
            aesProvider.IV = new byte[aesProvider.IV.Length];
            aesProvider.Key = aesKey;
            ICryptoTransform decryptor = aesProvider.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(decodedCpassword, 0, decodedCpassword.Length);
            string decryptedCpassword = Encoding.Unicode.GetString(decryptedBytes);
            return decryptedCpassword;
        }

        public static JToken CheckSid(string sid)
        {
            JObject jsonData = JankyDb.Instance;
            JArray wellKnownSids = (JArray)jsonData["trustees"]["item"];

            bool sidMatches = false;
            // iterate over the list of well known sids to see if any match.
            foreach (JToken wellKnownSid in wellKnownSids)
            {
                string sidToMatch = (string)wellKnownSid["SID"];
                // a bunch of well known sids all include the domain-unique sid, so we gotta check for matches amongst those.
                if ((sidToMatch.Contains("DOMAIN")) && (sid.Length >= 14))
                {
                    string[] trusteeSplit = sid.Split("-".ToCharArray());
                    string[] wkSidSplit = sidToMatch.Split("-".ToCharArray());
                    if (trusteeSplit[trusteeSplit.Length - 1] == wkSidSplit[wkSidSplit.Length - 1])
                    {
                        sidMatches = true;
                    }
                }
                // check if we have a direct match
                if ((string)wellKnownSid["SID"] == sid)
                {
                    sidMatches = true;
                }
                if (sidMatches == true)
                {
                    JToken checkedSid = wellKnownSid;
                    return checkedSid;
                }
            }
            return null;
        }


        public static string GetSafeString(JToken json, string inString)
        {
            string stringOut;
            try
            {
                stringOut = json[inString].ToString();
            }
            catch (System.NullReferenceException)
            {
                stringOut = "";
            }
            return stringOut;
        }

        public static void DebugWrite(string textToWrite)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(textToWrite);
            Console.ResetColor();
        }

        public static void WriteColor(string textToWrite, ConsoleColor fgColor)
        {
            Console.ForegroundColor = fgColor;
            Console.Write(textToWrite);
            Console.ResetColor();
        }

        public static void WriteColorLine(string textToWrite, ConsoleColor fgColor)
        {
            Console.ForegroundColor = fgColor;
            Console.WriteLine(textToWrite);
            Console.ResetColor();
        }

        public static void PrintBanner()
        {
            string barf = @"  .,-:::::/  :::::::..       ...      ...    :::::::::::::. .,::::::  :::::::..     .:::.   
,;;-'````'   ;;;;``;;;;   .;;;;;;;.   ;;     ;;; `;;;```.;;;;;;;''''  ;;;;``;;;;   ,;'``;.  
[[[   [[[[[[/ [[[,/[[['  ,[[     \[[,[['     [[[  `]]nnn]]'  [[cccc    [[[,/[[['   ''  ,[[' 
*$$c.    *$$  $$$$$$c    $$$,     $$$$$      $$$   $$$**     $$****    $$$$$$c     .c$$P'   
 `Y8bo,,,o88o 888b *88bo,*888,_ _,88P88    .d888   888o      888oo,__  888b *88bo,d88 _,oo, 
   `'YMUP*YMM MMMM   *W*   *YMMMMMP*  *YmmMMMM**   YMMMb     ****YUMMM MMMM   *W* MMMUP**^^ 
                                                            Now even Grouperer.              
                                                            github.com/mikeloss/Grouper2    
                                                            @mikeloss                          ";
            string[] barfLines = barf.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            ConsoleColor[] patternOne = { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.DarkRed, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
            ConsoleColor[] patternTwo =
            {
                ConsoleColor.White, ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.DarkBlue,
                ConsoleColor.DarkBlue, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White,
            };
            int i = 0;
            foreach (string barfLine in barfLines)
            {
                string barfOne = barfLine.Substring(0, 82);
                string barfTwo = barfLine.Substring(82, 9);
                WriteColor(barfOne, patternOne[i]);
                WriteColorLine(barfTwo, patternTwo[i]);
                i += 1;
            }
        }

        public static string GetActionString(string actionChar)
            // shut up, i know it's not really a char.
        {
            string actionString = "";

            switch (actionChar)
            {
                case "U":
                    actionString = "Update";
                    break;
                case "A":
                    actionString = "Add";
                    break;
                case "D":
                    actionString = "Delete";
                    break;
                case "C":
                    actionString = "Create";
                    break;
                case "R":
                    actionString = "Remove";
                    break;
                default:
                    Utility.DebugWrite("oh no this is new");
                    Utility.DebugWrite(actionChar);
                    actionString = "Broken";
                    break;
            }

            return actionString;
        }
    }
}