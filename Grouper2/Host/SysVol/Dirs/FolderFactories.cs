using System.IO;
using System.Linq;
using Grouper2.Host.SysVol.Dirs;

namespace Grouper2.Host.SysVol
{
    public static class FolderFactories
    {
        private static readonly GenericFactory _generic = new GenericFactory();
        private static readonly PolicyFactory _policy = new PolicyFactory();
        private static readonly ScriptFactory _script = new ScriptFactory();
        private static readonly GpoFactory _gpo = new GpoFactory();
        private static readonly UserFactory _user = new UserFactory();
        private static readonly MachineFactory _machine = new MachineFactory();

        public static SysvolDirectory Manufacture(string path, string sysvoldir)
        {
            // create a directory info object and ensure it exists
            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                return null;
            }

            // if it is descended from sysvol\fq.dn directly
            if (di.Parent != null && di.Parent.FullName.Equals(sysvoldir))
            {
                // build out policies dir
                if (string.Equals(di.Name.ToLower(), "policies"))
                    return _policy.GetDirectory(path);
                // build out scripts dir
                if (string.Equals(di.Name.ToLower(), "scripts"))
                    return _script.GetDirectory(path);
                // explicitly ignore everything else
                return null;
            }

            // if it looks like it might be a gpo
            if (di.Name.Contains('{'))
            {
                // ensure it is in a policies directory
                if (di.Parent != null && string.Equals(di.Parent.Name.ToLower(), "policies"))
                {
                    // rough validation of the guid
                    if (shittyValidateGuid(di.Name.Trim('{', '}')))
                        return _gpo.GetDirectory(path);
                }
            }

            // or if it looks like it might be part of a gpo
            else if (di.Parent != null && di.Parent.Name.Contains("{"))
            {
                // and its parent is a GPO
                if (shittyValidateGuid(di.Parent.Name.Trim('{', '}')))
                {
                    // and it is a machine folder
                    if (string.Equals(di.Name.ToLower(), "user"))
                    {
                        return _user.GetDirectory(path);
                    }

                    // or a machine folder
                    if (string.Equals(di.Name.ToLower(), "machine"))
                    {
                        return _machine.GetDirectory(path);
                    }
                }
            }

            // if it seems like a script directory, but it wasn't the root one
            if (!JankyDb.Vars.NoNtfrs && di.Name.ToLower().Equals("scripts"))
            {
                _script.GetDirectory(path);
            }

            // if we got here, it must be a generic folder?
            return _generic.GetDirectory(path);
        }

        private static bool shittyValidateGuid(string test)
        {
            string[] parts = test.Split('-');
            if (parts.Length != 5)
            {
                return false;
            }
            if (parts[0].Length != 8)
            {
                return false;
            }
            if (parts[4].Length != 12)
            {
                return false;
            }
            if (parts[1].Length != 4 || parts[2].Length != 4 || parts[3].Length != 4)
            {
                return false;
            }

            // it must be a gpo then..... right???
            return true;
        }

        public static SysvolDirectory MakeRootSysvolDirectory(string path)
        {
            return new SysvolRoot(path);
        }
    }

    

    internal abstract class DirectoryFactory
    {
        public abstract SysvolDirectory GetDirectory(string location);
    }

    internal partial class GenericFactory : DirectoryFactory
    {
        public override SysvolDirectory GetDirectory(string location)
        {
            return new GenericDirectory(location){MajorType = SysvolMajorType.Dir, Type = SysvolObjectType.UselessFluffDirectory};
        }
    }

    internal partial class PolicyFactory : DirectoryFactory
    {
        public override SysvolDirectory GetDirectory(string location)
        {
            return new PolicyDirectory(location){MajorType = SysvolMajorType.Dir, Type = SysvolObjectType.PolicyFolder};
        }
    }

    internal partial class ScriptFactory : DirectoryFactory
    {
        public override SysvolDirectory GetDirectory(string location)
        {
            return new ScriptDirectory(location){MajorType = SysvolMajorType.Dir, Type = SysvolObjectType.ScriptDirectory};
        }
    }

    internal partial class GpoFactory : DirectoryFactory
    {
        public override SysvolDirectory GetDirectory(string location)
        {
            return new Gpo(location){MajorType = SysvolMajorType.Dir, Type = SysvolObjectType.GpoDirectory};
        }
    }

    internal partial class UserFactory : DirectoryFactory
    {
        public override SysvolDirectory GetDirectory(string location)
        {
            return new UserDirectory(location){MajorType = SysvolMajorType.Dir, Type = SysvolObjectType.UserDirectory};
        }
    }

    internal partial class MachineFactory : DirectoryFactory
    {
        public override SysvolDirectory GetDirectory(string location)
        {
            return new MachineDirectory(location){MajorType = SysvolMajorType.Dir, Type = SysvolObjectType.MachineDirectory};
        }
    }
}