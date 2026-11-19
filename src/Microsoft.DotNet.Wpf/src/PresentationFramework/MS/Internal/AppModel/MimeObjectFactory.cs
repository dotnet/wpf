// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description:
// Provides a set of static methods for transforming pairs of
// mime type + stream into objects.
//

using System.IO;
using System.Windows.Markup;

namespace MS.Internal.AppModel
{
    internal delegate object StreamToObjectFactoryDelegateCore(Stream s, Uri baseUri, bool sandboxExternalContent, bool allowAsync,
                                                               bool isJournalNavigation, out XamlReader asyncObjectConverter, bool isUnsafe);

    internal static class MimeObjectFactory
    {
        /// <summary>
        /// Stores content type along with its factory callback.
        /// </summary>
        private static readonly Dictionary<ContentType, StreamToObjectFactoryDelegateCore> s_objectConvertersCore = new(9, new ContentType.WeakComparer());

        /// <remarks>
        /// The delegate that we are calling is responsible for closing the stream
        /// </remarks>
        internal static object GetObjectAndCloseStreamCore(Stream s, ContentType contentType, Uri baseUri, bool sandboxExternalContent, bool allowAsync,
                                                           bool isJournalNavigation, out XamlReader asyncObjectConverter, bool isUnsafe)
        {
            if (contentType is not null && s_objectConvertersCore.TryGetValue(contentType, out StreamToObjectFactoryDelegateCore callback))
                return callback(s, baseUri, sandboxExternalContent, allowAsync, isJournalNavigation, out asyncObjectConverter, isUnsafe);

            asyncObjectConverter = null;
            return null;
        }

        /// <remarks>
        /// The delegate registered here will be responsible for closing the stream passed to it.
        /// </remarks>
        internal static void RegisterCore(ContentType contentType, StreamToObjectFactoryDelegateCore method)
        {
            s_objectConvertersCore[contentType] = method;
        }
    }
}
