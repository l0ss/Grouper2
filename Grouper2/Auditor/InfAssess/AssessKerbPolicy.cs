using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Auditor
{
    public partial class GrouperAuditor
    {
        public  AuditedKerbPolicy AssessKerbPolicy(JToken kerbPolicy)
        {
            if (kerbPolicy == null) 
                throw new ArgumentNullException(nameof(kerbPolicy));
            /* Defaults

        MaxTicketAge = 10
        MaxRenewAge = 7
        MaxServiceAge = 600
        MaxClockSkew = 5
        TicketValidateClient = 1
        */

            AuditedKerbPolicy assessedKerbPolicy = new AuditedKerbPolicy();

            // basically with this we literally only check if they've deviated from defaults, except on TicketValidateClient.
            int interestLevel = 0;
            if (kerbPolicy["MaxTicketAge"] != null)
            {
                if (kerbPolicy["MaxTicketAge"].ToString() != "10")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.TicketAge = kerbPolicy["MaxTicketAge"].ToString() + " hours";
                }
            }

            if (kerbPolicy["MaxRenewAge"] != null)
            {
                if (kerbPolicy["MaxRenewAge"].ToString() != "7")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.MaxRenewAge = kerbPolicy["MaxRenewAge"].ToString() + " days";
                }
            }

            if (kerbPolicy["MaxServiceAge"] != null)
            {
                if (kerbPolicy["MaxServiceAge"].ToString() != "600")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.MaxServiceAge = kerbPolicy["MaxServiceAge"].ToString() + " minutes";
                }
            }

            if (kerbPolicy["MaxClockSkew"] != null)
            {
                if (kerbPolicy["MaxClockSkew"].ToString() != "5")
                {
                    interestLevel = 1;
                    assessedKerbPolicy.MaxClockSkew = kerbPolicy["MaxClockSkew"].ToString() + " minutes";
                }
            }

            if (kerbPolicy["TicketValidateClient"] != null)
            {
                if (kerbPolicy["TicketValidateClient"].ToString() != "1")
                {
                    interestLevel = 2;
                    assessedKerbPolicy.TicketValidateClient = false.ToString();
                }
            }

            // only return a valid thing if it meets our interests
            return interestLevel <= this.InterestLevel 
                ? null 
                : assessedKerbPolicy;
        }
    }
}