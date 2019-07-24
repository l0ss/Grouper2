using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grouper2.Auditor
{
    public class AuditReport
    {

        public string Runtime { get; set; }

        public ConcurrentDictionary<string, AdPolicy> CurrentPolicies { get; private set; }
        public ConcurrentBag<Finding> Scripts { get; internal set; }

        internal void AddPolicyReport(string policySysvolLocation, AdPolicy policyReport)
        {
            // sanity checking bullshit
            if (string.IsNullOrWhiteSpace(policySysvolLocation))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(policySysvolLocation));
            if (policyReport == null) throw new ArgumentNullException(nameof(policyReport));
            if (CurrentPolicies.ContainsKey(policySysvolLocation)) throw new ArgumentException("FUCK!");
            
            // add the thing with an indicator of current policy or not
            this.CurrentPolicies.TryAdd(
                policySysvolLocation.ToLower().Contains("ntfrs")
                    ? policySysvolLocation
                    : $"Current Policy - {policySysvolLocation}", policyReport);
        }

        internal AuditReport()
        {
            this.CurrentPolicies = new ConcurrentDictionary<string, AdPolicy>();
            this.Scripts = new ConcurrentBag<Finding>();
        }


    }
}
