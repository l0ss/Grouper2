using Newtonsoft.Json.Linq;

namespace Grouper2.InfAssess
{
    internal static partial class AssessInf
    {
        public static JObject AssessKerbPolicy(JToken kerbPolicy)
        {
            int interestLevel = 0;
            /* Defaults

        MaxTicketAge = 10
        MaxRenewAge = 7
        MaxServiceAge = 600
        MaxClockSkew = 5
        TicketValidateClient = 1
        */

            JObject assessedKerbPolicy = new JObject();

            // basically with this we literally only check if they've deviated from defaults, except on TicketValidateClient.

            if (kerbPolicy["MaxTicketAge"] != null)
            {
                if (kerbPolicy["MaxTicketAge"].ToString() != "10")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.Add("Maximum Ticket Age", kerbPolicy["MaxTicketAge"].ToString() + " hours");
                }
            }

            if (kerbPolicy["MaxRenewAge"] != null)
            {
                if (kerbPolicy["MaxRenewAge"].ToString() != "7")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.Add("Maximum lifetime for user ticket renewal", kerbPolicy["MaxRenewAge"].ToString() + " days");
                }
            }

            if (kerbPolicy["MaxServiceAge"] != null)
            {
                if (kerbPolicy["MaxServiceAge"].ToString() != "600")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.Add("Maximum lifetime for service ticket", kerbPolicy["MaxServiceAge"].ToString() + " minutes");
                }
            }

            if (kerbPolicy["MaxClockSkew"] != null)
            {
                if (kerbPolicy["MaxClockSkew"].ToString() != "5")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.Add("Maximum clock skew", kerbPolicy["MaxClockSkew"].ToString() + " minutes");
                }
            }

            if (kerbPolicy["TicketValidateClient"] != null)
            {
                if (kerbPolicy["TicketValidateClient"].ToString() != "1")
                {
                    interestLevel = 2;
                    assessedKerbPolicy.Add("Enforce user logon restrictions", "False");
                }
            }

            if (interestLevel <= GlobalVar.IntLevelToShow)
            {
                assessedKerbPolicy = null;
            }

            return assessedKerbPolicy;
        }
    }
}