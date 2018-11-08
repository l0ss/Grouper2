/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;

class Program
{
    static void Main(string[] args)
    {
        DirectoryEntry rootDse = new DirectoryEntry("LDAP://rootDSE");
        DirectoryEntry root = new DirectoryEntry("GC://" + rootDse.Properties["defaultNamingContext"].Value.ToString());
        DirectorySearcher searcher = new DirectorySearcher(root);
        searcher.Filter = "(objectClass=groupPolicyContainer)";

        foreach (SearchResult gpo in searcher.FindAll())
        {
            var gpoDesc = gpo.GetDirectoryEntry().Properties["distinguishedName"].Value.ToString();
            Console.WriteLine($"GPO: {gpoDesc}");

            DirectoryEntry gpoObject = new DirectoryEntry($"LDAP://{gpoDesc}");

            tryusing System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;
            using System.DirectoryServices;

class Program
    {
        static void Main(string[] args)
        {
            DirectoryEntry rootDse = new DirectoryEntry("LDAP://rootDSE");
            DirectoryEntry root = new DirectoryEntry("GC://" + rootDse.Properties["defaultNamingContext"].Value.ToString());
            DirectorySearcher searcher = new DirectorySearcher(root);
            searcher.Filter = "(objectClass=groupPolicyContainer)";

            foreach (SearchResult gpo in searcher.FindAll())
            {
                var gpoDesc = gpo.GetDirectoryEntry().Properties["distinguishedName"].Value.ToString();
                Console.WriteLine($"GPO: {gpoDesc}");

                DirectoryEntry gpoObject = new DirectoryEntry($"LDAP://{gpoDesc}");

                try
                {
                    Console.WriteLine($"DisplayName: {gpoObject.Properties["displayName"].Value.ToString()}");
                }
                catch
                {
                }





            }

            Console.ReadKey();
        }
    }
            {
                Console.WriteLine($"DisplayName: {gpoObject.Properties["displayName"].Value.ToString()}");
            }
            catch
            {
            }





        }

        Console.ReadKey();
    }
}
*/