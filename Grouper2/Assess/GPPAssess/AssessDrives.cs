using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        // ReSharper disable once UnusedMember.Local
        private JObject GetAssessedDrives(JObject gppCategory)
        {   
            JObject assessedGppDrives = new JObject();

            if (gppCategory["Drive"] is JArray)
            {
                foreach (JToken gppDrive in gppCategory["Drive"])
                {
                    JProperty assessedGppDrive = AssessGppDrive(gppDrive);
                    if (assessedGppDrive != null)
                    {
                        try
                        {
                            assessedGppDrives.Add(assessedGppDrive);
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
                JProperty assessedGppDrive = AssessGppDrive(gppCategory["Drive"]);
                assessedGppDrives.Add(assessedGppDrive);
            }

            if (assessedGppDrives.HasValues)
            {
                return assessedGppDrives;
            }
            else
            {
                return null;
            }
        }

        static JProperty AssessGppDrive(JToken gppDrive)
        {
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
            JObject gppDrivePath = FileSystem.InvestigatePath(gppDrive["Properties"]["@path"].ToString());
            if (gppDrivePath != null)
            {
                if (gppDrivePath["InterestLevel"] != null)
                {
                    int pathInterestLevel = int.Parse(gppDrivePath["InterestLevel"].ToString());
                    if (pathInterestLevel > interestLevel)
                    {
                        interestLevel = pathInterestLevel;
                    }
                }
            }

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                JObject assessedGppDrive = new JObject
                {
                    {"Name", gppDriveName},
                    {"Action", gppDriveAction},
                    {"Changed", gppDriveChanged},
                    {"Drive Letter", gppDriveLetter},
                    {"Label", gppDriveLabel}
                };
                if (gppDrivecPassword.Length > 0)
                {
                    assessedGppDrive.Add("Username", gppDriveUserName);
                    assessedGppDrive.Add("cPassword", gppDrivecPassword);
                    assessedGppDrive.Add("Decrypted Password", gppDrivePassword);
                }

                if (gppDrivePath != null)
                {
                    assessedGppDrive.Add("Path", gppDrivePath);
                }
                return new JProperty(gppDriveUid, assessedGppDrive);
            }

            return null;
        }
    }
}
