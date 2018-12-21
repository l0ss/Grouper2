using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class Utility
    {
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
            byte[] AesKey = {0x4e, 0x99, 0x06, 0xe8, 0xfc, 0xb6, 0x6c, 0xc9, 0xfa, 0xf4, 0x93, 0x10, 0x62, 0x0f, 0xfe, 0xe8,
                                 0xf4, 0x96, 0xe8, 0x06, 0xcc, 0x05, 0x79, 0x90, 0x20, 0x9b, 0x09, 0xa4, 0x33, 0xb6, 0x6c, 0x1b };
            aesProvider.IV = new byte[aesProvider.IV.Length];
            aesProvider.Key = AesKey;
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

        public static bool CanIWrite(string inPath)
        {
            bool canWrite = false;
            try
            {
                FileStream stream = File.OpenWrite(inPath);
                canWrite = stream.CanWrite;
                stream.Close();
            }
            catch (Exception e)
            {
                Utility.DebugWrite(e.ToString());
            }
            return canWrite;
        }

        public static void WriteColor(string textToWrite, ConsoleColor fgColor)
        {
            Console.ForegroundColor = fgColor;
            Console.WriteLine(textToWrite);
            Console.ResetColor();
        }

        public static void DebugWrite(string textToWrite)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(textToWrite);
            Console.ResetColor();
        }

        public static void PrintIndexAndValues(ArraySegment<String> arrSeg)
        {
            for (int i = arrSeg.Offset; i < (arrSeg.Offset + arrSeg.Count); i++)
            {
                Console.WriteLine("   [{0}] : {1}", i, arrSeg.Array[i]);
            }
            Console.WriteLine();
        }

        public static void PrintIndexAndValues(String[] myArr)
        {
            for (int i = 0; i < myArr.Length; i++)
            {
                Console.WriteLine("   [{0}] : {1}", i, myArr[i]);
            }
            Console.WriteLine();
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
                                                            @mikeloss
";
            string[] barfLines = barf.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            ConsoleColor[] pattern = { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.DarkRed, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
            int i = 0;
            foreach (string barfLine in barfLines)
            {
                WriteColor(barfLine, pattern[i]);
                i += 1;
            }
        }
    }
}