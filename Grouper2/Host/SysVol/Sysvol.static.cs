using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Grouper2.Host.SysVol
{
    // Expose some static methods used to interact with the SMB portions of sysvol to other members of grouper

    public partial class Sysvol
    {
        // singleton bullshit
        private static Sysvol _sysvol;
        private static object syncLock = new object();

        private static void InitMap()
        {
            if (_sysvol == null)
            {
                lock (syncLock)
                {
                    if (_sysvol == null)
                    {
                        _sysvol = new Sysvol(JankyDb.Vars.SysvolDir, JankyDb.Vars.NoNtfrs, JankyDb.Vars.NoGrepScripts);
                    }
                }
            }
        }

        public static Sysvol GetMap()
        {
            if (_sysvol == null)
            {
                InitMap();
            }

            return _sysvol;

        }

        public static List<string> GetImmediateChildFiles(string path)
        {
            if (File.Exists(path))
            {
                return Directory.EnumerateFiles(path).ToList();
            }
            throw new DirectoryNotFoundException();
        }

        public static List<string> GetImmediateChildDirs(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.EnumerateDirectories(path).ToList();
            }
            throw new DirectoryNotFoundException();
        }

        public static List<string> GetRecursiveChildFiles(string path)
        {
            if (File.Exists(path))
            {
                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToList();
            }
            throw new DirectoryNotFoundException();
        }

        public static List<string> GetRecursiveChildDirs(string path)
        {
            if (File.Exists(path))
            {
                return Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).ToList();
            }
            throw new DirectoryNotFoundException();
        }
    }
}
