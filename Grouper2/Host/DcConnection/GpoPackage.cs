using Newtonsoft.Json;

namespace Grouper2.Host.DcConnection
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class TrusteeKvp
    {
        [JsonProperty("Trustee")] public string Trustee { get; set; }
        [JsonProperty("Display Name")] public string DisplayName { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GpoPackage
    {
        [JsonProperty("cn")]  public string CN { get; set; }
        [JsonProperty("Distinguished Name")]  public string DisplayName { get; set; }
        [JsonProperty("MSI Path")] public string MsiPath { get; set; }
        [JsonProperty("Changed")] public string Changed { get; set; }
        [JsonProperty("Created")] public string Created { get; set; }
        [JsonProperty("Type")] public string Type { get; set; }
        [JsonProperty("ProductCode")] public string ProductCode { get; set; }
        [JsonProperty("Upgrade Code")] public string UpgradeCode { get; set; }
        [JsonProperty("ParentGPO")] public string ParentGpo { get; set; }
        internal string ParentUid { get; set; }

        public GpoPackage(string cn, string displayname, string msipath, string changed, string created, string type, string productcode,
            string upgradecode, string parentGpo)
        {
            this.CN = cn;
            this.DisplayName = displayname;
            this.MsiPath = msipath;
            this.Changed = changed;
            this.Created = created;
            this.Type = type;
            this.ProductCode = productcode;
            this.UpgradeCode = upgradecode;
            this.ParentGpo = parentGpo;
            this.ParentUid = parentGpo.ToLower().Trim('{', '}');
        }
    }
}
