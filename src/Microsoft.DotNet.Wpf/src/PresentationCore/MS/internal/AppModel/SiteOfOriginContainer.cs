// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
// SiteOfOriginContainer is an implementation of the abstract Package class. 
// It contains nontrivial overrides for GetPartCore and Exists.
// Many of the methods on Package are not applicable to loading application 
// resources, so the SiteOfOriginContainer implementations of these methods throw 
// the NotSupportedException.
// 

using System;
using System.IO.Packaging;
using System.IO;
using System.Collections.Generic;
using System.Windows.Resources;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Windows;
using System.Windows.Navigation;
using System.Security;
using MS.Internal.PresentationCore;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MS.Internal.AppModel
{
    /// <summary>
    /// SiteOfOriginContainer is an implementation of the abstract Package class. 
    /// It contains nontrivial overrides for GetPartCore and Exists.
    /// Many of the methods on Package are not applicable to loading files 
    /// so the SiteOfOriginContainer implementations of these methods throw 
    /// the NotSupportedException.
    /// </summary>
    internal class SiteOfOriginContainer : System.IO.Packaging.Package
    {
        //------------------------------------------------------
        //
        //  Static Methods
        //
        //------------------------------------------------------

        #region Static Methods

        internal static Uri SiteOfOrigin
        {
            [FriendAccessAllowed]
            get
            {
                Uri siteOfOrigin = SiteOfOriginForClickOnceApp;
                if (siteOfOrigin == null)
                {
                    // Calling FixFileUri because BaseDirectory will be a c:\\ style path
                    siteOfOrigin = BaseUriHelper.FixFileUri(new Uri(System.AppDomain.CurrentDomain.BaseDirectory));
                }

                Trace($"SiteOfOriginContainer: returning site of origin {siteOfOrigin}");

                return siteOfOrigin;
            }
        }

        // we separated this from the rest of the code because this code is used for media permission
        // tests in partial trust but we want to do this without hitting the code path for regular exe's
        // as in the code above. This will get hit for click once apps, xbaps, xaml and xps
        internal static Uri SiteOfOriginForClickOnceApp
        {
            get
            {
                // The ClickOnce API, ApplicationDeployment.IsNetworkDeployed, determines whether the app is network-deployed
                // by getting the ApplicationDeployment.CurrentDeployment property and catch the exception it can throw.
                // The exception is a first chance exception and caught, but it often confuses developers,
                // and can also have a perf impact. So we change to cache the value of SiteofOrigin in Dev10 to avoid the 
                // exception being thrown too many times.
                // An alternative is to cache the value of ApplicationDeployment.IsNetworkDeployed.
                if (_siteOfOriginForClickOnceApp == null)
                {
                    _siteOfOriginForClickOnceApp = new SecurityCriticalDataForSet<Uri>(null);
                }

                Invariant.Assert(_siteOfOriginForClickOnceApp != null);

                return _siteOfOriginForClickOnceApp.Value.Value;
            }
        }
       
        internal static Uri BrowserSource
        {
            get
            {
                return _browserSource.Value;
            }
            set
            {    
               _browserSource.Value = value; 
            }
        }
   
        #endregion

        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        internal SiteOfOriginContainer() : base(FileAccess.Read)
        {         
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        // None  

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------        

        #region Public Methods

        /// <remarks>
        /// If this were to be implemented for http site of origin, 
        /// it will require a server round trip.
        /// </remarks>
        /// <param name="uri"></param>
        /// <returns></returns>
        public override bool PartExists(Uri uri)
        {
            return true;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

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

        internal static System.Diagnostics.BooleanSwitch _traceSwitch = 
            new System.Diagnostics.BooleanSwitch("SiteOfOrigin", "SiteOfOriginContainer and SiteOfOriginPart trace messages");

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        [Conditional("DEBUG")]
        internal static void Trace(string message)
        {
            if (_traceSwitch.Enabled)
            {
                System.Diagnostics.Trace.TraceInformation(
                    $"{DateTime.Now:T} {DateTime.Now.Millisecond} {Environment.CurrentManagedThreadId}: {message}");
            }

        }

        [Conditional("DEBUG")]
        internal static void Trace(ref TraceInterpolatedStringHandler message)
        {
            if (_traceSwitch.Enabled)
            {
                System.Diagnostics.Trace.TraceInformation(message.ToStringAndClear());
            }

        }

        /// <summary>
        ///   Provides an interpolated string handler for <see
        ///   cref="TraceSwitchExtensions.TraceVerbose(TraceSwitch?, ref TraceSwitchExtensions.TraceVerboseInterpolatedStringHandler)"
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
            internal TraceInterpolatedStringHandler(int literalLength, int formattedCount, out bool shouldAppend)
            {
                if (_traceSwitch.Enabled)
                {
                    _builder = new StringBuilder();
                    _builder.Append($"{DateTime.Now:T} {DateTime.Now.Millisecond} {Environment.CurrentManagedThreadId}: ");
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
        //  Internal Events
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Protected Constructors
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// This method creates a SiteOfOriginPart which will create a WebRequest
        /// to access files at the site of origin.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected override PackagePart GetPartCore(Uri uri)
        {
            Trace($"SiteOfOriginContainer: Creating SiteOfOriginPart for Uri {uri}");
            return new SiteOfOriginPart(this, uri);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        private static SecurityCriticalDataForSet<Uri> _browserSource;
        private static SecurityCriticalDataForSet<Uri>? _siteOfOriginForClickOnceApp;

        #endregion Private Members

        //------------------------------------------------------
        //
        //  Uninteresting (but required) overrides
        //
        //------------------------------------------------------
        #region Uninteresting (but required) overrides

        protected override PackagePart CreatePartCore(Uri uri, string contentType, CompressionOption compressionOption)
        {
            return null;
        }

        protected override void DeletePartCore(Uri uri)
        {
            throw new NotSupportedException();
        }

        protected override PackagePart[] GetPartsCore()
        {
            throw new NotSupportedException();
        }

        protected override void FlushCore()
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
