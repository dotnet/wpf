// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Creates a PackWebRequest object
//
//

#if DEBUG
#define TRACE
#endif

using System;
using System.Net;
using System.Diagnostics;               // for Assert
using MS.Internal.IO.Packaging;         // for PackageCache
using MS.Internal.PresentationCore;     // for ExceptionStringTable
using System.Security;
using MS.Internal;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.IO.Packaging
{
    /// <summary>
    /// Invoked by .NET framework when our schema is recognized during a WebRequest
    /// </summary>
    public sealed class PackWebRequestFactory : IWebRequestCreate
    {
        static PackWebRequestFactory()
        {
#if DEBUG
            _traceSwitch = new BooleanSwitch("PackWebRequest", "PackWebRequest/Response and NetStream trace messages");
#endif
        }
        
        //------------------------------------------------------
        //
        //  IWebRequestCreate
        //
        //------------------------------------------------------
        /// <summary>
        /// Create
        /// </summary>
        /// <param name="uri">uri</param>
        /// <returns>PackWebRequest</returns>
        /// <remarks>Note that this factory may or may not be "registered" with the .NET WebRequest factory as handler
        /// for "pack" scheme web requests.  Because of this, callers should be sure to use the PackUriHelper static class
        /// to prepare their Uri's.  Calling any PackUriHelper method has the side effect of registering
        /// the "pack" scheme and associating this factory class as its default handler.</remarks>
        WebRequest IWebRequestCreate.Create(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            // Ensure uri is absolute - if we don't check now, the get_Scheme property will throw 
            // InvalidOperationException which would be misleading to the caller.
            if (!uri.IsAbsoluteUri)
                throw new ArgumentException(SR.UriMustBeAbsolute, "uri");

            // Ensure uri is correct scheme because we can be called directly.  Case sensitive
            // is fine because Uri.Scheme contract is to return in lower case only.
            if (!string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.Ordinal))
                throw new ArgumentException(SR.Format(SR.UriSchemeMismatch, PackUriHelper.UriSchemePack), "uri");

            Trace(this, $"responding to uri: {uri}");

            // only inspect cache if part name is present because cache only contains an object, not
            // the stream it was derived from
            Uri packageUri = System.IO.Packaging.PackUriHelper.GetPackageUri(uri);
            Uri partUri = System.IO.Packaging.PackUriHelper.GetPartUri(uri);

            if (partUri != null)
            {
                // Note: we look at PreloadedPackages first before we examine the PackageStore
                //  This is to make sure that an app cannot override any predefine packages

                // match cached object by authority component only - ignore the local path (part name)
                // inspect local package cache and default to that if possible

                // All predefined packages such as a package activated by DocumentApplication,
                //  ResourceContainer, and SiteOfOriginContainer are placed in PreloadedPackages
                bool cachedPackageIsThreadSafe;
                Package c = PreloadedPackages.GetPackage(packageUri, out cachedPackageIsThreadSafe);

                // If we don't find anything in the preloaded packages, look into the PackageStore
                bool cachedPackageIsFromPublicStore = false;
                if (c == null)
                {
                    cachedPackageIsThreadSafe = false;          // always assume PackageStore packages are not thread-safe
                    cachedPackageIsFromPublicStore = true;
                    
                    // Try to get a package from the package store
                    c = PackageStore.GetPackage(packageUri);
                }
                
                // do we have a package?
                if (c != null)
                {
                    Trace(this, "cache hit - returning CachedPackWebRequest");
                    // use the cached object
                    return new PackWebRequest(uri, packageUri, partUri, c, 
                        cachedPackageIsFromPublicStore, cachedPackageIsThreadSafe);   
                }
}

            Trace(this, "spawning regular PackWebRequest");

            return new PackWebRequest(uri, packageUri, partUri);
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // CreateWebRequest: Explicitly calls Create on PackWebRequest if uri is pack scheme
        // Ideally we would want to use RegisterPrefix and WebRequest.Create.
        // However, these two functions regress 700k working set in System.dll and System.xml.dll
        //  which is mostly for logging and config.
        // This helper function provides a way to bypass the regression
        [FriendAccessAllowed]
        internal static WebRequest CreateWebRequest(Uri uri)
        {
            if (string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.Ordinal))
            {
                return ((IWebRequestCreate) _factorySingleton).Create(uri);
            }
            else
            {
                return WpfWebRequestHelper.CreateRequest(uri);
            }
        }

#if DEBUG
        internal static bool TraceSwitchEnabled
        {
            get
            {
                return _traceSwitch.Enabled;
            }

            set
            {
                _traceSwitch.Enabled = value;
            }
        }

        private static System.Diagnostics.BooleanSwitch _traceSwitch;
#endif

        [Conditional("DEBUG")]
        internal static void Trace(object caller, string message)
        {
#if DEBUG
            if (_traceSwitch.Enabled)
            {
                System.Diagnostics.Trace.TraceInformation(
                    $"{DateTime.Now:T} {DateTime.Now.Millisecond} {Environment.CurrentManagedThreadId}: {caller.GetType().Name} - {message}");
            }
#endif
        }

        [Conditional("DEBUG")]
        internal static void Trace(object caller, [InterpolatedStringHandlerArgument(nameof(caller))] ref TraceInterpolatedStringHandler message)
        {
#if DEBUG
            if (_traceSwitch.Enabled)
            {
                System.Diagnostics.Trace.TraceInformation(message.ToStringAndClear());
            }
#endif
        }

        /// <summary>
        ///   Provides an interpolated string handler for <see
        ///   cref="PackWebRequestFactory.Trace(object, ref PackWebRequestFactory.TraceInterpolatedStringHandler)"
        ///   /> that only performs formatting if the condition applies.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [InterpolatedStringHandler]
        public struct TraceInterpolatedStringHandler
        {
            /// <summary>
            ///   The handler we use to perform the formatting.
            /// </summary>
            private StringBuilder.AppendInterpolatedStringHandler _stringBuilderHandler;

            /// <summary>
            ///   The underlying <see cref="StringBuilder"/> instance used by <see cref="_stringBuilderHandler"/>,
            ///   if any.
            /// </summary>
            private StringBuilder _builder;

            /// <summary>
            ///   Creates an instance of the handler.
            /// </summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="traceSwitch">The TraceSwitch passed to the <see cref="TraceSwitchExtensions"/> method.</param>
            /// <param name="shouldAppend">A value indicating whether formatting should proceed.</param>
            /// <remarks>
            ///   This is intended to be called only by compiler-generated code. Arguments are not validated as they'd
            ///   otherwise be for members intended to be used directly.
            /// </remarks>
            internal TraceInterpolatedStringHandler(int literalLength, int formattedCount, object caller, out bool shouldAppend)
            {
                if (_traceSwitch.Enabled)
                {
                    _builder = new StringBuilder();
                    _builder.Append($"{DateTime.Now:T} {DateTime.Now.Millisecond} {Environment.CurrentManagedThreadId}: {caller.GetType().Name} - ");
                    _stringBuilderHandler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount,
                        _builder);
                    shouldAppend = true;
                }
                else
                {
                    _stringBuilderHandler = default;
                    shouldAppend = false;
                }
            }

            /// <summary>
            ///   Extracts the built string from the handler.
            /// </summary>
            internal string ToStringAndClear()
            {
                string s = _builder?.ToString() ?? string.Empty;
                _stringBuilderHandler = default;
                _builder = null;
                return s;
            }

            /// <summary>
            ///   Writes the specified string to the handler.
            /// </summary>
            /// <param name="value">The string to write.</param>
            public void AppendLiteral(string value) => _stringBuilderHandler.AppendLiteral(value);

            /// <summary>
            ///   Writes the specified value to the handler.
            /// </summary>
            /// <param name="value">The value to write.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>
            ///   Writes the specified value to the handler.
            /// </summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, string? format) => _stringBuilderHandler.AppendFormatted(value, format);

            /// <summary>
            ///   Writes the specified character span to the handler.
            /// </summary>
            /// <param name="value">The span to write.</param>
            public void AppendFormatted(ReadOnlySpan<char> value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>
            ///   Writes the specified character span to the handler.
            /// </summary>
            /// <param name="value">The value to write.</param>
            public void AppendFormatted(string value) => _stringBuilderHandler.AppendFormatted(value);
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private static PackWebRequestFactory _factorySingleton = new PackWebRequestFactory();
    }
}
