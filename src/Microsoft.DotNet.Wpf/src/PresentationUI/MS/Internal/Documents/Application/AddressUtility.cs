// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: 
//    A collection of utility functions for dealing with e-mail address
//    strings and Uri objects.
using System;
using System.Globalization;

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
            return mailtoUri is not null && string.Equals(mailtoUri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);
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
                address = $"{mailtoUri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped)}@{mailtoUri.GetComponents(UriComponents.Host, UriFormat.Unescaped)}";                                                   
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
                // The delimiter between the scheme and the address in a mailto URI.
                // We unfortunately cannot use the Uri class SchemeDelimiter because
                // it is by default set to "://", which makes mailto: URIs generated
                // look like "mailto://user@host", which in turn cannot be parsed
                // properly by this class.
                return new Uri($"{Uri.UriSchemeMailto}:{address}");
            }

            return null;
        }

        #endregion Internal Methods
    }
}
