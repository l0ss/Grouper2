using System;
using System.Runtime.CompilerServices;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Finding Audit(Drives file)
        {
            if (file == null) 
                throw new ArgumentNullException(nameof(file));
            return GetAssessedDrives(file.JankyXmlStuff);
        }

        // ReSharper disable once UnusedMember.Local
        private AuditedDrives GetAssessedDrives(JObject gppCategory)
        {   
            AuditedDrives assessedGppDrives = new AuditedDrives();

            if (gppCategory["Drives"]["Drive"] is JArray)
            {
                foreach (JToken gppDrive in gppCategory["Drives"]["Drive"])
                {
                    AuditedDrive assessedGppDrive = AssessGppDrive(gppDrive);
                    if (assessedGppDrive != null)
                    {
                        try
                        {
                            assessedGppDrives.Drives.Add(assessedGppDrive);
                        }
                        catch (System.ArgumentException)
                        {
                            // in some rare cases we can have duplicated drive UIDs in the same file, just ignore it
                        }
                    }
                }
            }
            else
            {
                AuditedDrive assessedGppDrive = AssessGppDrive(gppCategory["Drives"]["Drive"]);
                assessedGppDrives.Drives.Add(assessedGppDrive);
            }

            if (assessedGppDrives.Drives.Count > 0)
            {
                return assessedGppDrives;
            }
            else
            {
                return null;
            }
        }

        private AuditedDrive AssessGppDrive(JToken gppDrive)
        {
            if (gppDrive == null)
            {
                Log.Degub("This shouldnt be null!!!!!!!!!!!!!!!");
                return null;
            }
               
            int interestLevel = 1;
            string gppDriveUid = JUtil.GetSafeString(gppDrive, "@uid");
            string gppDriveName = JUtil.GetSafeString(gppDrive, "@name");
            string gppDriveChanged = JUtil.GetSafeString(gppDrive, "@changed");
            string gppDriveAction = JUtil.GetActionString(gppDrive["Properties"]["@action"].ToString());
            string gppDriveUserName = JUtil.GetSafeString(gppDrive["Properties"], "@userName");
            string gppDrivecPassword = JUtil.GetSafeString(gppDrive["Properties"], "@cpassword");
            string gppDrivePassword = "";
            if (gppDrivecPassword.Length > 0)
            {
                gppDrivePassword = Util.DecryptCpassword(gppDrivecPassword);
                interestLevel = 10;
            }

            string gppDriveLetter = "";
            if (gppDrive["Properties"]["@useLetter"] != null)
            {
                if (gppDrive["Properties"]["@useLetter"].ToString() == "1")
                {
                    gppDriveLetter = JUtil.GetSafeString(gppDrive["Properties"], "@letter");
                }
                else if (gppDrive["Properties"]["@useLetter"].ToString() == "0")
                {
                    gppDriveLetter = "First letter available, starting at " +
                                     JUtil.GetSafeString(gppDrive["Properties"], "@letter");
                }
            }

            string gppDriveLabel = JUtil.GetSafeString(gppDrive["Properties"], "@label");
            AuditedPath gppDrivePath = FileSystem.InvestigatePath(gppDrive["Properties"]["@path"].ToString());
            if (gppDrivePath != null)
            {
                int pathInterestLevel = gppDrivePath.Interest;
                if (pathInterestLevel > interestLevel)
                {
                    interestLevel = pathInterestLevel;
                }
            }

            if (interestLevel >= this.InterestLevel)
            {
                AuditedDrive ret = new AuditedDrive()
                {
                    Uid = gppDriveUid,
                    Name = gppDriveName,
                    Action = gppDriveAction,
                    Changed = gppDriveChanged,
                    Letter = gppDriveLetter,
                    Label = gppDriveLabel
                };
                
                if (gppDrivecPassword.Length > 0)
                {
                    ret.Username = gppDriveUserName;
                    ret.CPass = gppDrivecPassword;
                    ret.CPassDecrypted = gppDrivePassword;
                }

                if (gppDrivePath != null)
                {
                    ret.AuditedPath = gppDrivePath;
                    //assessedGppDrive.Add("Path", gppDrivePath);
                }

                return ret;
            }

            return null;
        }
    }
}
