using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Grouper2.Host.DcConnection
{
    public partial class Ldap
    {
        private string GetUsernameFromComInterop(string sid)
        {
            // obey the killswitch
            if (!CanSendTraffic || string.IsNullOrWhiteSpace(sid))
            {
                return null;
            }

            try
            {
                // get the user
                string user = AdvapiWrapper.LookupUserNameBySid(sid, out string domain);
                // decide whether to return the user qualified with domain
                return !string.IsNullOrEmpty(domain) ? $"{domain}\\{user}" : user;
            }
            catch (ArgumentException)
            {
                // looks like the sid was malformed?
                return null;
            }
            catch (Exception)
            {
                // we really don't care if advapi failed, do we?
                return null;
            }
        }
    }


    internal static class AdvapiWrapper
    {
        // stuff for ldap or whatever
        private const int NO_ERROR = 0;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        internal enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool LookupAccountSid(
            string lpSystemName,
            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder referencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        public static string LookupUserNameBySid(string sid, out string domain)
        {
            // attempt a lookup using the Advapi binary
            // stolen wholesale from http://www.pinvoke.net/default.aspx/advapi32.LookupAccountSid
            StringBuilder name = new StringBuilder();
            uint cchName = (uint)name.Capacity;
            StringBuilder referencedDomainName = new StringBuilder();
            uint cchReferencedDomainName = (uint)referencedDomainName.Capacity;
            SID_NAME_USE sidUse;

            // sid to get
            SecurityIdentifier sidObj = new SecurityIdentifier(sid);
            byte[] Sid = new byte[sidObj.BinaryLength];
            sidObj.GetBinaryForm(Sid, 0);

            int err = NO_ERROR;
            if (!LookupAccountSid(null, Sid, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
            {
                err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (err == ERROR_INSUFFICIENT_BUFFER)
                {
                    name.EnsureCapacity((int)cchName);
                    referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
                    err = NO_ERROR;
                    if (!LookupAccountSid(null, Sid, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
                        err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                }
            }

            if (err == 0)
            {
                domain = referencedDomainName.ToString();
                return name.ToString();
            }
            else
                throw new Win32Exception(Marshal.GetLastWin32Error());

        }
    }



}
    