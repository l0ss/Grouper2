using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Grouper2.Auditor;
using Grouper2.Host.DcConnection;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Host.SysVol.Files
{
    /// <summary>
    /// Abstract class for a File, no defining what data if should hold.
    ///
    /// This class will also fill out the business logic that applies to all files from the IFile Interface
    /// </summary>
    public abstract class SysvolFile : DaclProvider, ISysvolFile
    {
        // at a minimum, a file should store its full path and some data based on that
        public string FilenameWithoutExt { get;  }
        public string FileExtension { get;  }
        public string ParentDir { get;  }

        protected string Contents()
        {
            try
            {
                string contents;
                FileStream fs = new FileStream(this.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader sr = new StreamReader(fs))
                {
                    contents = sr.ReadToEnd();
                }
                return contents;
            }
            catch (Exception e)
            {
                // TODO: actual erorr handling and propagation
                Log.Degub("TODO:", e, this);
                throw;
            }
        }

        /// <summary>
        /// Just basic instantiation
        /// </summary>
        /// <param name="path">Full file path to the file. Should decompose to parent directory, filename, file extension.</param>
        protected SysvolFile(string path)
        {
            // build from a file info
            FileInfo builder = new FileInfo(path);

            this.Path = builder.FullName;
            this.FilenameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(builder.Name);
            this.FileExtension = builder.Extension;
            this.ParentDir = builder.DirectoryName;

            // defensive check
            if (!builder.Exists || this.Path == null || this.FilenameWithoutExt == null || this.FileExtension == null || this.ParentDir == null)
            {
                throw new FileNotFoundException($"The path provided does not decompose into the expected file path components: {path}");
            }
        }

        ///////////////////////////
        /// Deal with interface stuff from IFile

        /// <summary>
        /// Get a list of Dacls associated with the file, filtered based on requested interest level
        /// </summary>
        /// <param name="ldap">The network interface responsible for doing network transactions</param>
        /// <param name="desiredInterestLevel">The interest level desired</param>
        /// <returns>A list of DACL objects associated with the items in the path of the current file</returns>
        public override Dictionary<string, Dacl> Dacls(Ldap ldap, int desiredInterestLevel)
        {
            // no need to do this twice?
            if (this._dacls != null) return this._dacls;

            // return early if we aren't networked
            if (!ldap.CanSendTraffic) return null;

            int inc = 0;

            string[] interestingTrustees =
                {"Everyone", "BUILTIN\\Users", "Authenticated Users", "Domain Users", "INTERACTIVE"};
            string[] boringTrustees =
            {
                "TrustedInstaller", "Administrators", "NT AUTHORITY\\SYSTEM", "Domain Admins", "Enterprise Admins",
                "Domain Controllers"
            };
            string[] interestingRights = { "FullControl", "Modify", "Write", "AppendData", "TakeOwnership" };
            string[] boringRights = { "Synchronize", "ReadAndExecute" };


            // object for result
            Dictionary<string, Dacl> fileDaclsJObject = new Dictionary<string, Dacl>();

            FileSecurity filePathSecObj;
            try
            {
                filePathSecObj = System.IO.File.GetAccessControl(Path);
            }
            catch (ArgumentException e)
            {
                Output.DebugWrite("Tried to check file permissions on invalid path: " + Path);
                Output.DebugWrite(e.ToString());
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                Output.DebugWrite(e.ToString());

                return null;
            }

            AuthorizationRuleCollection fileAccessRules =
                filePathSecObj.GetAccessRules(true, true, typeof(SecurityIdentifier));

            foreach (FileSystemAccessRule fileAccessRule in fileAccessRules)
            {
                // get inheritance and access control type values
                bool isInheritedString = fileAccessRule.IsInherited;
                string accessControlTypeString = "Allow";
                if (fileAccessRule.AccessControlType == AccessControlType.Deny) accessControlTypeString = "Deny";

                // get the user's SID
                string sid = fileAccessRule.IdentityReference.ToString();
                string displayNameString = ldap.GetUserFromSid(sid);

                // do some interest level analysis
                bool trusteeBoring = false;
                // check if our trustee is boring
                foreach (string boringTrustee in boringTrustees)
                {
                    // if we're showing everything that's fine, keep going
                    if (desiredInterestLevel == 0) break;

                    // otherwise if the trustee is boring, set the interest level to 0
                    if (displayNameString.ToLower().EndsWith(boringTrustee.ToLower()))
                    {
                        trusteeBoring = true;
                        // and don't bother comparing rest of array
                        break;
                    }
                }

                // skip rest of access rule if trustee is boring and we're not showing int level 0
                if (desiredInterestLevel != 0 && trusteeBoring) continue;

                // see if the trustee is interesting
                bool trusteeInteresting =
                    interestingTrustees.Any(i => displayNameString.ToLower().EndsWith(i.ToLower()));

                // get the rights array
                string[] fileSystemRightsArray =
                    fileAccessRule.FileSystemRights.ToString(). // get the rights
                        Replace(" ", ""). // remove all spaces
                        Split(','); // split on ,


                // then do some 'interest level' analysis
                // JArray for output
                JArray fileSystemRightsJArray = new JArray();
                foreach (string right in fileSystemRightsArray)
                {
                    bool rightInteresting = false;
                    bool rightBoring = false;

                    foreach (string boringRight in boringRights)
                        if (right.ToLower() == boringRight.ToLower())
                        {
                            rightBoring = true;
                            break;
                        }

                    foreach (string interestingRight in interestingRights)
                        if (right.ToLower() == interestingRight.ToLower())
                        {
                            rightInteresting = true;
                            break;
                        }

                    // if we're showing defaults, just add it to the result and move on
                    if (desiredInterestLevel == 0)
                    {
                        fileSystemRightsJArray.Add(right);
                        continue;
                    }

                    // if we aren't, and it's boring, skip it and move on.
                    if (rightBoring) continue;
                    // if it's interesting, add it and move on.
                    if (rightInteresting)
                    {
                        fileSystemRightsJArray.Add(right);
                    }
                    // if it's neither boring nor interesting, add it if the 'interestlevel to show' value is low enough
                    else if (desiredInterestLevel < 3)
                    {
                        Output.DebugWrite(right + " was not labelled as boring or interesting.");
                        fileSystemRightsJArray.Add(right);
                    }
                    else
                    {
                        Output.DebugWrite("Shouldn't hit here, label FS right as boring or interesting." + right);
                    }
                }

                // no point continuing if no rights to show
                if (!fileSystemRightsJArray.HasValues) continue;

                // if the trustee isn't interesting and we're excluding low-level findings, bail out
                if (!trusteeInteresting && desiredInterestLevel > 4) return null;
                // build the object
                string rightsString = fileSystemRightsJArray.ToString().Trim('[', ']').Trim().Replace("\"", "");

                fileDaclsJObject.Add(inc.ToString(), new Dacl(inc, accessControlTypeString, displayNameString, isInheritedString,
                    rightsString));

                inc++;
            }

            //DebugWrite(fileDaclsJObject.ToString());
            // before we return, let's just save the results in the event of another call
            this._dacls = fileDaclsJObject;
            // now return
            return fileDaclsJObject;
        }
    }
}