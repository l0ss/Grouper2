using System;
using System.ComponentModel;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Grouper2.Host
{
    public class CurrentUser
    {
        // threading bullshit
        private static  CurrentUser _user;
        private static object syncLock = new object();
        public static CurrentUser Query
        {
            get
            {
                if (_user == null)
                {
                    lock (syncLock)
                    {
                        if (_user == null)
                        {
                            _user = new CurrentUser();
                        }
                    }
                }

                return _user;
            }
        }
        

        private readonly bool _authorisedToRead;
        public readonly WindowsIdentity Identity;
        public readonly string CurrentSid;
        public readonly WindowsPrincipal Principal;

        protected CurrentUser()
        {
            this._authorisedToRead = JankyDb.Vars.OnlineMode;
            if (_authorisedToRead)
            {
                this.Identity = WindowsIdentity.GetCurrent();
                if (Identity.User != null) this.CurrentSid = Identity.User.ToString();
                this.Principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            }
            else
            {
                this.Identity = null;
                this.Principal = null;
                this.CurrentSid = null;
            }
            
        }

        public bool CanReadFrom(string inPath)
        {
            if (!_authorisedToRead) return false;

            try
            {
                File.Open(inPath, FileMode.Open, FileAccess.Read).Dispose();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Utility.Output.DebugWrite("Tested read perms for " + inPath + " and couldn't read.");
            }
            catch (ArgumentException)
            {
                Utility.Output.DebugWrite("Tested read perms for " + inPath + " but it doesn't seem to be a valid file path.");
            }
            catch (Exception e)
            {
                Utility.Output.DebugWrite(e.ToString());
            }
            return false;
        }

        public bool CanWriteTo(string inPath)
        {
            // this will return true if write or modify or take ownership or any of those other good perms are available.

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
                        return HasAccess(dirInfo, fsRight);
                    }

                    FileInfo fileInfo = new FileInfo(inPath);
                    return HasAccess(fileInfo, fsRight);
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

        public bool HasAccess(DirectoryInfo directory, FileSystemRights right)
        {
            try
            {
                // Get the collection of authorization rules that apply to the directory.
                AuthorizationRuleCollection acl = directory.GetAccessControl()
                    .GetAccessRules(true, true, typeof(SecurityIdentifier));
                return HasFileOrDirectoryAccess(right, acl);
            }

            catch (UnauthorizedAccessException e)
            {
                Utility.Output.DebugWrite(e.ToString());
            }

            return false;
        }

        public bool HasAccess(FileInfo file, FileSystemRights right)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (!Enum.IsDefined(typeof(FileSystemRights), right))
                throw new InvalidEnumArgumentException(nameof(right), (int) right, typeof(FileSystemRights));

            try
            {
                // Get the collection of authorization rules that apply to the file.
                AuthorizationRuleCollection acl = file.GetAccessControl()
                    .GetAccessRules(true, true, typeof(SecurityIdentifier));

                return HasFileOrDirectoryAccess(right, acl);
            }

            catch (UnauthorizedAccessException e)
            {
                Utility.Output.DebugWrite(e.ToString());
            }

            return false;
        }

        private bool HasFileOrDirectoryAccess(FileSystemRights right,
                                              AuthorizationRuleCollection acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            if (!Enum.IsDefined(typeof(FileSystemRights), right))
                throw new InvalidEnumArgumentException(nameof(right), (int) right, typeof(FileSystemRights));

            bool allow = false;
            bool inheritedAllow = false;
            bool inheritedDeny = false;

            for (int i = 0; i < acl.Count; i++)
            {
                FileSystemAccessRule currentRule = (FileSystemAccessRule)acl[i];
                // If the current rule applies to the current user.
                if (currentRule != null && Identity.User != null && (Identity.User.Equals(currentRule.IdentityReference) ||
                                                                     Principal.IsInRole(
                                                                         (SecurityIdentifier)currentRule.IdentityReference)))
                {

                    if (currentRule.AccessControlType.Equals(AccessControlType.Deny))
                    {
                        if ((currentRule.FileSystemRights & right) == right)
                        {
                            if (currentRule.IsInherited)
                            {
                                inheritedDeny = true;
                            }
                            else
                            { // Non inherited "deny" takes overall precedence.
                                return false;
                            }
                        }
                    }
                    else if (currentRule.AccessControlType
                                                    .Equals(AccessControlType.Allow))
                    {
                        if ((currentRule.FileSystemRights & right) == right)
                        {
                            if (currentRule.IsInherited)
                            {
                                inheritedAllow = true;
                            }
                            else
                            {
                                allow = true;
                            }
                        }
                    }
                }
            }

            if (allow)
            { // Non inherited "allow" takes precedence over inherited rules.
                return true;
            }
            return inheritedAllow && !inheritedDeny;
        }
    }
}
