using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;

namespace Grouper2.Host.DcConnection
{
    public partial class Ldap
    {
        /// <summary>
        /// A networked operation which can be done once at instantiation to prevent
        /// repeated network traffic for SIDs
        /// </summary>
        private ConcurrentBag<Sid> CollectAllSids(string domain)
        {
            // obey the killswitch
            if (!this.CanSendTraffic) return null;

            ConcurrentBag<Sid> retval = new ConcurrentBag<Sid>();
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domain))
            {
                using (PrincipalSearcher searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    foreach (Principal result in searcher.FindAll())
                    {
                        retval.Add(new Sid(result.Sid, result.Name, result.DistinguishedName,
                            result.Description, SidType.User));
                    }
                }

                using (PrincipalSearcher searcher = new PrincipalSearcher(new GroupPrincipal(context)))
                {
                    foreach (Principal result in searcher.FindAll())
                    {
                        retval.Add(new Sid(result.Sid, result.Name, result.DistinguishedName,
                            result.Description, SidType.Group));
                    }
                }
            }

            // return the list or, if empty, a null
            return retval.Count != 0 ? retval : null;
        }

        private SearchResultCollection CollectAllGposData(string domain)
        {
            // obey the killswitch
            if (!this.CanSendTraffic) return null;

            // prep the return object
            SearchResultCollection gpoSearchResults;

            // do some weird directory entry shit to get the correct context for a call, and make disposable
            using (DirectoryEntry contextPath = new DirectoryEntry("LDAP://rootDSE"))
            {
                using (DirectoryEntry rootDefNamingContext = new DirectoryEntry($"GC://{contextPath.Properties["defaultNamingContext"].Value}"))
                {
                    using (DirectorySearcher gpoSearcher = new DirectorySearcher(rootDefNamingContext)
                    {
                        Filter = "(objectClass=groupPolicyContainer)",
                        SecurityMasks = SecurityMasks.Dacl | SecurityMasks.Owner,
                        PageSize = 1000
                    })
                    {
                        // find some gpos
                        gpoSearchResults = gpoSearcher.FindAll();
                    }
                }
            }

            // return the list or, if empty, a null
            return gpoSearchResults.Count == 0 ? null : gpoSearchResults;
        }

        private SearchResultCollection CollectAllPackagesData(string domain)
        {
            // obey the killswitch
            if (!this.CanSendTraffic) return null;

            // this bit c/o @grouppolicyguy
            SearchResultCollection foundPackages = null;
            using (DirectorySearcher packageSearcher = new DirectorySearcher($"LDAP://{domain}/System/Policies")
            {
                Filter = "(objectClass=packageRegistration)",
                PropertiesToLoad = {
                    "displayName",
                    "distinguishedName",
                    "msiFileList",
                    "msiScriptName",
                    "productCode",
                    "whenCreated",
                    "whenChanged",
                    "upgradeProductCode",
                    "cn"
                }
            })
            {
                foundPackages = packageSearcher.FindAll();
            }

            // Check the search results and abort early if needed
            if (foundPackages.Count <= 0)
            {
                return null;
            }

            // return the list or, if empty, a null
            return foundPackages.Count <= 0 ? null : foundPackages;
        }
    }
}