using Grouper2.Utility;
using Newtonsoft.Json.Linq;

namespace Grouper2.GPPAssess
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNTServices(JObject gppCategory)
        {
            JObject assessedGppNtServices = new JObject();

            if (gppCategory["NTService"] is JArray)
            {
                foreach (JToken gppNtService in gppCategory["NTService"])
                {
                    JProperty assessedGppNtService = AssessGppNTService(gppNtService);
                    if (assessedGppNtService != null)
                    {
                        assessedGppNtServices.Add(assessedGppNtService);
                    }
                }
            }
            else
            {
                JProperty assessedGppNtService = AssessGppNTService(gppCategory["NTService"]);
                assessedGppNtServices.Add(assessedGppNtService);
            }

            if (assessedGppNtServices.HasValues)
            {
                return assessedGppNtServices;
            }
            else
            {
                return null;
            }
        }

        static JProperty AssessGppNTService(JToken gppNtService)
        {
            int interestLevel = 1;
            string gppNtServiceUid = JUtil.GetSafeString(gppNtService, "@uid");
            string gppNtServiceChanged = JUtil.GetSafeString(gppNtService, "@changed");
            string gppNtServiceName = JUtil.GetSafeString(gppNtService, "@name");
            string gppNtServiceUserName = JUtil.GetSafeString(gppNtService["Properties"], "@accountName");
            string gppNtServicecPassword = JUtil.GetSafeString(gppNtService["Properties"], "@cpassword");
            string gppNtServicePassword = "";
            if (gppNtServicecPassword.Length > 0)
            {
                gppNtServicePassword = Util.DecryptCpassword(gppNtServicecPassword);
                interestLevel = 10;
            }
            
            string gppNtServiceTimeout = JUtil.GetSafeString(gppNtService["Properties"], "@timeout");
            string gppNtServiceStartup = JUtil.GetSafeString(gppNtService["Properties"], "@startupType");
            string gppNtServiceAction = JUtil.GetSafeString(gppNtService["Properties"], "@serviceAction");
            string gppNtServiceServiceName = JUtil.GetSafeString(gppNtService["Properties"], "@serviceName");

            string gppNtServiceFirstFailure = JUtil.GetSafeString(gppNtService["Properties"], "@firstFailure");
            string gppNtServiceSecondFailure = JUtil.GetSafeString(gppNtService["Properties"], "@secondFailure");
            string gppNtServiceThirdFailure = JUtil.GetSafeString(gppNtService["Properties"], "@thirdFailure");

            
            JObject gppNtServiceProgram =
                FileSystem.InvestigatePath(JUtil.GetSafeString(gppNtService["Properties"], "@program"));
            JObject gppNtServiceArgs =
                FileSystem.InvestigateString(JUtil.GetSafeString(gppNtService["Properties"], "@args"));

            
            
            if ((gppNtServiceProgram != null) && (gppNtServiceProgram["InterestLevel"] != null))
            {
                int progInterestLevelInt = int.Parse(gppNtServiceProgram["InterestLevel"].ToString());
                if (progInterestLevelInt > interestLevel)
                {
                    interestLevel = progInterestLevelInt;
                }
            }

            if (gppNtServiceArgs["InterestLevel"] != null)
            {
                int argsInterestLevelInt = int.Parse(gppNtServiceArgs["InterestLevel"].ToString());
                if (argsInterestLevelInt > interestLevel)
                {
                    interestLevel = argsInterestLevelInt;
                }
            }

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                JObject assessedGppNtService = new JObject
                {
                    {"Name", gppNtServiceName}, {"Changed", gppNtServiceChanged}
                };
                if (gppNtServiceServiceName.Length > 0) assessedGppNtService.Add("Service Name", gppNtServiceName);
                if (gppNtServicecPassword.Length > 0)
                {
                    assessedGppNtService.Add("Username", gppNtServiceUserName);
                    assessedGppNtService.Add("cPassword", gppNtServicecPassword);
                    assessedGppNtService.Add("Decrypted Password", gppNtServicePassword);
                }
                if (gppNtServiceTimeout.Length > 0) assessedGppNtService.Add("Timeout", gppNtServiceTimeout);
                if (gppNtServiceStartup.Length > 0) assessedGppNtService.Add("Startup Type", gppNtServiceStartup);
                if (gppNtServiceAction.Length > 0) assessedGppNtService.Add("Action", gppNtServiceAction);
                if (gppNtServiceFirstFailure.Length > 0) assessedGppNtService.Add("Action on first failure", gppNtServiceFirstFailure);
                if (gppNtServiceSecondFailure.Length > 0) assessedGppNtService.Add("Action on second failure", gppNtServiceSecondFailure);
                if (gppNtServiceThirdFailure.Length > 0) assessedGppNtService.Add("Action on third failure", gppNtServiceThirdFailure);
                if (gppNtServiceProgram.HasValues) assessedGppNtService.Add("Program", gppNtServiceProgram);
                if (gppNtServiceArgs.HasValues) assessedGppNtService.Add("Args", gppNtServiceArgs);

                return new JProperty(gppNtServiceUid, assessedGppNtService);
            }

            return null;
        }
    }
}

