using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static List<string> DedupeList(List<string> listStrings)
        {
            List<string> result = new List<string>();
            foreach (var t in listStrings)
            {
                if (!result.Contains(t))
                    result.Add(t);
            }
            return result;
        }

        public static bool IsEmptyOrWhiteSpace(string inString)
        {
           return inString.All(char.IsWhiteSpace);
        }

        public static JArray GetInterestingWordsFromFile(string inPath)
        {
            // validate if the file exists
            bool fileExists = FileSystem.DoesFileExist(inPath);
            if (!fileExists)
            {
                return null;
            }

            // get our list of interesting words
            JArray interestingWords = (JArray) JankyDb.Instance["interestingWords"];
            
            // get contents of the file and smash case
            string fileContents = "";
            try
            {
                fileContents = File.ReadAllText(inPath).ToLower();
            }
            catch (UnauthorizedAccessException e)
            {
                if (GlobalVar.DebugMode)
                {
                    Utility.DebugWrite(e.ToString());
                }
            }

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

        public static int IndexOfNth(string str, string value, int nth = 1)
        // from https://stackoverflow.com/questions/22669044/how-to-get-the-index-of-second-comma-in-a-string
        {
            if (nth <= 0)
                throw new ArgumentException("Can not find the zeroth index of substring in string. Must start with 1");
            int offset = str.IndexOf(value);
            for (int i = 1; i < nth; i++)
            {
                if (offset == -1) return -1;
                offset = str.IndexOf(value, offset + 1);
            }
            return offset;
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
                    isValid = string.IsNullOrEmpty(root.Trim('\\', '/')) == false;
                }
            }
            catch (Exception)
            {
                isValid = false;
            }

            return isValid;
        }

        public static List<string> FindFilePathsInString(string inString)
        {
            List<string> foundFilePaths = new List<string>();

            string[] stringBits = inString.Split(' ');

            foreach (string stringBit in stringBits)
            {
                string cleanedBit = stringBit.Trim('\'', '\"');
                if (IsValidPath(cleanedBit))
                {
                    foundFilePaths.Add(cleanedBit);
                }
            }

            return foundFilePaths;
        }

        public static JObject InvestigateString(string inString)
        // general purpose method for returning some information about why a string might be interesting.
        // TODO expand/finish this
        {
            int interestLevel = 0;
            JObject investigationResults = new JObject {{"Value", inString}};

            // make a list to put any interesting words we find in it
            JArray interestingWordsFound = new JArray();
            // refer to our master list of interesting words
            JArray interestingWords = (JArray) JankyDb.Instance["interestingWords"];
            foreach (string interestingWord in interestingWords)
            {
                if (inString.ToLower().Contains(interestingWord))
                {
                    interestingWordsFound.Add(interestingWord);
                    interestLevel = 4;
                }
            }

            List<string> foundFilePaths = FindFilePathsInString(inString);

            JArray investigatedPaths = new JArray();

            foreach (string foundFilePath in foundFilePaths)
            {
                JObject investigatedPath = FileSystem.InvestigatePath(foundFilePath);

                if (investigatedPath != null)
                {
                    if (investigatedPath["InterestLevel"] != null && int.Parse(investigatedPath["InterestLevel"].ToString()) >= GlobalVar.IntLevelToShow)
                    {
                        investigatedPaths.Add(investigatedPath);
                    }
                }
            }

            if (investigatedPaths.Count > 0)
            {
                investigationResults.Add("Paths", investigatedPaths);
            }

            if (interestingWordsFound.Count > 0)
            {
                investigationResults.Add("Interesting Words", interestingWordsFound);
            }

            investigationResults.Add("InterestLevel", interestLevel);
            return investigationResults;
            
        }

        public static JObject GetFileDaclJObject(string filePathString)
        {
            int inc = 0;

            string[] interestingTrustees = new string[] {"Everyone", "BUILTIN\\Users", "Authenticated Users", "Domain Users", "INTERACTIVE", };
            string[] boringTrustees = new string[] {"TrustedInstaller", "Administrators", "NT AUTHORITY\\SYSTEM", "Domain Admins", "Enterprise Admins", "Domain Controllers"};
            string[] interestingRights = new string[] {"FullControl", "Modify", "Write", "AppendData", "TakeOwnership"};
            string[] boringRights = new string[] {"Synchronize", "ReadAndExecute" };

            if (!GlobalVar.OnlineChecks)
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
                DebugWrite("Tried to check file permissions on invalid path: " + filePathString);
                DebugWrite(e.ToString());
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                if (GlobalVar.DebugMode)
                {
                    DebugWrite(e.ToString());
                }
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
                string displayNameString = LDAPstuff.GetUserFromSid(sid);
                // do some interest level analysis
                bool trusteeBoring = false;
                bool trusteeInteresting = false;
                // check if our trustee is boring
                foreach (string boringTrustee in boringTrustees)
                {
                    // if we're showing everything that's fine, keep going
                    if (GlobalVar.IntLevelToShow == 0)
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
                if ((GlobalVar.IntLevelToShow != 0) && trusteeBoring)
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
                    if (GlobalVar.IntLevelToShow == 0)
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
                    else if (GlobalVar.IntLevelToShow < 3)
                    {
                        Utility.DebugWrite(right + " was not labelled as boring or interesting.");
                        fileSystemRightsJArray.Add(right);
                    }
                    else
                    {
                        Utility.DebugWrite("Shouldn't hit here, label FS right as boring or interesting." + right);
                    }
                }

                // no point continuing if no rights to show
                if (fileSystemRightsJArray.HasValues)
                {
                    // if the trustee isn't interesting and we're excluding low-level findings, bail out
                    if ((!trusteeInteresting) && (GlobalVar.IntLevelToShow > 4))
                    {
                        return null;
                    }
                    // build the object
                    JObject fileDaclJObject = new JObject
                    {
                        {accessControlTypeString, displayNameString},
                        {"Inherited?", isInheritedString},
                        {"Rights", fileSystemRightsJArray.ToString()}
                    };
                    // add the object to the array.
                    fileDaclsJObject.Add(inc.ToString(), fileDaclJObject);

                    inc++;
                }
            }
            //DebugWrite(fileDaclsJObject.ToString());
            return fileDaclsJObject;
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
            JArray wellKnownSids = (JArray)jsonData["trustees"];

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
                if (sidMatches)
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
            catch (NullReferenceException)
            {
                stringOut = "";
            }
            return stringOut;
        }

        public static void DebugWrite(string textToWrite)
        {
            if (GlobalVar.DebugMode)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n" + textToWrite + "\n");
                Console.ResetColor();
            }
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
            string barf = @"  .,-:::::/::::::..      ..     ...   ::::::::::::..,::::::::::::..  ,;'``;. 
,;;-'````' ;;;``;;;;  .;;;;;;.  ;;    ;;;`;;;```.;;;;;;'''';;;``;;;; ''  ,[[ 
[[[   [[[[[[[[,/[[[' ,[[    \[[[['    [[[ `]]nnn]]' [[cccc  [[,/[[['  .c$P'  
'$$c.    '$$$$$$$c   $$$,    $$$$     $$$  $$$''    $$''''  $$$$$c   d8MMMUP*
 `Y8bo,,,o8888b '88bo'888,__,8888   .d888  888o     888oo,__88b '88bo        
   `'YMUP'YMMMM   'W'  'YMMMMP' 'YmMMMM''  YMMMb    ''''YUMMMMM   'W'        
                                                    Now even Grouperer.      
                                                    github.com/l0ss/Grouper2 
                                                    @mikeloss                ";
            string[] barfLines = barf.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            ConsoleColor[] patternOne = { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.DarkRed, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
            ConsoleColor[] patternTwo =
            {
                ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.DarkBlue, ConsoleColor.White,
                ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White
            };
            int i = 0;
            foreach (string barfLine in barfLines)
            {
                string barfOne = barfLine.Substring(0, 69);
                string barfTwo = barfLine.Substring(69, 8);
                WriteColor(barfOne, patternOne[i]);
                WriteColorLine(barfTwo, patternTwo[i]);
                i += 1;
            }
        }

        public static string GetActionString(string actionChar)
            // shut up, i know it's not really a char.
        {
            string actionString;

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
                    actionString = "Replace";
                    break;
                default:
                    DebugWrite("oh no this is new");
                    DebugWrite(actionChar);
                    actionString = "Broken";
                    break;
            }

            return actionString;
        }
    }
}
