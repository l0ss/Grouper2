using System.IO;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
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
                        assessedGppDrives.Add(assessedGppDrive);
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
            if (gppDrive["Properties"]["@useLetter"].ToString() == "1")
            {
                gppDriveLetter = Utility.GetSafeString(gppDrive["Properties"], "@letter");
            }
            else if (gppDrive["Properties"]["@useLetter"].ToString() == "0")
            {
                gppDriveLetter = "First letter available, starting at " +
                                 Utility.GetSafeString(gppDrive["Properties"], "@letter");
            }

            string gppDriveLabel = Utility.GetSafeString(gppDrive["Properties"], "@label");
            JObject gppDrivePath = Utility.InvestigatePath(gppDrive["Properties"]["@path"].ToString());
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
                JObject assessedGppDrive = new JObject();
                assessedGppDrive.Add("Name", gppDriveName);
                assessedGppDrive.Add("Action", gppDriveAction);
                assessedGppDrive.Add("Changed", gppDriveChanged);
                assessedGppDrive.Add("Path", gppDrivePath);
                assessedGppDrive.Add("Drive Letter", gppDriveLetter);
                assessedGppDrive.Add("Label", gppDriveLabel);
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