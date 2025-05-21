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

using System.IO.Packaging;
using System.IO;
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
            get
            {
                // Calling FixFileUri because BaseDirectory will be a c:\\ style path
                Uri siteOfOrigin = BaseUriHelper.FixFileUri(new Uri(System.AppDomain.CurrentDomain.BaseDirectory));
#if DEBUG
            if (_traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        Environment.CurrentManagedThreadId + 
                        ": SiteOfOriginContainer: returning site of origin " + siteOfOrigin);
#endif

                return siteOfOrigin;
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
        // None

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
#if DEBUG
            if (_traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        Environment.CurrentManagedThreadId + 
                        ": SiteOfOriginContainer: Creating SiteOfOriginPart for Uri " + uri);
#endif
            return new SiteOfOriginPart(this, uri);
        }

        #endregion


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
