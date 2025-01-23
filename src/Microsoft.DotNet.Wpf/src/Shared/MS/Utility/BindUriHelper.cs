// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

#if PRESENTATION_CORE
using System.Windows.Navigation;

namespace MS.Internal.PresentationCore
#elif PRESENTATIONFRAMEWORK
using System.Windows.Navigation;

namespace MS.Internal.Utility
#elif REACHFRAMEWORK
namespace MS.Internal.Utility
#else
#error Class is being used from an unknown assembly.
#endif
{
    // Methods in this partial class are shared by PresentationFramework. See also WpfWebRequestHelper.
    internal static partial class BindUriHelper
   {
        private const int MAX_PATH_LENGTH = 2048 ;
        private const int MAX_SCHEME_LENGTH = 32;
        public const int MAX_URL_LENGTH = MAX_PATH_LENGTH + MAX_SCHEME_LENGTH + 3; /*=sizeof("://")*/

        //
        // Uri-toString does 3 things over the standard .toString()
        //
        //  1) We don't unescape special control characters. The default Uri.ToString() 
        //     will unescape a character like ctrl-g, or ctrl-h so the actual char is emitted. 
        //     However it's considered safer to emit the escaped version. 
        //
        //  2) We truncate urls so that they are always <= MAX_URL_LENGTH
        // 
        // This method should be called whenever you are taking a Uri
        // and performing a p-invoke on it. 
        //
        internal static string UriToString(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            return new StringBuilder(
                uri.GetComponents(
                    uri.IsAbsoluteUri ? UriComponents.AbsoluteUri : UriComponents.SerializationInfoString, 
                    UriFormat.SafeUnescaped), 
                MAX_URL_LENGTH).ToString();
        }

#if PRESENTATION_CORE || PRESENTATIONFRAMEWORK
        static internal bool DoSchemeAndHostMatch(Uri first, Uri second)
        {
            // Check that both the scheme and the host match. 
            return string.Equals(first.Scheme, second.Scheme, StringComparison.OrdinalIgnoreCase) && string.Equals(first.Host, second.Host);
        }

        static internal Uri GetResolvedUri(Uri baseUri, Uri orgUri)
        {
            Uri newUri;
            
            if (orgUri == null)
            {
                newUri = null;
            }
            else if (!orgUri.IsAbsoluteUri)
            {
                // if the orgUri is an absolute Uri, don't need to resolve it again.
                newUri = new Uri(baseUri ?? BaseUriHelper.PackAppBaseUri, orgUri);
            }
            else
            {
                newUri = BaseUriHelper.FixFileUri(orgUri);
            }

            return newUri;
        }

#endif // PRESENTATION_CORE || PRESENTATIONFRAMEWORK
    }
}
