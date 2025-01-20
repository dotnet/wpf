// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Packaging;
using System.Windows.Navigation;

namespace MS.Internal.AppModel
{
    /// <summary>
    /// SiteOfOriginContainer is an implementation of the abstract Package class. 
    /// It contains nontrivial overrides for GetPartCore and Exists.
    /// Many of the methods on Package are not applicable to loading files 
    /// so the SiteOfOriginContainer implementations of these methods throw 
    /// the NotSupportedException.
    /// </summary>
    internal sealed class SiteOfOriginContainer : Package
    {
        private static readonly BooleanSwitch s_traceSwitch = new("SiteOfOrigin", "SiteOfOriginContainer and SiteOfOriginPart trace messages");

        /// <summary>
        /// Represents the debug trace <see cref="BooleanSwitch"/> for <see cref="SiteOfOriginContainer"/> and <see cref="SiteOfOriginPart"/>.
        /// </summary>
        internal static bool TraceSwitchEnabled
        {
            get => s_traceSwitch.Enabled;
            set => s_traceSwitch.Enabled = value;
        }

        /// <summary>
        /// Retrieves the site of origin, derived from <see cref="AppContext.BaseDirectory"/>.
        /// </summary>
        internal static Uri SiteOfOrigin
        {
            get
            {
                // Calling FixFileUri because BaseDirectory will be a c:\\ style path
                Uri siteOfOrigin = BaseUriHelper.FixFileUri(new Uri(AppContext.BaseDirectory));
#if DEBUG
                if (TraceSwitchEnabled)
                {
                    Trace.TraceInformation($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.Millisecond} {Environment.CurrentManagedThreadId}" +
                                           $": SiteOfOriginContainer: returning site of origin {siteOfOrigin}");
                }
#endif

                return siteOfOrigin;
            }
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        internal SiteOfOriginContainer() : base(FileAccess.Read)
        {
        }

        /// <remarks>
        /// If this were to be implemented for http site of origin, it will require a server round trip.
        /// </remarks>
        /// <returns>Regardless of <paramref name="uri"/> input, always returns <see langword="true"/>.</returns>
        public override bool PartExists(Uri uri)
        {
            return true;
        }

        /// <summary>
        /// This method creates a SiteOfOriginPart which will create a WebRequest
        /// to access files at the site of origin.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected override PackagePart GetPartCore(Uri uri)
        {
#if DEBUG
            if (TraceSwitchEnabled)
            {
                Trace.TraceInformation($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.Millisecond} {Environment.CurrentManagedThreadId}" +
                                       $": SiteOfOriginContainer: Creating SiteOfOriginPart for Uri {uri}");
            }
#endif
            return new SiteOfOriginPart(this, uri);
        }

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
    }
}
