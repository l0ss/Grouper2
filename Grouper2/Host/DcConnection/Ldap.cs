using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Grouper2.Auditor;
using Grouper2.Host.DcConnection.Sddl;
using Grouper2.Host.SysVol;
using Grouper2.Utility;

namespace Grouper2.Host.DcConnection
{
    public partial class Ldap
    {
        

        // I honestly don't remember wtf this is for?
        public string CurrentUserSid { get; private set; }


        // stuff for ldap or whatever
        private readonly string _domain = string.Empty;
        private readonly int DesiredInterestLevel;

        // proper props
        private bool _packagesHaveBeenPopulated;



        protected Ldap(bool onlineMode, string domain, int desiredInterestLevel)
        {
            _domain = domain;
            DesiredInterestLevel = desiredInterestLevel;

            // set the killswitch and stop if required
            CanSendTraffic = onlineMode;
            if (!CanSendTraffic)
            {
                GpoPackages = null;
                CurrentUserSid = string.Empty;
                return;
            }

        }

        // if the killswitch was set, return the 
        public bool CanSendTraffic { get; }

        // threadsafe shit
        public ConcurrentBag<Gpo> DomainGpos { get; private set; } 
        public ConcurrentBag<GpoPackage> GpoPackages { get; private set; }
        public ConcurrentBag<Sid> DomainSids { get; private set; }
        private ConcurrentDictionary<string, string> _collectedSidDictionary = new ConcurrentDictionary<string, string>();


        private void CollectOnlineData()
        {
            // obey the killswitch
            if (!this.CanSendTraffic) return;
            
            // force a sid collection and populate the dict
            try
            {
                // get the sids
                _collectedSidDictionary = new ConcurrentDictionary<string, string>();
                DomainSids = CollectAllSids(_domain);
                if (DomainSids == null)
                    throw new ActiveDirectoryOperationException("Unable to get any SIDs from the domain");

                // push some data to the username lookup table
                foreach (Sid sid in DomainSids)
                    _collectedSidDictionary.TryAdd(sid.FullSID.Value, sid.Name);
            }
            catch (Exception e)
            {
                // error propagation. An error here indicates issues with the LDAP conn to the DC
                Log.Degub("Unable to get any SID data from the domain. Possible LDAP connection issues", e);
                return;
            }


            // force a GPO collection
            try
            {
                // collect packages and deal with a fucked up response
                GpoPackages = CollectGpoPackages(_domain);
                if (GpoPackages == null)
                    throw new ActiveDirectoryOperationException("Unable to get any packages from the domain");

                // Get the GPOs and sort the packages.
                DomainGpos = CollectDomainGpos(GpoPackages, DesiredInterestLevel);
                if (DomainGpos == null)
                    throw new ActiveDirectoryOperationException("Unable to get GPOs from the domain");
            }
            catch (Exception e)
            {
                // error propagation. An error here indicates issues with the LDAP conn to the DC
                Log.Degub("Unable to get any GPO or GPO Package data from the domain. Possible LDAP connection issues?", e);
                throw;
            }
        }

        private string LookupUserInCollectedSids(string sidString)
        {
            if (string.IsNullOrWhiteSpace(sidString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sidString));

            // obey the killswitch
            if (!this.CanSendTraffic 
                // and sanity check the collection
                || this._hasBeenCollected == false) 
                return null;
            
            // let's get a little tricky here and keep track of the sids we've already seen in memory to keep
            // the time we spend waiting for network traffic down
            // this was already populated with the sids for the entire domain
            if (_collectedSidDictionary.ContainsKey(sidString))
                try
                {
                    return _collectedSidDictionary.Single(s => s.Key.Equals(sidString)).Value;
                }
                catch (Exception e)
                {
                    Output.DebugWrite(e.ToString());
                    Log.Degub("collected sids probably didn't contain a thing?", e);
                }

            return null;
        }

        public TrusteeKvp GetTrusteeKvp(string trusteeSid)
        {
            // nullcheck
            if (trusteeSid == null) 
                throw new ArgumentNullException(nameof(trusteeSid));
            
            // clean it up just in case
            trusteeSid = trusteeSid.Trim().Trim('*');

            // vladiation
            if (!Sid.IsShoddilyValidatedSid(trusteeSid))
                return null;

            try
            {
                var user = GetUserFromSid(trusteeSid);
                return new TrusteeKvp()
                {
                    Trustee = trusteeSid,
                    DisplayName = user
                };
            }
            catch (Exception e)
            {
                // TODO: this doesn't return what is promised, it should probably be an error
                return new TrusteeKvp()
                {
                    DisplayName = $"SID Lookup Failed: {e.ToString()}",
                    Trustee = trusteeSid
                };
            }
        }

        private string SidLookupOffline(string sidString)
        {
            string name;
            try
            {
                name = Sid.GetWellKnownSidAlias(sidString);
                if (!string.IsNullOrEmpty(name))
                { // since we got something that wasn't already in the quicklookup, add it before return
                    if (!this._collectedSidDictionary.TryAdd(sidString, name))
                    {
                        // failed to add, but it's not a huge deal, so fuck it
                    }
                    return name;
                }
            }
            catch (Exception e)
            {
                Output.DebugWrite(e.ToString());
                Log.Degub("collected sids probably didn't add a thing?", e);
            }

            return null;
        }

        /// <summary>
        /// Threadsafe? :| Resolve a SID string to a username on the localmachine or in the domain
        /// </summary>
        /// <param name="sidString">the SID to lookup</param>
        /// <returns>A valid username in the domain or on the machine</returns>
        public string GetUserFromSid(string sidString)
        {
            // nullcheck
            if (string.IsNullOrWhiteSpace(sidString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sidString));
            
            
            // look in the already collected usernames just in case
            string name = LookupUserInCollectedSids(sidString);
            if (!string.IsNullOrEmpty(name)) 
                return name;

            // if the killswitch isn't set, try online collection first
            if (CanSendTraffic)
            {
                // that didn't work, so attempt an online lookup using the Advapi binary
                try
                {
                    name = GetUsernameFromComInterop(sidString);
                    if (!string.IsNullOrEmpty(name))
                    { // since we got something that wasn't already in the quicklookup, add it before return
                        if (!this._collectedSidDictionary.TryAdd(sidString, name))
                        {
                            // failed to add, but it's not a huge deal, so fuck it
                        }
                        return name;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                // Advapi failed, so fallback to well known sids
                Output.DebugWrite(@"Error in SID Lookup : resolving SID " + sidString +
                                  " handing off to well known sids list.");
            }
            
            // try an offline lookup
            name = SidLookupOffline(sidString);
            if (!string.IsNullOrWhiteSpace(name))
                return name;



            // this doesn't return what is promised, it should probably be an error
            if (this.CanSendTraffic)
                return "LDAP enabled SID Lookup Failed";
            else
                return "Offline SID Lookup Failed.";


        }
        
        private ConcurrentBag<Gpo> CollectDomainGpos(ConcurrentBag<GpoPackage> packagesForDomain, int interest)
        {
            if (packagesForDomain == null) throw new ArgumentNullException(nameof(packagesForDomain));
            if (packagesForDomain.Count == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(packagesForDomain));

            // if the killswitch is set, return a null
            if (!CanSendTraffic) return null;

            try // TODO: this try block is too broad. narrow it
            {
                // new dictionary for data from each GPO to go into
                ConcurrentBag<Gpo> gposData = new ConcurrentBag<Gpo>();
                foreach (SearchResult gpoSearchResult in CollectAllGposData(_domain))
                {
                    // prep to do some shit with the search result
                    Gpo gpoData;
                    string sddlString;

                    using (DirectoryEntry gpoDe = gpoSearchResult.GetDirectoryEntry())
                    {
                        // build out the base Gpo
                        gpoData = new Gpo
                        {
                            DisplayName = gpoDe.Properties["displayName"].Value.ToString(),
                            Uid = gpoDe.Properties["name"].Value.ToString(),
                            DistinguishedName = gpoDe.Properties["distinguishedName"].Value.ToString(),
                            Created = gpoDe.Properties["whenCreated"].Value.ToString()
                        };

                        // check to see if this GPO exists in the list we are building already
                        if (gposData.Any(g => g.Uid.Equals(gpoData.Uid)))
                        {
                            // this is to catch duplicate UIDs caused by Default Domain Policy and Domain Controller Policy having 'well known guids'
                            Output.DebugWrite(
                                "\nI think you're in a multi-domain environment cos I just saw two GPOs with the same GUID. " +
                                "\nYou should be careful not to miss stuff in the Default Domain Policy and Default Domain Controller Policy.");
                            // let's just move on and let the current GPO go out of scope?
                            continue;
                        }

                        // integrate the package data into the gpo
                        // doing it here may miss out on some packages because we release some shit above.
                        // TODO: l0ss, is this the intention? I think the previous code would have kept packages with duplicate parent-UIDs providing (maybe) incorrect results?
                        gpoData.GpoPackages = packagesForDomain.Where(p => gpoData.Uid.Equals(p.ParentUid)).ToList();


                        ////////////////
                        // Human-readify the GPO-Status flag
                        // 3= all disabled
                        // 2= computer configuration settings disabled
                        // 1= user policy disabled
                        // 0 = all enabled
                        switch (gpoDe.Properties["flags"].Value.ToString())
                        {
                            case "0":
                                gpoData.GpoStatus = "Enabled";
                                break;
                            case "1":
                                gpoData.GpoStatus = "User Policy Disabled";
                                break;
                            case "2":
                                gpoData.GpoStatus = "Computer Policy Disabled";
                                break;
                            case "3":
                                gpoData.GpoStatus = "Disabled";
                                break;
                            default:
                                gpoData.GpoStatus = "Couldn't process GPO Enabled Status. Weird.";
                                break;
                        }

                        // get the acl sddl
                        sddlString = gpoDe.ObjectSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                    } // let the directory entry we built go out of scope and dispose itself

                    // build the ACL data
                    gpoData.GpoAcls =
                        ParseSddl.ParseSddlString(sddlString, SecurableObjectType.DirectoryServiceObject);

                    // add the gpo to the list
                    gposData.Add(gpoData);
                }

                // return the list
                return gposData;
            }
            catch (Exception exception)
            {
                Output.DebugWrite(exception.ToString());
                //Console.ReadKey();
                Environment.Exit(1);
            }

            return null;
        }

        /// <summary>
        ///     This is a mess because whoever wrote the active directory package searching library is a big meany.
        ///     It maybe?? works though, so whatever
        /// </summary>
        private ConcurrentBag<GpoPackage> CollectGpoPackages(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(domain));

            // if the killswitch is set, return a null
            if (!CanSendTraffic) return null;
            // sanity check to return early if it's already been done
            if (_packagesHaveBeenPopulated) return this.GpoPackages;

            //iterate through the apps we can find and build out their objects
            ConcurrentBag<GpoPackage> gpoPackages = new ConcurrentBag<GpoPackage>();
            foreach (SearchResult package in CollectAllPackagesData(domain))
            {
                string[] lvItems = new string[8];

                try
                {
                    // set up the vars for the package
                    string cn = package.Properties["cn"][0].ToString();
                    string msiPath = null;
                    string changed = null;
                    string created = null;
                    string type = null;
                    string parentGpoUid = null;
                    string productCodeGuid = null;
                    string upgradeCodeGuid = null;
                    string displayName = package.Properties["displayName"][0].ToString();

                    //check to see if there are transforms
                    if (package.Properties["msiFileList"].Count > 1)
                    {
                        for (int i = 0; i < package.Properties["msiFileList"].Count; i++)
                        {
                            string[] splitPath = package.Properties["msiFileList"][i].ToString()
                                .Split(':');
                            if (splitPath[0] == "0")
                            {
                                msiPath = splitPath[1];
                            }
                            else
                            {
                                // if there is more than one transform, need to concatenate them
                                if (package.Properties["msiFileList"].Count > 2)
                                    lvItems[3] = splitPath[1] + ";" + lvItems[3];
                                else
                                    lvItems[3] = splitPath[1];
                            }
                        }
                    }
                    else
                    {
                        lvItems[2] = package.Properties["msiFileList"][0].ToString()
                            .TrimStart('0', ':');
                        lvItems[3] = "";
                    }

                    //the product code is a byte array, so we need to get the enum on it and iterate through the collection
                    productCodeGuid = new
                        Guid((byte[]) package.Properties["productCode"][0]).ToString();
                    // and again for the upgradeCode
                    upgradeCodeGuid = new
                        Guid((byte[]) package.Properties["upgradeProductCode"][0]).ToString();


                    //now do the whenChanged and whenCreated stuff
                    created = ((DateTime) package.Properties["whenCreated"][0]).ToString("G");
                    changed = ((DateTime) package.Properties["whenChanged"][0]).ToString("G");

                    //Next we need to find the GPO this app is in
                    string FQDN = "";
                    string[] arrFQDN = package.Properties["adsPath"][0].ToString().Split(',');

                    for (int i = 0; i != arrFQDN.Length; i++)
                    {
                        // skip the first 4 in the array
                        if (i <= 3) continue;

                        //if its the first one we want, don't put a comma in front of it
                        if (i == 4)
                            FQDN = arrFQDN[i];
                        else
                            FQDN = FQDN + "," + arrFQDN[i];
                    }

                    parentGpoUid = new
                        DirectoryEntry($"LDAP://{FQDN}").Properties["Name"][0].ToString();

                    //now resolve whether the app is published or assigned
                    if (arrFQDN[3] == "CN=User")
                    {
                        if (package.Properties["msiScriptName"][0].ToString() == "A")
                            type = "User Assigned";
                        if (package.Properties["msiScriptName"][0].ToString() == "P")
                            type = "User Published";
                        if (package.Properties["msiScriptName"][0].ToString() == "R")
                            type = "Package Removed";
                    }
                    else if (package.Properties["msiScriptName"][0].ToString() == "R")
                    {
                        type = "Package Removed";
                    }

                    else
                    {
                        type = "Computer Assigned";
                    }
                    
                    // get the package interest
                    if (PackageInterest(msiPath) >= this.DesiredInterestLevel)
                    {
                        gpoPackages.Add(new GpoPackage(
                            cn,
                            displayName,
                            msiPath,
                            changed,
                            created,
                            type,
                            productCodeGuid,
                            upgradeCodeGuid,
                            parentGpoUid
                        ));
                    }

                    
                }
                catch (Exception e)
                {
                    Output.DebugWrite(e.ToString());
                }
            }

            if (gpoPackages.Count > 0)
            {
                // set the bool indicating the packages were collected
                _packagesHaveBeenPopulated = true;
                return gpoPackages;
            }
            else
            {
                _packagesHaveBeenPopulated = false;
                return null;
            }
        }

        private int PackageInterest(string msiPath)
        {
            if (string.IsNullOrWhiteSpace(msiPath))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(msiPath));
            
            int interestLevel = 3;


            AuditedPath assessedMsiPath = FileSystem.InvestigatePath(msiPath);
            if (assessedMsiPath != null)
            {
                if (assessedMsiPath.Interest > interestLevel)
                {

                    interestLevel = assessedMsiPath.Interest;
                }
            }

            if (interestLevel >= this.DesiredInterestLevel)
            {
                return interestLevel;
            }
            else
            {
                return -10;
            }

        }
    }
}