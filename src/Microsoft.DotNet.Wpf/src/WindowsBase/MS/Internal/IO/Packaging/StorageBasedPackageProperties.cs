// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class provides access to the package properties of an RM-protected OPC
//  document. The "package properties" are a subset of the standard OLE property
//  sets SummaryInformation and DocumentSummaryInformation, and include such
//  properties as Title and Subject.
//
//  An RM-protected OPC document is physically represented by an OLE compound
//  file containing a well-known stream in which an OPC Zip archive, encrypted
//  in its entirety, is stored. The package properties of an RM-protected OPC
//  document are stored in the standard OLE compound file property set streams,
//  \005SummaryInformation and \005DocumentSummaryInformation. The contents of
//  these streams is intended to mirror the corresponding metadata properties
//  stored in the OPC package itself. These properties are duplicated, in the
//  clear, outside of the encrypted OPC package stream, so that tools such as
//  the Shell can display the properties without having to decrypt the package.
//
//  It is the responsibility of the application to ensure that the properties in
//  the OLE property set streams are synchronized with the properties in the
//  OPC package.
//
//
//
//
//

using System;
using System.IO;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Security; // SecurityCritical
using System.Text;      //For UTF-8 encoding.

using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.IO.Packaging.CompoundFile;
using MS.Internal.WindowsBase;  //for SecurityHelper.

// Enable presharp pragma warning suppress directives.
#pragma warning disable 1634, 1691

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// This class provides access to the package properties, such as Title and
    /// Subject, of an RM-protected OPC package. These properties are a subset of
    /// of the standard OLE property sets SummaryInformation and
    /// DocumentSummaryInformation.
    /// </summary>
    internal class StorageBasedPackageProperties : PackageProperties, IDisposable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        //
        // The constructor is internal because an application never directly
        // creates an object of this class. An application obtains an object
        // of this class through the PackageProperties CLR property of the
        // EncryptedPackageEnvelope class:
        //
        //      EncryptedPackageEnvelope ep = EncryptedPackageEnvelope.Create("mydoc.xps", ...);
        //      PackageProperties props = ep.PackageProperties;
        //
        internal
        StorageBasedPackageProperties(
            StorageRoot root
            )
        {
            _pss = (IPropertySetStorage)root.GetRootIStorage();

            //
            // Open the property sets with the same access with which the file itself
            // was opened.
            //
            _grfMode = SafeNativeCompoundFileConstants.STGM_DIRECT
                        | SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE;
            SafeNativeCompoundFileMethods.UpdateModeFlagFromFileAccess(root.OpenAccess, ref _grfMode);

            OpenPropertyStorage(ref FormatId.SummaryInformation, out _psSummInfo);
            OpenPropertyStorage(ref FormatId.DocumentSummaryInformation, out _psDocSummInfo);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~StorageBasedPackageProperties()
        {
            Dispose(false);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        #region SummaryInformation properties

        /// <value>
        /// The title.
        /// </value>
        public override string Title
        {
            get
            {
                return GetOleProperty(FormatId.SummaryInformation, PropertyId.Title) as string;
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.Title, value);
            }
        }

        /// <value>
        /// The topic of the contents.
        /// </value>
        public override string Subject
        {
            get
            {
                return GetOleProperty(FormatId.SummaryInformation, PropertyId.Subject) as string;
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.Subject, value);
            }
        }

        /// <value>
        /// The primary creator. The identification is environment-specific and
        /// can consist of a name, email address, employee ID, etc. It is
        /// recommended that this value be only as verbose as necessary to
        /// identify the individual.
        /// </value>
        public override string Creator
        {
            get
            {
                return GetOleProperty(FormatId.SummaryInformation, PropertyId.Creator) as string;
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.Creator, value);
            }
        }

        /// <value>
        /// A delimited set of keywords to support searching and indexing. This
        /// is typically a list of terms that are not available elsewhere in the
        /// properties.
        /// </value>
        public override string Keywords
        {
            get
            {
                return GetOleProperty(FormatId.SummaryInformation, PropertyId.Keywords) as string;
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.Keywords, value);
            }
        }

        /// <value>
        /// The description or abstract of the contents.
        /// </value>
        public override string Description
        {
            get
            {
                return GetOleProperty(FormatId.SummaryInformation, PropertyId.Description) as string;
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.Description, value);
            }
        }

        /// <value>
        /// The user who performed the last modification. The identification is
        /// environment-specific and can consist of a name, email address,
        /// employee ID, etc. It is recommended that this value be only as
        /// verbose as necessary to identify the individual.
        /// </value>
        public override string LastModifiedBy
        {
            get
            {
                return GetOleProperty(FormatId.SummaryInformation, PropertyId.LastModifiedBy) as string;
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.LastModifiedBy, value);
            }
        }

        /// <value>
        /// The revision number. This value indicates the number of saves or
        /// revisions. The application is responsible for updating this value
        /// after each revision.
        /// </value>
        public override string Revision
        {
            get
            {
                return GetOleProperty(FormatId.SummaryInformation, PropertyId.Revision) as string;
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.Revision, value);
            }
        }

        /// <value>
        /// The date and time of the last printing.
        /// </value>
        public override Nullable<DateTime> LastPrinted
        {
            get
            {
                return GetDateTimeProperty(FormatId.SummaryInformation, PropertyId.LastPrinted);
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.LastPrinted, value);
            }
        }

        /// <value>
        /// The creation date and time.
        /// </value>
        public override Nullable<DateTime> Created
        {
            get
            {
                return GetDateTimeProperty(FormatId.SummaryInformation, PropertyId.DateCreated);
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.DateCreated, value);
            }
        }

        /// <value>
        /// The date and time of the last modification.
        /// </value>
        public override Nullable<DateTime> Modified
        {
            get
            {
                return GetDateTimeProperty(FormatId.SummaryInformation, PropertyId.DateModified);
            }

            set
            {
                SetOleProperty(FormatId.SummaryInformation, PropertyId.DateModified, value);
            }
        }

        #endregion SummaryInformation properties

        #region DocumentSummaryInformation properties

        /// <value>
        /// The category. This value is typically used by UI applications to create navigation
        /// controls.
        /// </value>
        public override string Category
        {
            get
            {
                return GetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Category) as string;
            }

            set
            {
                SetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Category, value);
            }
        }

        /// <value>
        /// A unique identifier.
        /// </value>
        public override string Identifier
        {
            get
            {
                return GetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Identifier) as string;
            }

            set
            {
                SetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Identifier, value);
            }
        }

        /// <value>
        /// The type of content represented, generally defined by a specific
        /// use and intended audience. Example values include "Whitepaper" 
        /// and "Exam". (This property is distinct from
        /// MIME content types as defined in RFC 2045.) 
        /// </value>
        public override string ContentType
        {
            get
            {
                string contentType = GetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.ContentType) as string;
                if (contentType == null)
                {
                    return contentType;
                }
                else
                {
                    //Creating a ContentType object to validate the content type string.
                    //Can replace this later with a static method to validate the content type.
                    return new ContentType(contentType).ToString();
                }
            }

            set
            {
                if (value == null)
                {
                    //indicates that the property should be deleted.
                    SetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.ContentType, value);
                }
                else
                {
                    //Creating a ContentType object to validate the content type string.
                    //Can replace this later with a static method to validate the content type.
                    SetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.ContentType, new ContentType(value).ToString());
                }
            }
        }

        /// <value>
        /// The primary language of the package content. The language tag is
        /// composed of one or more parts: A primary language subtag and a
        /// (possibly empty) series of subsequent subtags, for example, "EN-US".
        /// These values MUST follow the convention specified in RFC 3066.
        /// </value>
        public override string Language
        {
            get
            {
                return GetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Language) as string;
            }

            set
            {
                SetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Language, value);
            }
        }

        /// <value>
        /// The version number. This value is set by the user or by the application.
        /// </value>
        public override string Version
        {
            get
            {
                return GetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Version) as string;
            }

            set
            {
                SetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.Version, value);
            }
        }

        /// <value>
        /// The status of the content. Example values include "Draft",
        /// "Reviewed", and "Final".
        /// </value>
        public override string ContentStatus
        {
            get
            {
                return GetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.ContentStatus) as string;
            }

            set
            {
                SetOleProperty(FormatId.DocumentSummaryInformation, PropertyId.ContentStatus, value);
            }
        }

        #endregion DocumentSummaryInformation properties

        //------------------------------------------------------
        //
        //  IDisposable Methods
        //
        //------------------------------------------------------

        #region IDisposable

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        ///
        /// If disposing equals false, the method has been called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing">
        /// true if called from Dispose(); false if called from the finalizer.
        /// </param>
        protected override void
        Dispose(
            bool disposing
            )
        {
            try
            {
                if (!_disposed && disposing)
                {
                    if (_psSummInfo != null)
                    {
                        try
                        {
                            ((IDisposable)_psSummInfo).Dispose();
                        }
                        finally
                        {
                            _psSummInfo = null;
                        }
                    }

                    if (_psDocSummInfo != null)
                    {
                        try
                        {
                            ((IDisposable)_psDocSummInfo).Dispose();
                        }
                        finally
                        {
                            _psDocSummInfo = null;
                        }
                    }
                }
            }
            finally
            {
                //
                // By setting _disposed = true, we ensure that all future accesses to
                // this object will fail (because both GetOleProperty and SetOleProperty
                // call CheckDisposed). Note that we wrap the entire body of
                // Dispose(bool) (this method) in an "if (!_disposed)". 
                // And we also set each reference to null immediately after attempting
                // to release it. So we never attempt to release any reference more
                // than once.
                //           
                _disposed = true;
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Obtain the specified property from the specified property set.
        /// </summary>
        /// <param name="fmtid">
        /// "Format identifier" that specifies the property set from which to
        /// obtain the property.
        /// </param>
        /// <param name="propId">
        /// Identifier of the property to obtain.
        /// </param>
        /// <returns>
        /// An object of the appropriate type for the specified property which
        /// contains the property value, or null if the property does not exist
        /// in the encrypted package.
        /// </returns>
        private object
        GetOleProperty(
            Guid fmtid,
            uint propId
            )
        {
            CheckDisposed();

            // fmtid is always either DocSum or Sum.
            IPropertyStorage ps =
                fmtid == FormatId.SummaryInformation ? _psSummInfo : _psDocSummInfo;
            if (ps == null)
            {
                // This file doesn't even contain the property storage that this
                // property belongs to, so it certainly doesn't contain the property.
                return null;
            }

            object obj = null;

            PROPSPEC[] propSpecs = new PROPSPEC[1];
            PROPVARIANT[] vals = new PROPVARIANT[1];

            propSpecs[0].propType = (uint)PropSpecType.Id;
            propSpecs[0].union.propId = propId;

            VARTYPE vtExpected = GetVtFromPropId(fmtid, propId);

            int hresult = ps.ReadMultiple(1, propSpecs, vals);

            if (hresult == SafeNativeCompoundFileConstants.S_OK)
            {
                try
                {
                    if (vals[0].vt != vtExpected)
                    {
                        throw new FileFormatException(
                                        SR.Format(
                                            SR.WrongDocumentPropertyVariantType,
                                            propId,
                                            fmtid.ToString(),
                                            vals[0].vt,
                                            vtExpected
                                            )
                                        );
                    }

                    switch (vals[0].vt)
                    {
                        case VARTYPE.VT_LPSTR:
                            //
                            // We store string properties as CP_ACP or UTF-8. 
                            // But no matter which format the string was encoded, we always use the UTF-8
                            // encoder/decoder to decode the byte array, because the UTF-8 code of an ASCII
                            // string is the same as the ASCII string.
                            //
                            IntPtr pszVal = vals[0].union.pszVal;
                            //
                            // Because both the ASCII string and UTF-8 encoded string (byte array) are
                            // stored in a memory block (pszVal) terminated by null, we can use 
                            // Marshal.PtrToStringAnsi(pszVal) to convert the memory block pointed by
                            // pszVal to a string. Then from the string.Length, we can get the number of
                            // bytes in the memory block. Otherwise, we cannot easily tell how many bytes
                            // are stored in pszVal without an extra parameter.
                            //
                            string ansiString = Marshal.PtrToStringAnsi(pszVal);
                            int nLen = ansiString.Length;

                            byte[] byteArray = new byte[nLen];
                            Marshal.Copy(pszVal, byteArray, 0, nLen);

                            obj = UTF8Encoding.UTF8.GetString(byteArray);
                            break;

                        case VARTYPE.VT_FILETIME:
                            //
                            // DateTime doesn't have a conversion from FILETIME. It has a
                            // misleadingly named "FromFileTime" method that actually wants
                            // a long. So...
                            //
                            obj = new Nullable<DateTime>(DateTime.FromFileTime(vals[0].union.hVal));
                            break;

                        default:
                            throw new FileFormatException(
                                        SR.Format(SR.InvalidDocumentPropertyVariantType, vals[0].vt));
                    }
                }
                finally
                {
#pragma warning suppress 6031 // suppressing a "by design" ignored return value
                    SafeNativeCompoundFileMethods.SafePropVariantClear(ref vals[0]);
                }
            }
            else if (hresult == SafeNativeCompoundFileConstants.S_FALSE)
            {
                // Do nothing -- return the null object reference.
            }
            else
            {
                SecurityHelper.ThrowExceptionForHR(hresult);
            }

            return obj;
        }

        /// <summary>
        /// Set or delete the specified property in the specified property set.
        /// </summary>
        /// <param name="fmtid">
        /// "Format identifier" that specifies the property set in which to
        /// set the property.
        /// </param>
        /// <param name="propId">
        /// Identifier of the property to set.
        /// </param>
        /// <param name="propVal">
        /// An object of the appropriate type for the specified property which
        /// contains the property value, or null if the property is to be deleted.
        /// </param>
        private void
        SetOleProperty(
            Guid fmtid,
            uint propId,
            object propVal
            )
        {
            CheckDisposed();

            IPropertyStorage ps =
                fmtid == FormatId.SummaryInformation ? _psSummInfo : _psDocSummInfo;

            if (ps == null)
            {
                //
                // The property set does not exist, so create it.
                //
                if (propVal != null)
                {
                    _pss.Create(
                            ref fmtid,
                            ref fmtid,
                            SafeNativeCompoundFileConstants.PROPSETFLAG_ANSI,
                            (uint)_grfMode,
                            out ps
                            );
                    if (fmtid == FormatId.SummaryInformation)
                    {
                        _psSummInfo = ps;
                    }
                    else
                    {
                        _psDocSummInfo = ps;
                    }
                }
                else
                {
                    //
                    // But if we were going to delete the property anyway, there's
                    // nothing to do.
                    //
                    return;
                }
            }

            PROPSPEC[] propSpecs = new PROPSPEC[1];
            PROPVARIANT[] vals = new PROPVARIANT[1];

            propSpecs[0].propType = (uint)PropSpecType.Id;
            propSpecs[0].union.propId = propId;

            if (propVal == null)
            {
                //
                // New value is null => remove the property. Unlike in the case of ReadMultiple,
                // we can just let this one throw an exception on failure. There are no non-zero
                // success codes to worry about.
                //
                ps.DeleteMultiple(1, propSpecs);
                return;
            }

            //
            // New value is non-null => set a new value for the property.
            //
            IntPtr pszVal = IntPtr.Zero;
            try
            {
                if (propVal is string)
                {
                    //
                    // 1) We store string properties internally as UTF-16. 
                    //    During save, convert the string (UTF-16) to CP_ACP and back
                    // 2) If property value changed during that process, store it in CF OLE Storage as UTF-8
                    // 3) Otherwise store it as CP_ACP
                    //
                    string inputString = propVal as string;

                    pszVal = Marshal.StringToCoTaskMemAnsi(inputString);
                    string convertedString = Marshal.PtrToStringAnsi(pszVal);

                    if (String.CompareOrdinal(inputString, convertedString) != 0)
                    {
                        // The string is not an ASCII string. Use UTF-8 to encode it!
                        byte[] byteArray = UTF8Encoding.UTF8.GetBytes(inputString);
                        int nLen = byteArray.Length;

                        //
                        // Before memory allocation for holding UTF-8 codes, we need to first free the memory
                        // allocated by Marshal.StringToCoTaskMemAnsi().
                        // Note that if there is any exception in this try scope, the memory will still be released
                        // by the finally of this try scope.
                        //
                        if (pszVal != IntPtr.Zero)
                        {
                            Marshal.FreeCoTaskMem(pszVal);
                            pszVal = IntPtr.Zero;
                        }

                        pszVal = Marshal.AllocCoTaskMem(checked(nLen + 1));  //The extra one byte is for the string terminator null.

                        Marshal.Copy(byteArray, 0, pszVal, nLen);
                        Marshal.WriteByte(pszVal, nLen, 0);     //Put the string terminator null at the end of the array.
                    }

                    vals[0].vt = VARTYPE.VT_LPSTR;
                    vals[0].union.pszVal = pszVal;
                }
                else if (propVal is DateTime)
                {
                    // set FileTime as an Int64 to avoid pointer operations
                    vals[0].vt = VARTYPE.VT_FILETIME;
                    vals[0].union.hVal = ((DateTime)propVal).ToFileTime();
                }
                else
                {
                    throw new ArgumentException(
                                SR.Format(SR.InvalidDocumentPropertyType, propVal.GetType().ToString()),
                                "propVal");
                }

                //
                // Again, we can just let it throw on failure; no non-zero success codes. It won't throw
                // if the property doesn't exist.
                //
                ps.WriteMultiple(1, propSpecs, vals, 0);
            }
            finally
            {
                if (pszVal != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pszVal);
                }
            }
        }

        /// <summary>
        /// Obtain the specified nullable DateTime property from the specified property set. Since nullable DateTime
        /// is a struct, the "null" case (when the property is absent) must be treated specially.
        /// </summary>
        /// <param name="fmtid">
        /// "Format identifier" that specifies the property set from which to obtain the property.
        /// </param>
        /// <param name="propId">
        /// Identifier of the property to obtain.
        /// </param>
        /// <returns>
        /// A nullable DateTime structure representing the specified property. Note that a Generic Nullable
        /// struct can be compared successfully to null.
        /// </returns>
        private Nullable<DateTime>
        GetDateTimeProperty(
            Guid fmtid,
            uint propId
            )
        {
            object obj = GetOleProperty(fmtid, propId);
            return obj != null ? (Nullable<DateTime>)obj : new Nullable<DateTime>();
        }

        private void
        OpenPropertyStorage(
            ref Guid fmtid,
            out IPropertyStorage ips
            )
        {
            int hr = _pss.Open(ref fmtid, (uint)_grfMode, out ips);

            //
            // A COM "not found" error is acceptable; it just means that the
            // file doesn't have the requested property set. Any other COM error code 
            // is an error.
            //
            if (hr == SafeNativeCompoundFileConstants.STG_E_FILENOTFOUND)
            {
                ips = null; // Just for safety; the failed call to Open should have set it to null.
            }
            else
            {
                // Throw if we failed.
                SecurityHelper.ThrowExceptionForHR(hr);
            }
        }

        private VARTYPE
        GetVtFromPropId(
            Guid fmtid,
            uint propId
            )
        {
            if (fmtid == FormatId.SummaryInformation)
            {
                switch (propId)
                {
                    case PropertyId.Title:
                    case PropertyId.Subject:
                    case PropertyId.Creator:
                    case PropertyId.Keywords:
                    case PropertyId.Description:
                    case PropertyId.LastModifiedBy:
                    case PropertyId.Revision:
                        return VARTYPE.VT_LPSTR;

                    case PropertyId.LastPrinted:
                    case PropertyId.DateCreated:
                    case PropertyId.DateModified:
                        return VARTYPE.VT_FILETIME;

                    default:
                        throw new ArgumentException(
                            SR.Format(SR.UnknownDocumentProperty, fmtid.ToString(), propId),
                            "propId"
                            );
                }
            }
            else if (fmtid == FormatId.DocumentSummaryInformation)
            {
                switch (propId)
                {
                    case PropertyId.Category:
                    case PropertyId.Identifier:
                    case PropertyId.ContentType:
                    case PropertyId.Language:
                    case PropertyId.Version:
                    case PropertyId.ContentStatus:
                        return VARTYPE.VT_LPSTR;

                    default:
                        throw new ArgumentException(
                            SR.Format(SR.UnknownDocumentProperty, fmtid.ToString(), propId),
                            "propId"
                            );
                }
            }
            else
            {
                throw new ArgumentException(
                    SR.Format(SR.UnknownDocumentProperty, fmtid.ToString(), propId),
                    "fmtid"
                    );
            }
        }

        private void
        CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, SR.StorageBasedPackagePropertiesDiposed);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _disposed;

        private int _grfMode;  // Mode in which the compound file was opened.

        //
        // Interface to the OLE property sets in the compound file representing
        // the RM-protected OPC package.
        //
        private IPropertySetStorage _pss;
        private IPropertyStorage _psSummInfo;
        private IPropertyStorage _psDocSummInfo;

        #endregion Private Fields
    }
}
