// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: PeoplePickerWrapper provides a managed wrapper around
//              the unmanaged ActiveDirectory ICommonQuery COM object.

using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.TrustUI;

using MS.Internal.PresentationUI;

namespace MS.Internal.Documents
{

    /// <summary>
    /// PeoplePickerWrapper provides a managed wrapper around
    /// the unmanaged ActiveDirectory ICommonQuery COM object
    /// </summary>
    internal partial class PeoplePickerWrapper 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors

        /// <summary>
        /// Constructs a new PeoplePickerWrapper object.
        /// </summary>
        internal PeoplePickerWrapper()
        {

        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods
        /// <summary>
        /// Invokes the "People Picker" dialog and returns a list of strings
        /// representing the email address of the users and groups selected therein.
        /// </summary>   
        /// <param name="hWndParent">The parent window for this dialog.  If null, window will
        /// be shown non-modally.</param>
        internal String[] Show(IntPtr hWndParent)
        {
            ValidateHWnd(hWndParent);

            IDataObject data = OpenQueryWindow(hWndParent);

            //If the data returned from OpenQueryWindow is null,
            //it means no data was entered in the dialog so we will
            //return null.  Otherwise we need to extract the data
            //from the returned IDataObject object.
            if (data != null)
            {                
                //Get a MemoryStream that contains the data contained in the
                //IDataObject (which is a raw form of a DsObjects struct).
                System.Windows.DataObject dataObject = new System.Windows.DataObject(data);
                System.IO.MemoryStream dsObjectStream =
                    dataObject.GetData(
                        UnsafeNativeMethods.CFSTR_DSOBJECTNAMES) as System.IO.MemoryStream;
                
                //Extract the data from that memory stream.  These will come back as
                //ActiveDirectory paths in the form 'LDAP://CN=...'                

                String[] ldapPaths = Array.Empty<String>();
               
                //Get a wrapper for the DsObjectNames object our pointer points to.
                DsObjectNamesWrapper dsObjects = new DsObjectNamesWrapper(dsObjectStream);               

                try
                {
                    //Get the names from the DsObjectNamesWrapper (these are AD paths)
                    ldapPaths = dsObjects.Names;
                }
                finally
                {
                    dsObjects.Dispose();
                    dsObjectStream.Close();    
                }

                //Get a set of e-mail addresses from the paths using AD and return them.
                return GetEmailAddressesFromPaths(ldapPaths);               
            }
            else
            {
                return Array.Empty<String>();
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        /// <summary>
        /// Instantiates an ICommonQuery COM object and invokes OpenQueryWindow on it
        /// with the necessary parameters to bring up the "people picker" portion of 
        /// the dialog.
        /// </summary>
        /// <returns></returns>  
        /// <param name="hWndParent">The HWND for the ICommonQuery.OpenQueryWindow call which
        /// defines the parent of the dialog.</param>
        private IDataObject OpenQueryWindow(IntPtr hWndParent)
        {           
            Type commonQueryType = Type.GetTypeFromCLSID(UnsafeNativeMethods.CLSID_CommonQuery);

            //Get an instance of the ICommonQuery COM object.
            UnsafeNativeMethods.ICommonQuery commonQueryInstance = Activator.CreateInstance(commonQueryType)                     
                as UnsafeNativeMethods.ICommonQuery;

            Invariant.Assert(commonQueryInstance != null, "Unable to create an instance of ICommonQuery.");

            //Set up the QueryInitParams -- this is essentially empty as we require no special flags,
            //default usernames, passwords or server information for our purposes.
            UnsafeNativeMethods.QueryInitParams queryInitParams = 
                new UnsafeNativeMethods.QueryInitParams();
            queryInitParams.cbStruct = 
                (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.QueryInitParams));
            queryInitParams.dwFlags = 0;
            queryInitParams.pDefaultScope = null;
            queryInitParams.pDefaultSaveLocation = null;
            queryInitParams.pUserName = null;
            queryInitParams.pPassword = null;
            queryInitParams.pServer = null;
            
            //Allocate memory for our QueryInitParams structure that will be used in the
            //OpenQueryWindowParams structure.
            IntPtr queryInitParamsPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(queryInitParams));

            //Now try to invoke the OpenQueryWindow method
            uint hresult = UnsafeNativeMethods.E_FAIL;
            IDataObject data = null;

            try
            {
                //Stuff the queryInitParams into a pointer (that we can assign to
                //OpenQueryWindowParams.pHandlerParameters  below).
                Marshal.StructureToPtr(queryInitParams, queryInitParamsPtr, false /*fDeleteOld*/);

                //Set up the OpenQueryWindowParams.
                //We require the default form with:
                // - The specified "Find Users" default form
                // - "OK and Cancel" buttons shown
                // - Options enabled
                // - No "Find:" dropdown (for things other than users)
                // - No menus
                // - Single item selection
                UnsafeNativeMethods.OpenQueryWindowParams openQueryWindowParams = 
                    new UnsafeNativeMethods.OpenQueryWindowParams();
                openQueryWindowParams.cbStruct = 
                    (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.OpenQueryWindowParams));
                openQueryWindowParams.dwFlags = UnsafeNativeMethods.OQWF_DEFAULTFORM | 
                                                UnsafeNativeMethods.OQWF_OKCANCEL | 
                                                UnsafeNativeMethods.OQWF_SHOWOPTIONAL |
                                                UnsafeNativeMethods.OQWF_REMOVEFORMS |
                                                UnsafeNativeMethods.OQWF_HIDEMENUS;
                openQueryWindowParams.clsidHandler = UnsafeNativeMethods.CLSID_DsQuery;
                openQueryWindowParams.pHandlerParameters = queryInitParamsPtr; 
                openQueryWindowParams.clsidDefaultForm = 
                    UnsafeNativeMethods.CLSID_DsFindPeople; //Bring up the people picker
                openQueryWindowParams.pPersistQuery = IntPtr.Zero;      //We aren't persisting this query anywhere
                openQueryWindowParams.pFormParameters = IntPtr.Zero;    //We aren't pre-populating the form

                //Invoke the OpenQueryWindow method on the ICommonQuery object,
                //which will invoke the dialog and return any entered data in the
                //"data" field.
                //OpenQueryWindow will not return until the dialog is closed.                
                hresult = commonQueryInstance.OpenQueryWindow(hWndParent,
                                            ref openQueryWindowParams,
                                            out data);
            }
            finally
            {

                //Free the memory used for our QueryInitParams structure.
                Marshal.FreeCoTaskMem(queryInitParamsPtr);
                commonQueryInstance = null;
            }
            
            if (hresult == UnsafeNativeMethods.S_OK)
            {
                //The user pressed "OK," so we can return the selected data
                return data;
            }
            else if (hresult == UnsafeNativeMethods.S_FALSE)
            {
                //The user canceled out, so we just return null.
                return null;
            }
            else
            {
                //An error condition was reported, we throw an exception with the
                //hresult included.
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                        SR.Get(SRID.PeoplePickerErrorConditionFromOpenQueryWindow), hresult));
            }
        
        }                    
        
        /// <summary>
        /// Turns a set of AD paths (in the form 'LDAP://CN=...') into a set of
        /// e-mail addresses by looking up the paths in the Directory and retrieving
        /// the 'mail' property, if it exists for the given AD object.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private String[] GetEmailAddressesFromPaths(String[] paths)
        {
            List<String> addresses = new List<String>(paths.Length);

            for (int i = 0; i < paths.Length; i++)
            {
                PropertyValueCollection emailCollection = null;
                DirectoryEntry directoryEntry = null;


                //Create a DirectoryEntry pointing to the current path;
                //Attempt to retrieve the "mail" field.
                directoryEntry = new DirectoryEntry(paths[i]);
                emailCollection =
                        directoryEntry.Properties[_adEmailAddressKey];

                if (emailCollection != null && emailCollection.Count > 0)
                {
                    //We have a non-empty e-mail collection; we will add the
                    //first available e-mail address.
                    String address = emailCollection[0] as String;

                    if (address != null)
                    {
                        addresses.Add(address);
                    }
                }
               
            }

            return addresses.ToArray();
        }

        /// <summary>
        /// Verifies that the given parent HWND is either null or an RMPublishingDialog 
        /// Windows Form.
        /// </summary>
        /// <param name="hWndParent"></param>
        /// 
        private void ValidateHWnd(IntPtr hWndParent)
        {
            if( hWndParent != IntPtr.Zero )
            {
                System.Windows.Forms.Control rmPublishingDialog = 
                    System.Windows.Forms.Control.FromHandle(hWndParent) as RMPublishingDialog;

                if (rmPublishingDialog == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.PeoplePickerInvalidParentWindow));
                }
            }            
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        //The key name for the ActiveDirectory E-Mail address property
        private const String _adEmailAddressKey = "mail";

        #region DsObjectNamesWrapper Class

        /// <summary>
        /// The DsObjectNamesWrapper class wraps a DsObjectNames Struct thus hiding
        /// the complexity of the unmanaged->managed mangling that's necessary to 
        /// retrieve the object names we need therein.
        /// 
        /// This is used in PeoplePickerWrapper.ParseDataFromHandle to simplify the code.
        /// </summary>
        private class DsObjectNamesWrapper : IDisposable
        {
            /// <summary>
            /// Static constructor for DsObjectNamesWrapper.            
            /// </summary>
            static DsObjectNamesWrapper()
            {
                // Do not remove! (See above Security comment)
            }

            /// <summary>
            /// Constructs a new DsObjectNamesWrapper given a MemoryStream containing
            /// a raw representation of a DsObjectNames object.
            /// </summary>
            /// <param name="ptrToDsObjectNames">A pointer to a valid DsObjectNames struct</param>
            internal DsObjectNamesWrapper(System.IO.MemoryStream dataStream)
            {
                if (dataStream == null)
                {
                    throw new ArgumentNullException("dataStream");
                }

                //We need to get a pointer to this data for our DsObjectNamesWrapper
                //to wrap.
                //First we convert the stream to an array of bytes.
                byte[] data = dataStream.ToArray();
                               
                //Allocate memory to store an unmanaged copy of this data
                _ptrToDsObjectNames = Marshal.AllocHGlobal(data.Length);
                Invariant.Assert(_ptrToDsObjectNames != IntPtr.Zero, "Invalid pointer to DsObjectNames data.");

                //Marshal the data over to the unmanaged side
                Marshal.Copy(data, 0, _ptrToDsObjectNames, data.Length);

                //Get a DsObjectNames structure out of the pointer we
                //were handed.
                _dsObjectNames =
                    (UnsafeNativeMethods.DsObjectNames)Marshal.PtrToStructure(
                        _ptrToDsObjectNames, typeof(UnsafeNativeMethods.DsObjectNames)); 
                                                 
            }

            /// <summary>
            /// Finalizer for DsObjectNamesWrapper, ensures that unmanaged resources are properly
            /// cleaned up.          
            /// </summary>
            ~DsObjectNamesWrapper()
            {
                this.Dispose();
            }

            /// <summary>
            /// The number of names in the DsObjectNames struct this class is wrapping.
            /// </summary>            
            internal uint Count
            {
                get
                {
                    ThrowIfDisposed();
                    return _dsObjectNames.cItems;
                }
            }

            /// <summary>
            /// An array of names contained within the DsObjectNames struct.
            /// </summary>            
            internal String[] Names
            {
                get
                {
                    ThrowIfDisposed();

                    if (_names == null)
                    {
                        _names = GetNamesFromDsObjectStruct();
                    }

                    return _names;
                }
            }

            /// <summary>
            /// Gets the list of names from the struct.
            /// </summary>
            /// </summary>            
            /// <returns></returns>
            private String[] GetNamesFromDsObjectStruct()
            {
                ThrowIfDisposed();

                String[] names = new string[Count];

                for (int i = 0; i < Count; i++)
                {
                    names[i] = BuildStringForItemName(i);
                }

                return names;
            }

            /// <summary>
            /// Extracts the name for the specified entry number from the DsObjectNames data
            /// </summary>            
            /// <param name="index">The index of the name to retrieve</param>
            /// <returns></returns> 
            private String BuildStringForItemName(int index)
            {
                ThrowIfDisposed();

                //Ensure we're within proper bounds.
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                //First we have to get a DsObject out of the array (aObjects) of DsObjects.
                //The information in this struct will tell us where to find the name data
                //we want.
                UnsafeNativeMethods.DsObject dsObject = GetDsObjectForIndex(index);

                //The offset of the name from the start of the DsObjectNames structure is stored in
                //the corresponding DsObject's offsetName field, so the pointer to the beginning of the
                //string can be computed as:
                //
                // offset = StartOfStructAddress + dsObject.offsetName;                
                IntPtr nameOffset = new IntPtr(_ptrToDsObjectNames.ToInt64() + dsObject.offsetName);

                //We marshal this pointer to a string and return it.
                return Marshal.PtrToStringAuto(nameOffset);
            }

            /// <summary>
            /// Retrieves a DsObject structure from the "array" in our DsObjectNames structure.
            /// Because marshalling cannot properly marshal a nested, variable sized 
            /// array in a struct, we have to do this the hard way.
            /// That is, the "array" of DsObjects that's in our managed DsObjectNames structure  
            /// definition is merely a pointer to a big chunk of data that we have to manually
            /// retrieve DsObjects from by manipulating pointers, rather than doing aObjects[i].
            /// </summary>            
            /// <param name="index">The index of the DsObject to retrieve</param>
            /// <returns></returns>
            private UnsafeNativeMethods.DsObject GetDsObjectForIndex(int index)
            {
                ThrowIfDisposed();

                //Ensure we're within proper bounds.
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                //Now we calculate the offset of the specified array index.
                //This is the address of the first element in the array, plus
                //the number of entries past that times the size of an entry, or:
                //offset = StartOfStructAddress + FirstArrayEntryOffset + index * SizeOfEntry

                IntPtr offset = new IntPtr(_ptrToDsObjectNames.ToInt64() + 
                    _dsObjectArrayFieldOffset + 
                    index * _sizeOfDsObject);

                //Marshal that to a DsObject structure.
                UnsafeNativeMethods.DsObject dsObject =
                        (UnsafeNativeMethods.DsObject)Marshal.PtrToStructure(offset, 
                            typeof(UnsafeNativeMethods.DsObject));

                return dsObject;

            }

            /// <summary>
            /// Implemented to deal with FxCop rule UseSafeHandleToEncapsulateNativeResources
            /// We can now assert that the code will not allow methods to be called after
            /// we are disposed.
            /// </summary>
            private void ThrowIfDisposed()
            {
                if (_isDisposed) throw new ObjectDisposedException("DsObjectNamesWrapper");
            }

            /// <summary>
            /// Cleans up the unmanaged memory allocations made by this object.
            /// </summary>            
            public void Dispose()
            {
                lock (this)
                {
                    if (!_isDisposed)
                    {
                        Marshal.FreeHGlobal(_ptrToDsObjectNames);
                        _isDisposed = true;
                        GC.SuppressFinalize(this);
                    }
                }
            }

            #region Private Fields
            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            /// <summary>
            /// Pointer to the DsObjectNames struct we're wrapping.
            /// We use this to calculate pointers to field offsets.
            /// </summary>
            private IntPtr                  _ptrToDsObjectNames;

            /// <summary>
            /// The list of names in the DsObjectNames struct 
            /// </summary>
            private String[]                _names;

            /// <summary>
            /// The DsObjectNames struct we're wrapping.
            /// </summary>
            private UnsafeNativeMethods.DsObjectNames _dsObjectNames;

            private bool _isDisposed;

            // Useful constants:   

            /// <summary>
            /// The offset from the start of a DsObjectNames structure to the
            /// DsObjects array.
            /// </summary>
            private static readonly int _dsObjectArrayFieldOffset = 
                Marshal.SizeOf(typeof(Guid)) + Marshal.SizeOf(typeof(UInt32));

            /// <summary>
            /// The size of a DsObject.
            /// </summary>
            private static readonly int _sizeOfDsObject = 
                Marshal.SizeOf(typeof(UnsafeNativeMethods.DsObject));

            #endregion Private Fields

        }

        #endregion DsObjectNamesWrapper Class            

        #endregion Private Fields
    }
}
