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
            string gppDriveUid = Utility.GetSafeString(gppDrive, "@uid");
            string gppDriveName = Utility.GetSafeString(gppDrive, "@name");
            string gppDriveChanged = Utility.GetSafeString(gppDrive, "@changed");
            string gppDriveAction = Utility.GetActionString(gppDrive["Properties"]["@action"].ToString());
            string gppDriveUserName = Utility.GetSafeString(gppDrive["Properties"], "@userName");
            string gppDrivecPassword = Utility.GetSafeString(gppDrive["Properties"], "@cpassword");
            string gppDrivePassword = "";
            if (gppDrivecPassword.Length > 0)
            {
                gppDrivePassword = Utility.DecryptCpassword(gppDrivecPassword);
                interestLevel = 10;
            }

            string gppDriveLetter = "";
            if (gppDrive["Properties"]["@useLetter"] != null)
            {
                if (gppDrive["Properties"]["@useLetter"].ToString() == "1")
                {
                    gppDriveLetter = Utility.GetSafeString(gppDrive["Properties"], "@letter");
                }
                else if (gppDrive["Properties"]["@useLetter"].ToString() == "0")
                {
                    gppDriveLetter = "First letter available, starting at " +
                                     Utility.GetSafeString(gppDrive["Properties"], "@letter");
                }
            }

            string gppDriveLabel = Utility.GetSafeString(gppDrive["Properties"], "@label");
            JObject gppDrivePath = FileSystem.InvestigatePath(gppDrive["Properties"]["@path"].ToString());
            if (gppDrivePath["InterestLevel"] != null)
            {
                int pathInterestLevel = int.Parse(gppDrivePath["InterestLevel"].ToString());
                if (pathInterestLevel > interestLevel)
                {
                    interestLevel = pathInterestLevel;
                }
            }

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                JObject assessedGppDrive = new JObject
                {
                    {"Name", gppDriveName},
                    {"Action", gppDriveAction},
                    {"Changed", gppDriveChanged},
                    {"Path", gppDrivePath},
                    {"Drive Letter", gppDriveLetter},
                    {"Label", gppDriveLabel}
                };
                if (gppDrivecPassword.Length > 0)
                {
                    assessedGppDrive.Add("Username", gppDriveUserName);
                    assessedGppDrive.Add("cPassword", gppDrivecPassword);
                    assessedGppDrive.Add("Decrypted Password", gppDrivePassword);
                }
                return new JProperty(gppDriveUid, assessedGppDrive);
            }

            return null;
        }
    }
}
