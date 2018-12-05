using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Grouper2
{
    class Utility
    {
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

        static void DirSearch(string sDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        Console.WriteLine(f);
                    }
                    DirSearch(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
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