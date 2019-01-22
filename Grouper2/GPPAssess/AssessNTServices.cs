using Newtonsoft.Json.Linq;

namespace Grouper2
{
    public partial class AssessGpp
    {
        private JObject GetAssessedNTServices(JObject gppCategory)
        {
            JObject assessedGppNTServices = new JObject();

            if (gppCategory["NTService"] is JArray)
            {
                foreach (JToken gppNTService in gppCategory["NTService"])
                {
                    JProperty assessedGppNTService = AssessGppNTService(gppNTService);
                    assessedGppNTServices.Add(assessedGppNTService);
                }
            }
            else
            {
                JProperty assessedGppNTService = AssessGppNTService(gppCategory["NTService"]);
                assessedGppNTServices.Add(assessedGppNTService);
            }

            if (assessedGppNTServices.HasValues)
            {
                return assessedGppNTServices;
            }
            else
            {
                return null;
            }
        }

        static JProperty AssessGppNTService(JToken gppNTService)
        {
            int interestLevel = 1;
            string gppNTServiceUid = Utility.GetSafeString(gppNTService, "@uid");
            string gppNTServiceChanged = Utility.GetSafeString(gppNTService, "@changed");
            string gppNTServiceName = Utility.GetSafeString(gppNTService, "@name");
            string gppNTServiceUserName = Utility.GetSafeString(gppNTService["Properties"], "@accountName");
            string gppNTServicecPassword = Utility.GetSafeString(gppNTService["Properties"], "@cpassword");
            string gppNTServicePassword = "";
            if (gppNTServicecPassword.Length > 0)
            {
                gppNTServicePassword = Utility.DecryptCpassword(gppNTServicecPassword);
                interestLevel = 10;
            }
            
            string gppNTServiceTimeout = Utility.GetSafeString(gppNTService["Properties"], "@timeout");
            string gppNTServiceStartup = Utility.GetSafeString(gppNTService["Properties"], "@startupType");
            string gppNTServiceAction = Utility.GetSafeString(gppNTService["Properties"], "@serviceAction");
            string gppNTServiceServiceName = Utility.GetSafeString(gppNTService["Properties"], "@serviceName");

            string gppNTServiceFirstFailure = Utility.GetSafeString(gppNTService["Properties"], "@firstFailure");
            string gppNTServiceSecondFailure = Utility.GetSafeString(gppNTService["Properties"], "@secondFailure");
            string gppNTServiceThirdFailure = Utility.GetSafeString(gppNTService["Properties"], "@thirdFailure");

            
            JObject gppNTServiceProgram =
                Utility.InvestigatePath(Utility.GetSafeString(gppNTService["Properties"], "@program"));
            JObject gppNTServiceArgs =
                Utility.InvestigateString(Utility.GetSafeString(gppNTService["Properties"], "@args"));

            
            
            if (gppNTServiceProgram["InterestLevel"] != null)
            {
                int progInterestLevelInt = int.Parse(gppNTServiceProgram["InterestLevel"].ToString());
                if (progInterestLevelInt > interestLevel)
                {
                    interestLevel = progInterestLevelInt;
                }
            }

            if (gppNTServiceArgs["InterestLevel"] != null)
            {
                int argsInterestLevelInt = int.Parse(gppNTServiceArgs["InterestLevel"].ToString());
                if (argsInterestLevelInt > interestLevel)
                {
                    interestLevel = argsInterestLevelInt;
                }
            }

            if (interestLevel >= GlobalVar.IntLevelToShow)
            {
                JObject assessedGppNTService = new JObject();
                assessedGppNTService.Add("Name", gppNTServiceName);
                assessedGppNTService.Add("Changed", gppNTServiceChanged);
                if (gppNTServiceServiceName.Length > 0) assessedGppNTService.Add("Service Name", gppNTServiceName);
                if (gppNTServicecPassword.Length > 0)
                {
                    assessedGppNTService.Add("Username", gppNTServiceUserName);
                    assessedGppNTService.Add("cPassword", gppNTServicecPassword);
                    assessedGppNTService.Add("Decrypted Password", gppNTServicePassword);
                }
                if (gppNTServiceTimeout.Length > 0) assessedGppNTService.Add("Timeout", gppNTServiceTimeout);
                if (gppNTServiceStartup.Length > 0) assessedGppNTService.Add("Startup Type", gppNTServiceStartup);
                if (gppNTServiceAction.Length > 0) assessedGppNTService.Add("Action", gppNTServiceAction);
                if (gppNTServiceFirstFailure.Length > 0) assessedGppNTService.Add("Action on first failure", gppNTServiceFirstFailure);
                if (gppNTServiceSecondFailure.Length > 0) assessedGppNTService.Add("Action on second failure", gppNTServiceSecondFailure);
                if (gppNTServiceThirdFailure.Length > 0) assessedGppNTService.Add("Action on third failure", gppNTServiceThirdFailure);
                if (gppNTServiceProgram.HasValues) assessedGppNTService.Add("Program", gppNTServiceProgram);
                if (gppNTServiceArgs.HasValues) assessedGppNTService.Add("Args", gppNTServiceArgs);

                return new JProperty(gppNTServiceUid, assessedGppNTService);
            }

            return null;
        }
    }
}

