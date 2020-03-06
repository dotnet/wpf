// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    A collection of utility functions for dealing with e-mail address
//    strings and Uri objects.
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MS.Internal.WindowsBase;


namespace MS.Internal.Documents
{
    /// <summary>
    /// A collection of utility functions for dealing with e-mail address
    /// strings and Uri objects.
    /// </summary>
    internal static class AddressUtility
    {
        #region Internal Methods
        //------------------------------------------------------
        // Internal Methods
        //------------------------------------------------------

        /// <summary>
        /// Checks if the scheme of the URI given is of the mailto scheme.
        /// </summary>
        /// <param name="mailtoUri">The URI to check</param>
        /// <returns>Whether or not it is a mailto URI</returns>
        internal static bool IsMailtoUri(Uri mailtoUri)
        {
            if (mailtoUri != null)
            {
                return SecurityHelper.AreStringTypesEqual(
                    mailtoUri.Scheme,
                    Uri.UriSchemeMailto);
            }

            return false;
        }

        /// <summary>
        /// Gets the e-mail address from the URI given.
        /// </summary>
        /// <param name="mailtoUri">The URI</param>
        /// <returns>The e-mail address</returns>
        internal static string GetEmailAddressFromMailtoUri(Uri mailtoUri)
        {
            string address = string.Empty;

            // Ensure it is a mailto: scheme URI
            if (IsMailtoUri(mailtoUri))
            {
                // Create the address from the URI
                address = string.Format(
                    CultureInfo.CurrentCulture,
                    _addressTemplate,
                    mailtoUri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped),
                    mailtoUri.GetComponents(UriComponents.Host, UriFormat.Unescaped));                                                   
            }

            return address;
        }

        /// <summary>
        /// Generate a mailto: URI from the e-mail address given as an
        /// argument.
        /// </summary>
        /// <param name="address">The address to generate a URI from</param>
        /// <returns>The generated URI</returns>
        internal static Uri GenerateMailtoUri(string address)
        {            
            // Add the scheme to the e-mail address to form a URI object
            if (!string.IsNullOrEmpty(address))
            {
                return new Uri(string.Format(
                    CultureInfo.CurrentCulture,
                    _mailtoUriTemplate,
                    Uri.UriSchemeMailto,
                    _mailtoSchemeDelimiter,
                    address));
            }

            return null;
        }

        #endregion Internal Methods

        #region Private Fields
        //--------------------------------------------------------------------------
        // Private Fields
        //--------------------------------------------------------------------------

        /// <summary>
        /// The template for an e-mail address with a user name and a host.
        /// </summary>
        private const string _addressTemplate = "{0}@{1}";

        /// <summary>
        /// The template for a mailto scheme URI with the mailto scheme and an
        /// address.
        /// </summary>
        private const string _mailtoUriTemplate = "{0}{1}{2}";

        /// <summary>
        /// The delimiter between the scheme and the address in a mailto URI.
        /// We unfortunately cannot use the Uri class SchemeDelimiter because
        /// it is by default set to "://", which makes mailto: URIs generated
        /// look like "mailto://user@host", whicn in turn cannot be parsed
        /// properly by this class.
        /// </summary>
        private const string _mailtoSchemeDelimiter = ":";        

        #endregion Private Fields

    }
}
