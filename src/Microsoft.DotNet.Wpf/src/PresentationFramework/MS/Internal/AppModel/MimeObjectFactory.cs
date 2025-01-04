// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Provides a set of static methods for transforming pairs of
// mime type + stream into objects.
//

using System.IO;
using System.Windows.Markup;

namespace MS.Internal.AppModel
{
    internal delegate object StreamToObjectFactoryDelegateCore(Stream s, Uri baseUri, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter, bool isUnsafe);

    internal static class MimeObjectFactory
    {
        //------------------------------------------------------
        //
        //  Internal Static Methods
        //
        //------------------------------------------------------

        #region internal static methods

        // The delegate that we are calling is responsible for closing the stream
        internal static object GetObjectAndCloseStreamCore(Stream s, ContentType contentType, Uri baseUri, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter, bool isUnsafe)
        {
            object objToReturn = null;
            asyncObjectConverter = null;

            if (contentType != null)
            {
                if (_objectConvertersCore.TryGetValue(contentType, out StreamToObjectFactoryDelegateCore d))
                {
                    objToReturn = d(s, baseUri, sandboxExternalContent, allowAsync, isJournalNavigation, out asyncObjectConverter, isUnsafe);
                }
            }

            return objToReturn;
        }
        // The delegate registered here will be responsible for closing the stream passed to it.
        internal static void RegisterCore(ContentType contentType, StreamToObjectFactoryDelegateCore method)
        {
            _objectConvertersCore[contentType] = method;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------

        #region private members

        private static readonly Dictionary<ContentType, StreamToObjectFactoryDelegateCore> _objectConvertersCore = new Dictionary<ContentType, StreamToObjectFactoryDelegateCore>(9, new ContentType.WeakComparer());

        #endregion
    }
}
