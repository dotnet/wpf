// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Security note
//   Mitigation: see DefaultCredentialsZonePolicy / PackageBoundaryGuard
//   Kill switches (default off):
//     Switch.System.Windows.Net.DoNotApplyZoneCheckForDefaultCredentials
//     Switch.System.Windows.Documents.DisableXpsPackageBoundaryEnforcement
//   Do NOT remove the gate logic without security review.

//
// Description:
//
// Helper that decides whether default (NTLM/Kerberos/Negotiate) credentials
// should be attached to an outgoing WebRequest, based on the URL Security Zone
// of the target URI.
//
// Historical note:
//   On .NET Framework, WPF used to register an ICredentialPolicy with
//   System.Net.AuthenticationManager.CredentialPolicy. The framework's auth
//   client modules (NTLM/Kerberos/Negotiate/Basic/Digest) would consult that
//   policy when responding to a 401 challenge and, for Internet/Untrusted
//   zones, suppress the credentials. That mechanism is implemented by
//   <see cref="CustomCredentialPolicy"/>, which is preserved in source for
//   backward-compatibility purposes (selected via the AppContext switch
//   "Switch.System.Windows.Net.DoNotApplyZoneCheckForDefaultCredentials").
//
//   On .NET 5+, AuthenticationManager.CredentialPolicy is obsolete (SYSLIB0009)
//   and is a runtime no-op: the new HttpClient-based pipeline never consults
//   it. Registering a policy therefore does nothing, which silently regressed
//   the original safety behavior.
//
//   To restore .NET Framework parity, callers can perform the zone check
//   inline before enabling UseDefaultCredentials by calling
//   ShouldSendDefaultCredentials below. This is the default behavior; the
//   AppContext switch above can be set to true to opt out and restore the
//   previous (no-op) registration path as a compatibility escape hatch.
//
// Policy:
//   Allow default credentials to flow only to Local Machine / Intranet /
//   Trusted zones. Block them for Internet / Untrusted zones.
//
//   On modern Windows installs without Internet Explorer/Edge zone configuration
//   (and in some service / lockdown contexts), IInternetSecurityManager.MapUrlToZone
//   can return URLZONE_INTERNET even for loopback (127.0.0.1, ::1) and RFC1918
//   private addresses. To preserve the documented intent and avoid silently
//   regressing intranet/loopback authentication scenarios, an explicit IP-based
//   pre-check is performed for definitively non-routable destinations
//   (loopback / RFC1918 / link-local / IPv6 link-local & ULA) before falling
//   back to MapUrlToZone. The pre-check can only relax the decision in cases
//   where the destination is provably non-public; it never widens trust to a
//   public IP, so the Internet-zone leak protection is preserved.
//
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// IMPORTANT: We are creating an instance of IInternetSecurityManager here. This
// is currently also done in the AppSecurityManager at the Framework level and
// in CustomCredentialPolicy. Any modification to either of these classes--
// especially concerning MapUrlToZone--should be considered for both classes.
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//
// An IInternetSecurityManagerSite is not currently needed here, because the
// only method of IInternetSecurityManager that we are calling is MapUrlToZone,
// and that does not prompt the user.
//

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using MS.Internal.PresentationCore;
using MS.Win32;

namespace MS.Internal.AppModel
{
    internal static class DefaultCredentialsZonePolicy
    {
        static DefaultCredentialsZonePolicy()
        {
            _lockObj = new object();
        }

        /// <summary>
        /// Returns true if it is safe to attach default (NTLM/Kerberos/Negotiate)
        /// credentials to a request targeted at <paramref name="uri"/>.
        ///
        /// Restores the .NET Framework behavior that used to be provided by the
        /// now-obsolete <c>AuthenticationManager.CredentialPolicy</c> mechanism
        /// (SYSLIB0009): default credentials are only allowed to flow to URIs
        /// in the Local Machine, Intranet or Trusted security zones.
        /// </summary>
        internal static bool ShouldSendDefaultCredentials(Uri uri)
        {
            // Fail-closed on null or non-absolute URIs.
            //
            // Scope: this gate is only consulted from WpfWebRequestHelper.CreateRequest
            // and only inside the `if (httpRequest != null)` branch - i.e. exclusively
            // for HttpWebRequest/HttpsWebRequest targets. Non-HTTP schemes never reach
            // this method:
            //   * UNC paths (\\server\share\...) flow through FileWebRequest, so
            //     UseDefaultCredentials handling on UNC is unchanged by this gate.
            //   * file://, ftp://, pack:// likewise never produce an HttpWebRequest.
            //   * Relative URIs cannot reach WebRequest.Create at all - that API
            //     throws on relative input - so any null/non-absolute URI here is
            //     an unexpected/degenerate state and we deliberately refuse the
            //     automatic credential handshake rather than silently allow it.
            //
            // "Fail closed" here only suppresses the *automatic* NTLM/Kerberos/
            // Negotiate handshake on a 401 challenge. Explicit credentials set by
            // the caller via WebRequest.Credentials still flow normally and
            // continue to honor any server challenge.
            if (uri == null || !uri.IsAbsoluteUri)
            {
                return false;
            }

            // Explicit pre-check: if every IP the host resolves to is non-routable
            // (loopback / RFC1918 / link-local / IPv6 link-local or ULA), permit
            // credentials. This honors the documented Local Machine / Intranet
            // intent even when IInternetSecurityManager is degraded (e.g. modern
            // Windows installs without IE/Edge zone configuration).
            if (AllResolvedAddressesAreLocalOrPrivate(uri.DnsSafeHost))
            {
                return true;
            }

            switch (MapUrlToZone(uri))
            {
                // Always safe to send default credentials to these zones
                case NativeMethods.URLZONE_LOCAL_MACHINE:
                case NativeMethods.URLZONE_INTRANET:
                case NativeMethods.URLZONE_TRUSTED:
                    return true;

                // Never send default credentials to these zones (or any unknown zone)
                case NativeMethods.URLZONE_INTERNET:
                case NativeMethods.URLZONE_UNTRUSTED:
                default:
                    return false;
            }
        }

        // Per-host result cache. The decision for a given hostname is stable for the
        // lifetime of the process barring DNS changes; caching avoids a synchronous
        // DNS lookup on every outgoing request.
        private static readonly ConcurrentDictionary<string, bool> s_localHostCache =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true when every IP address that <paramref name="host"/> resolves
        /// to is non-routable: IPv4 loopback (127.0.0.0/8), IPv4 link-local
        /// (169.254.0.0/16), RFC1918 private space (10/8, 172.16/12, 192.168/16),
        /// IPv6 loopback (::1), IPv6 link-local (fe80::/10) or IPv6 unique-local
        /// addresses (fc00::/7).
        /// </summary>
        /// <remarks>
        /// Used as a pre-check before IInternetSecurityManager.MapUrlToZone so
        /// loopback/Intranet decisions are reliable even on modern Windows installs
        /// where the zone manager is degraded (no IE/Edge zone configuration).
        /// DNS failures resolve to "not local/private" and fail closed in the caller.
        /// Results are cached per host for the lifetime of the process.
        /// </remarks>
        private static bool AllResolvedAddressesAreLocalOrPrivate(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            return s_localHostCache.GetOrAdd(host, static h =>
            {
                IPAddress[] addresses;
                if (IPAddress.TryParse(h, out IPAddress literal))
                {
                    addresses = new[] { literal };
                }
                else
                {
                    try
                    {
                        addresses = Dns.GetHostAddresses(h);
                    }
                    catch
                    {
                        // DNS failure: do not relax the policy. Fall through to
                        // MapUrlToZone (which will deny by default for unknown zones).
                        return false;
                    }
                }

                if (addresses == null || addresses.Length == 0)
                {
                    return false;
                }

                foreach (IPAddress a in addresses)
                {
                    if (!IsLocalOrPrivate(a))
                    {
                        return false;
                    }
                }
                return true;
            });
        }

        private static bool IsLocalOrPrivate(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = address.GetAddressBytes();
                // 10.0.0.0/8
                if (b[0] == 10) return true;
                // 172.16.0.0/12
                if (b[0] == 172 && (b[1] & 0xF0) == 16) return true;
                // 192.168.0.0/16
                if (b[0] == 192 && b[1] == 168) return true;
                // 169.254.0.0/16 (link-local)
                if (b[0] == 169 && b[1] == 254) return true;
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal) return true;
                if (address.IsIPv6SiteLocal) return true;
                // Unique Local Address fc00::/7
                byte[] b = address.GetAddressBytes();
                if ((b[0] & 0xFE) == 0xFC) return true;
            }

            return false;
        }

        // Sentinel returned by MapUrlToZone when the underlying
        // IInternetSecurityManager could not be created (e.g. exotic Windows SKUs
        // without urlmon registration). The switch in ShouldSendDefaultCredentials
        // treats any non-trusted zone (including this sentinel) as "do not send
        // default credentials", so callers automatically fail closed.
        private const int URLZONE_UNAVAILABLE = -1;

        internal static int MapUrlToZone(Uri uri)
        {
            EnsureSecurityManager();

            UnsafeNativeMethods.IInternetSecurityManager sm = _securityManager;
            if (sm == null)
            {
                // Initialization previously failed; fail closed.
                return URLZONE_UNAVAILABLE;
            }

            sm.MapUrlToZone(BindUriHelper.UriToString(uri), out int targetZone, 0);
            return targetZone;
        }

        private static void EnsureSecurityManager()
        {
            // IMPORTANT: See comments in header r.e. IInternetSecurityManager

            if (_securityManager != null || _securityManagerInitFailed)
            {
                return;
            }

            lock (_lockObj)
            {
                if (_securityManager != null || _securityManagerInitFailed)
                {
                    return;
                }

                try
                {
                    _securityManager = (UnsafeNativeMethods.IInternetSecurityManager)new InternetSecurityManager();
                }
                catch (Exception)
                {
                    // CoCreateInstance failure on exotic Windows SKUs (e.g. Server
                    // Core / Nano Server / future trimmed images without urlmon
                    // registration). Mark as failed so we do not pay the cost of
                    // attempting recreation on every request, and let MapUrlToZone
                    // fail closed -- the caller will refuse to attach default
                    // credentials, which preserves the security guarantee.
                    _securityManagerInitFailed = true;
                }
            }
        }

        [ComImport, ComVisible(false), Guid("7b8a2d94-0ac9-11d1-896c-00c04Fb6bfc4")]
        private class InternetSecurityManager
        {
        }

        private static UnsafeNativeMethods.IInternetSecurityManager _securityManager;

        private static volatile bool _securityManagerInitFailed;

        private static object _lockObj;
    }
}
