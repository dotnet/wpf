// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Provides a set of static methods for transforming pairs of
// mime type + stream into objects.
//

using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using MS.Internal.Utility;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Markup;

namespace MS.Internal.AppModel
{
    internal delegate object StreamToObjectFactoryDelegate(Stream s, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter);

    internal static class MimeObjectFactory
    {
        //------------------------------------------------------
        //
        //  Internal Static Methods
        //
        //------------------------------------------------------

        #region internal static methods

        // The delegate that we are calling is responsible for closing the stream
        internal static object GetObjectAndCloseStream(Stream s, ContentType contentType, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter)
        {
            object objToReturn = null;
            asyncObjectConverter = null;

            if (contentType != null)
            {
                StreamToObjectFactoryDelegate d;
                if (_objectConverters.TryGetValue(contentType, out d))
                {
                    objToReturn = d(s, baseUri, canUseTopLevelBrowser, sandboxExternalContent, allowAsync, isJournalNavigation, out asyncObjectConverter);
                }
            }

            return objToReturn;
        }

        // The delegate registered here will be responsible for closing the stream passed to it.
        internal static void Register(ContentType contentType, StreamToObjectFactoryDelegate method)
        {
            _objectConverters[contentType] = method;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------

        #region private members

        private static readonly Dictionary<ContentType, StreamToObjectFactoryDelegate> _objectConverters = new Dictionary<ContentType, StreamToObjectFactoryDelegate>(capacity: 9, new ContentType.WeakComparer());

        #endregion
    }
}
