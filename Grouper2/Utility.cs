using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class Utility
    {
        public static string DecryptCpassword(string Cpassword)
        {
            // reimplemented based on @obscuresec's Get-GPPPassword PowerShell
            int CpassMod = Cpassword.Length % 4;
            string Padding = "";
            switch (CpassMod)
            {
                case 1:
                    Padding = "=";
                    break;
                case 2:
                    Padding = "==";
                    break;
                case 3:
                    Padding = "=";
                    break;
            }
            string CpasswordPadded = Cpassword + Padding;
            byte[] DecodedCpassword = System.Convert.FromBase64String(CpasswordPadded);
            AesCryptoServiceProvider AesProvider = new AesCryptoServiceProvider();
            byte[] AesKey = new byte[] {0x4e, 0x99, 0x06, 0xe8, 0xfc, 0xb6, 0x6c, 0xc9, 0xfa, 0xf4, 0x93, 0x10, 0x62, 0x0f, 0xfe, 0xe8,
                                 0xf4, 0x96, 0xe8, 0x06, 0xcc, 0x05, 0x79, 0x90, 0x20, 0x9b, 0x09, 0xa4, 0x33, 0xb6, 0x6c, 0x1b };
            AesProvider.IV = new byte[AesProvider.IV.Length];
            AesProvider.Key = AesKey;
            ICryptoTransform Decryptor = AesProvider.CreateDecryptor();
            byte[] DecryptedBytes = Decryptor.TransformFinalBlock(DecodedCpassword, 0, DecodedCpassword.Length);
            string DecryptedCpassword = Encoding.Unicode.GetString(DecryptedBytes);
            return DecryptedCpassword;
        }

        public static JToken CheckSID(string SID)
        {
            JObject JsonData = JankyDB.Instance;
            JArray WellKnownSIDS = (JArray)JsonData["trustees"]["item"];

            bool SIDmatches = false;
            // iterate over the list of well known sids to see if any match.
            foreach (JToken WellKnownSID in WellKnownSIDS)
            {
                string SIDToMatch = (string)WellKnownSID["SID"];
                // a bunch of well known sids all include the domain-unique sid, so we gotta check for matches amongst those.
                if ((SIDToMatch.Contains("DOMAIN")) && (SID.Length >= 14))
                {
                    string[] TrusteeSplit = SID.Split("-".ToCharArray());
                    string[] WKSIDSplit = SIDToMatch.Split("-".ToCharArray());
                    if (TrusteeSplit[TrusteeSplit.Length - 1] == WKSIDSplit[WKSIDSplit.Length - 1])
                    {
                        SIDmatches = true;
                    }
                }
                // check if we have a direct match
                if ((string)WellKnownSID["SID"] == SID)
                {
                    SIDmatches = true;
                }
                if (SIDmatches == true)
                {
                    JToken CheckedSID = WellKnownSID;
                    return CheckedSID;
                }
            }
            return null;
        }

        public static void WriteColor(string textToWrite, ConsoleColor fgColor)
        {
            Console.ForegroundColor = fgColor;
            Console.WriteLine(textToWrite);
            Console.ResetColor();
        }

        public static void DebugWrite(string TextToWrite)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(TextToWrite);
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


        static public void PrintBanner()
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
            string[] barflines = barf.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            System.ConsoleColor[] Pattern = { System.ConsoleColor.White, System.ConsoleColor.Yellow, System.ConsoleColor.Red, System.ConsoleColor.Red, System.ConsoleColor.DarkRed, System.ConsoleColor.DarkRed, System.ConsoleColor.White, System.ConsoleColor.White, System.ConsoleColor.White, System.ConsoleColor.White };
            int i = 0;
            foreach (string barfline in barflines)
            {
                WriteColor(barfline, Pattern[i]);
                i += 1;
            }
        }
    }
}