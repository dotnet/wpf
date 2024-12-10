// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System.Collections;
using System.Collections.ObjectModel;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using MS.Internal.PresentationCore;
using Windows.Win32.Foundation;                        // SecurityHelper

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Media.Imaging
{
    #region BitmapMetadata

    /// <summary>
    /// Metadata Class for BitmapImage.
    /// </summary>
    public partial class BitmapMetadata : ImageMetadata, IEnumerable, IEnumerable<String>
    {
        //*************************************************************
        //
        //  IWICMetadataBlockReader
        //
        //*************************************************************

        // Guid: IID_IWICMetadataBlockReader
        [ComImport(),
         Guid("FEAA2A8D-B3F3-43E4-B25C-D1DE990A1AE1"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICMetadataBlockReader
        {
            [PreserveSig]
            HRESULT GetContainerFormat(
                out Guid containerFormat
            );

            [PreserveSig]
            HRESULT GetCount(
                out UInt32 count
            );

            [PreserveSig]
            HRESULT GetReaderByIndex(
                UInt32 index,
                out IntPtr /* IWICMetadataReader */ ppIMetadataReader
            );

            [PreserveSig]
            HRESULT GetEnumerator(
                out IntPtr /* IEnumUnknown */ pIEnumMetadata
            );
        }

        //*************************************************************
        //
        //  IWICMetadataBlockWriter
        //
        //*************************************************************

        // Guid: IID_IWICMetadataBlockWriter
        [ComImport(),
             Guid("08FB9676-B444-41E8-8DBE-6A53A542BFF1"),
             InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICMetadataBlockWriter : IWICMetadataBlockReader
        {
            [PreserveSig]
            new HRESULT GetContainerFormat(
                out Guid containerFormat
            );

            [PreserveSig]
            new HRESULT GetCount(
                out UInt32 count
            );

            [PreserveSig]
            new HRESULT GetReaderByIndex(
                UInt32 index,
                out IntPtr /* IWICMetadataReader */ ppIMetadataReader
            );

            [PreserveSig]
            new HRESULT GetEnumerator(
                out IntPtr /* IEnumUnknown */ pIEnumMetadata
            );

            [PreserveSig]
            HRESULT InitializeFromBlockReader(
                IntPtr /* IWICMetadataBlockReader */ pIBlockReader
            );

            [PreserveSig]
            HRESULT GetWriterByIndex(
                UInt32 index,
                out IntPtr /* IWICMetadataWriter */ ppIMetadataWriter
            );

            [PreserveSig]
            HRESULT AddWriter(
                IntPtr /* IWICMetadataWriter */ pIMetadataWriter
            );

            [PreserveSig]
            HRESULT SetWriterByIndex(
                UInt32 index,
                IntPtr /* IWICMetadataWriter */ pIMetadataWriter
            );

            [PreserveSig]
            HRESULT RemoveWriterByIndex(
                UInt32 index
            );
        }

        //*************************************************************
        //
        //  BitmapMetadataBlockWriter
        //
        //*************************************************************

        [ClassInterface(ClassInterfaceType.None)]
        internal sealed class BitmapMetadataBlockWriter :
            IWICMetadataBlockWriter,
            IWICMetadataBlockReader
        {
            internal BitmapMetadataBlockWriter(Guid containerFormat, bool fixedSize)
            {
                _fixedSize = fixedSize;
                _containerFormat = containerFormat;
                _metadataBlocks = new ArrayList();
            }

            internal BitmapMetadataBlockWriter(BitmapMetadataBlockWriter blockWriter, object syncObject)
            {
                Guid guidVendor = new Guid(MILGuidData.GUID_VendorMicrosoft);

                _fixedSize = blockWriter._fixedSize;
                _containerFormat = blockWriter._containerFormat;
                _metadataBlocks = new ArrayList();

                ArrayList metadataBlocks = blockWriter.MetadataBlocks;

                using (FactoryMaker factoryMaker = new FactoryMaker())
                {
                    foreach (SafeMILHandle metadataHandle in metadataBlocks)
                    {
                        lock (syncObject)
                        {
                            IntPtr pIMetadataWriter = IntPtr.Zero;

                            try
                            {
                                UnsafeNativeMethods.WICComponentFactory.CreateMetadataWriterFromReader(
                                    factoryMaker.ImagingFactoryPtr,
                                    metadataHandle,
                                    ref guidVendor,
                                    out pIMetadataWriter).ThrowOnFailureExtended();

                                SafeMILHandle metadataWriter = new SafeMILHandle(pIMetadataWriter);
                                pIMetadataWriter = IntPtr.Zero;

                                _metadataBlocks.Add(metadataWriter);
                            }
                            finally
                            {
                                if (pIMetadataWriter != IntPtr.Zero)
                                {
                                    #pragma warning suppress 6031 // Return value ignored on purpose.
                                    UnsafeNativeMethods.MILUnknown.Release(pIMetadataWriter);
                                }
                            }
                        }
                    }
                }
            }

            public HRESULT GetContainerFormat(out Guid containerFormat)
            {
                containerFormat = _containerFormat;
                return HRESULT.S_OK;
            }

            public HRESULT GetCount(out uint count)
            {
                count = (uint)_metadataBlocks.Count;
                return HRESULT.S_OK;
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT GetReaderByIndex(
                UInt32 index,
                out IntPtr /* IWICMetadataReader */ pIMetadataReader
            )
            {
                if (index >= _metadataBlocks.Count)
                {
                    pIMetadataReader = IntPtr.Zero;
                    return HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                SafeMILHandle metadataReader = (SafeMILHandle) _metadataBlocks[(int)index];

                Guid wicMetadataReader = MILGuidData.IID_IWICMetadataReader;
                return UnsafeNativeMethods.MILUnknown.QueryInterface(
                    metadataReader,
                    ref wicMetadataReader,
                    out pIMetadataReader);
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT GetEnumerator(
                out IntPtr /* IEnumUnknown */ pIEnumMetadata
            )
            {
                BitmapMetadataBlockWriterEnumerator blockEnumerator;

                blockEnumerator = new BitmapMetadataBlockWriterEnumerator(this);

                pIEnumMetadata = Marshal.GetComInterfaceForObject(
                    blockEnumerator,
                    typeof(IEnumUnknown));

                return HRESULT.S_OK;
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT InitializeFromBlockReader(
                IntPtr /* IWICMetadataBlockReader */ pIBlockReader
            )
            {
                Invariant.Assert(pIBlockReader != IntPtr.Zero);

                Guid guidVendor = new Guid(MILGuidData.GUID_VendorMicrosoft);

                ArrayList metadataBlocks = new ArrayList();

                HRESULT result = UnsafeNativeMethods.WICMetadataBlockReader.GetCount(
                    pIBlockReader,
                    out uint count);

                if (result.Succeeded)
                {
                    using (FactoryMaker factoryMaker = new FactoryMaker())
                    {
                        for (UInt32 i=0; i<count; i++)
                        {
                            SafeMILHandle pIMetadataReader = null;
                            IntPtr pIMetadataWriter = IntPtr.Zero;

                            try
                            {
                                result = UnsafeNativeMethods.WICMetadataBlockReader.GetReaderByIndex(
                                    pIBlockReader,
                                    i,
                                    out pIMetadataReader);

                                if (result.Failed)
                                {
                                    break;
                                }

                                result = UnsafeNativeMethods.WICComponentFactory.CreateMetadataWriterFromReader(
                                    factoryMaker.ImagingFactoryPtr,
                                    pIMetadataReader,
                                    ref guidVendor,
                                    out pIMetadataWriter);

                                if (result.Failed)
                                {
                                    break;
                                }

                                // pIMetadataWriter already has an active reference, give it to the safe handle.
                                SafeMILHandle metadataWriter = new SafeMILHandle(pIMetadataWriter);
                                pIMetadataWriter = IntPtr.Zero;

                                metadataBlocks.Add(metadataWriter);
                            }
                            finally
                            {
                                if (pIMetadataReader != null)
                                {
                                    pIMetadataReader.Dispose();
                                }
                                if (pIMetadataWriter != IntPtr.Zero)
                                {
                                    // Return value ignored on purpose.
                                    _ = UnsafeNativeMethods.MILUnknown.Release(pIMetadataWriter);
                                }
                            }
                        }
                    }

                    _metadataBlocks = metadataBlocks;
                }

                return result;
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT GetWriterByIndex(
                UInt32 index,
                out IntPtr  /* IWICMetadataWriter */ pIMetadataWriter
            )
            {
                if (index >= _metadataBlocks.Count)
                {
                    pIMetadataWriter = IntPtr.Zero;
                    return HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                SafeMILHandle metadataWriter = (SafeMILHandle) _metadataBlocks[(int)index];

                Guid wicMetadataWriter = MILGuidData.IID_IWICMetadataWriter;
                return UnsafeNativeMethods.MILUnknown.QueryInterface(
                    metadataWriter,
                    ref wicMetadataWriter,
                    out pIMetadataWriter);
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT AddWriter(
                IntPtr /* IWICMetadataWriter */ pIMetadataWriter
            )
            {
                if (pIMetadataWriter == IntPtr.Zero)
                {
                    return HRESULT.FromWin32(WIN32_ERROR.ERROR_INVALID_PARAMETER);
                }

                if (_fixedSize && _metadataBlocks.Count>0)
                {
                    return HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                SafeMILHandle metadataWriter = new SafeMILHandle(pIMetadataWriter);

                UnsafeNativeMethods.MILUnknown.AddRef(metadataWriter);

                _metadataBlocks.Add(metadataWriter);

                return HRESULT.S_OK;
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT SetWriterByIndex(
                UInt32 index,
                IntPtr /* IWICMetadataWriter */ pIMetadataWriter
            )
            {
                if (index >= _metadataBlocks.Count)
                {
                    return HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                if (pIMetadataWriter == IntPtr.Zero)
                {
                    return HRESULT.FromWin32(WIN32_ERROR.ERROR_INVALID_PARAMETER);
                }

                if (_fixedSize)
                {
                    return HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                SafeMILHandle metadataWriter = new SafeMILHandle(pIMetadataWriter);

                UnsafeNativeMethods.MILUnknown.AddRef(metadataWriter);

                _metadataBlocks[(int)index] = metadataWriter;

                return HRESULT.S_OK;
            }

            public HRESULT RemoveWriterByIndex(uint index)
            {
                if (index >= _metadataBlocks.Count)
                {
                    return HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                if (_fixedSize)
                {
                    return HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                _metadataBlocks.Remove(index);

                return HRESULT.S_OK;
            }

            internal ArrayList MetadataBlocks
            {
                get
                {
                    return _metadataBlocks;
                }
            }

            private bool _fixedSize;
            private Guid _containerFormat;
            private ArrayList _metadataBlocks;
        }

        //*************************************************************
        //
        //  IEnumUnknown
        //
        //*************************************************************

        // Guid: IID_IEnumUnknown
        [ComImport(),
         Guid("00000100-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IEnumUnknown
        {
            [PreserveSig]
            HRESULT Next(
                UInt32 celt,
                out IntPtr /* IUnknown ** */ rgelt,
                ref UInt32 pceltFetched
            );

            [PreserveSig]
            HRESULT Skip(
                UInt32 celt
            );

            [PreserveSig]
            HRESULT Reset();

            [PreserveSig]
            HRESULT Clone(
                ref IntPtr /* IEnumString ** */ ppEnum
            );
        }

        //*************************************************************
        //
        //  BitmapMetadataBlockWriterEnumerator
        //
        //*************************************************************

        [ClassInterface(ClassInterfaceType.None)]
        internal sealed class BitmapMetadataBlockWriterEnumerator : IEnumUnknown
        {
            internal BitmapMetadataBlockWriterEnumerator(
                BitmapMetadataBlockWriter blockWriter
            )
            {
                Debug.Assert(blockWriter != null);
                Debug.Assert(blockWriter.MetadataBlocks != null);

                _metadataBlocks = blockWriter.MetadataBlocks;
                _index = 0;
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT Next(
                UInt32 celt,
                out IntPtr /* IUnknown ** */ rgelt,
                ref UInt32 pceltFetched
            )
            {
                // This implementation only supports single enumeration.
                if (celt > 1)
                {
                    rgelt = IntPtr.Zero;
                    pceltFetched = 0;
                    return HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                if (_index >= _metadataBlocks.Count || celt == 0)
                {
                    rgelt = IntPtr.Zero;
                    pceltFetched = 0;
                    return HRESULT.S_FALSE;
                }
                else
                {
                    SafeMILHandle metadataHandle = (SafeMILHandle)_metadataBlocks[(int)_index];

                    Guid wicMetadataReader = MILGuidData.IID_IWICMetadataReader;
                    HRESULT result = UnsafeNativeMethods.MILUnknown.QueryInterface(
                        metadataHandle,
                        ref wicMetadataReader,
                        out rgelt);

                    if (result.Succeeded)
                    {
                        pceltFetched = 1;
                        _index++;
                    }

                    return result;
                }
            }

            public HRESULT Skip(uint celt)
            {
                uint newIndex = _index + celt;

                if (newIndex > _metadataBlocks.Count)
                {
                    _index = (uint)_metadataBlocks.Count;
                    return HRESULT.S_FALSE;
                }
                else
                {
                    _index += celt;
                    return HRESULT.S_OK;
                }
            }

            public HRESULT Reset()
            {
                _index = 0;
                return HRESULT.S_OK;
            }

            /// <summary>
            /// This method is part of an interface that is only called by way of WindowsCodec.  It is not
            /// publicly exposed or accessible in any way.
            /// </summary>
            public HRESULT Clone(ref IntPtr /* IEnumUnknown ** */ ppEnum)
            {
                ppEnum = IntPtr.Zero;
                return HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION;
            }

            private ArrayList _metadataBlocks;
            private UInt32 _index;
        }

        #region Constructors

        public BitmapMetadata(String containerFormat)
        {
            ArgumentNullException.ThrowIfNull(containerFormat);

            Guid guid = new Guid();

            // Find the length of the string needed
            UnsafeNativeMethods.WICCodec.WICMapShortNameToGuid(containerFormat, ref guid).ThrowOnFailureExtended();

            Init(guid, false, false);
        }

        /// <summary>
        ///
        /// </summary>
        internal BitmapMetadata()
        {
            _metadataHandle = null;
            _readOnly = true;
            _fixedSize = false;
            _blockWriter = null;
        }

        /// <summary>
        ///
        /// </summary>
        internal BitmapMetadata(
            SafeMILHandle metadataHandle,
            bool readOnly,
            bool fixedSize,
            object syncObject
        )
        {
            _metadataHandle = metadataHandle;
            _readOnly = readOnly;
            _fixedSize = fixedSize;
            _blockWriter = null;
            _syncObject = syncObject;
        }

        /// <summary>
        ///
        /// </summary>
        internal BitmapMetadata(BitmapMetadata bitmapMetadata)
        {
            ArgumentNullException.ThrowIfNull(bitmapMetadata);

            Init(bitmapMetadata.GuidFormat, false, bitmapMetadata._fixedSize);
        }

        /// <summary>
        ///
        /// </summary>
        private void Init(Guid containerFormat, bool readOnly, bool fixedSize)
        {
            HRESULT result = HRESULT.S_OK;
            IntPtr /* IWICMetadataQueryWriter */ queryWriter = IntPtr.Zero;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                Guid vendorMicrosoft = new Guid(MILGuidData.GUID_VendorMicrosoft);

                // If it's a metadata format, create a Query Writer to wrap it.
                result = UnsafeNativeMethods.WICImagingFactory.CreateQueryWriter(
                    factoryMaker.ImagingFactoryPtr,
                    ref containerFormat,
                    ref vendorMicrosoft,
                    out queryWriter
                    );
            }

            if (result.Succeeded)
            {
                _readOnly = readOnly;
                _fixedSize = fixedSize;
                _blockWriter = null;

                _metadataHandle = new SafeMILHandle(queryWriter);
                _syncObject = _metadataHandle;
            }
            else
            {
                InitializeFromBlockWriter(containerFormat, readOnly, fixedSize);
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void InitializeFromBlockWriter(Guid containerFormat, bool readOnly, bool fixedSize)
        {
            IntPtr /* IWICMetadataBlockWriter */ blockWriter = IntPtr.Zero;
            IntPtr /* IWICMetadataQueryWriter */ queryWriter = IntPtr.Zero;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    // Otherwise, simulate a metadata block writer for this imaging container format.
                    _blockWriter = new BitmapMetadataBlockWriter(containerFormat, fixedSize);

                    blockWriter = Marshal.GetComInterfaceForObject(
                        _blockWriter,
                        typeof(IWICMetadataBlockWriter));

                    UnsafeNativeMethods.WICComponentFactory.CreateQueryWriterFromBlockWriter(
                        factoryMaker.ImagingFactoryPtr,
                        blockWriter,
                        ref queryWriter).ThrowOnFailureExtended();

                    _readOnly = readOnly;
                    _fixedSize = fixedSize;

                    _metadataHandle = new SafeMILHandle(queryWriter);
                    queryWriter = IntPtr.Zero;

                    _syncObject = _metadataHandle;
                }
                finally
                {
                    if (blockWriter != IntPtr.Zero)
                    {
                        #pragma warning suppress 6031 // Return value ignored on purpose.
                        UnsafeNativeMethods.MILUnknown.Release(blockWriter);
                    }
                    if (queryWriter != IntPtr.Zero)
                    {
                        #pragma warning suppress 6031 // Return value ignored on purpose.
                        UnsafeNativeMethods.MILUnknown.Release(queryWriter);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void InitializeFromBlockWriter(BitmapMetadataBlockWriter sourceBlockWriter, object syncObject)
        {
            IntPtr /* IWICMetadataBlockWriter */ blockWriter = IntPtr.Zero;
            IntPtr /* IWICMetadataQueryWriter */ queryWriter = IntPtr.Zero;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    // Otherwise, simulate a metadata block writer for this imaging container format.
                    _blockWriter = new BitmapMetadataBlockWriter(sourceBlockWriter, syncObject);

                    blockWriter = Marshal.GetComInterfaceForObject(
                        _blockWriter,
                        typeof(IWICMetadataBlockWriter));

                    UnsafeNativeMethods.WICComponentFactory.CreateQueryWriterFromBlockWriter(
                        factoryMaker.ImagingFactoryPtr,
                        blockWriter,
                        ref queryWriter).ThrowOnFailureExtended();

                    _readOnly = false;
                    _fixedSize = false;

                    _metadataHandle = new SafeMILHandle(queryWriter);
                    queryWriter = IntPtr.Zero;

                    _syncObject = _metadataHandle;
                }
                finally
                {
                    if (blockWriter != IntPtr.Zero)
                    {
                        #pragma warning suppress 6031 // Return value ignored on purpose.
                        UnsafeNativeMethods.MILUnknown.Release(blockWriter);
                    }
                    if (queryWriter != IntPtr.Zero)
                    {
                        #pragma warning suppress 6031 // Return value ignored on purpose.
                        UnsafeNativeMethods.MILUnknown.Release(queryWriter);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void InitializeFromMetadataWriter(SafeMILHandle metadataHandle, object syncObject)
        {
            HRESULT result;
            IntPtr queryWriter = IntPtr.Zero;

            Guid guidVendor = new Guid(MILGuidData.GUID_VendorMicrosoft);

            // Create a query writer for this metadata format
            try
            {
                using (FactoryMaker factoryMaker = new FactoryMaker())
                {
                    lock (syncObject)
                    {
                        result = UnsafeNativeMethods.WICImagingFactory.CreateQueryWriterFromReader(
                            factoryMaker.ImagingFactoryPtr,
                            metadataHandle,
                            ref guidVendor,
                            out queryWriter);
                    }
                }

                if (result.Succeeded)
                {
                    _readOnly = false;
                    _fixedSize = false;
                    _blockWriter = null;

                    _metadataHandle = new SafeMILHandle(queryWriter);
                    queryWriter = IntPtr.Zero;

                    _syncObject = _metadataHandle;
                }
                else if (!result.IsWindowsCodecError())
                {
                    result.ThrowOnFailureExtended();
                }
            }
            finally
            {
                if (queryWriter != IntPtr.Zero)
                {
                    // Return value ignored on purpose.
                    _ = UnsafeNativeMethods.MILUnknown.Release(queryWriter);
                }
            }
        }

        #endregion

        #region Freezable

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapMetadata Clone()
        {
            return (BitmapMetadata)base.Clone();
        }

        /// <summary>
        /// Implementation of <see cref="Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new BitmapMetadata();
        }

        /// <summary>
        /// Implementation of <see cref="Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata) sourceFreezable;
            base.CloneCore(sourceFreezable);

            CopyCommon(sourceBitmapMetadata);
        }

        /// <summary>
        /// Implementation of <see cref="Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceBitmapMetadata);
        }


        /// <summary>
        /// Implementation of <see cref="Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmapMetadata);
        }


        /// <summary>
        /// Implementation of <see cref="Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmapMetadata);
        }

        #endregion

        public String Format
        {
            get
            {
                EnsureBitmapMetadata();
                StringBuilder format = null;
                uint length = 0;

                // This calls EnsureBitmapMetadata()
                Guid guid = GuidFormat;

                // Find the length of the string needed
                lock (_syncObject)
                {
                    UnsafeNativeMethods.WICCodec.WICMapGuidToShortName(
                        ref guid,
                        0,
                        format,
                        ref length).ThrowOnFailureExtended();

                    // Get the string back
                    if (length > 0)
                    {
                        format = new StringBuilder((int)length);

                        UnsafeNativeMethods.WICCodec.WICMapGuidToShortName(
                            ref guid,
                            length,
                            format,
                            ref length).ThrowOnFailureExtended();
                    }
                }

                return format.ToString();
            }
        }

        public String Location
        {
            get
            {
                StringBuilder location = null;

                EnsureBitmapMetadata();

                // Find the length of the string needed
                lock (_syncObject)
                {
                    UnsafeNativeMethods.WICMetadataQueryReader.GetLocation(
                        _metadataHandle,
                        0,
                        location,
                        out uint length).ThrowOnFailureExtended();

                    // Get the string back
                    if (length > 0)
                    {
                        location = new StringBuilder((int)length);

                        UnsafeNativeMethods.WICMetadataQueryReader.GetLocation(
                            _metadataHandle,
                            length,
                            location,
                            out length).ThrowOnFailureExtended();
                    }
                }

                return location.ToString();
            }
        }

        #region Properties

        public bool IsReadOnly
        {
            get
            {
                EnsureBitmapMetadata();

                return _readOnly;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                EnsureBitmapMetadata();

                return _fixedSize;
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        ///
        /// </summary>
        public void SetQuery(String query, object value)
        {
            WritePreamble();

            ArgumentNullException.ThrowIfNull(query);

            ArgumentNullException.ThrowIfNull(value);

            if (_readOnly)
            {
                throw new InvalidOperationException(SR.Image_MetadataReadOnly);
            }

            // Store these for debugging stress failures.
            _setQueryString = query;
            _setQueryValue = value;

            EnsureBitmapMetadata();

            PROPVARIANT propVar = new PROPVARIANT();

            try
            {
                propVar.Init(value);

                if (propVar.RequiresSyncObject)
                {
                    BitmapMetadata metadata = value as BitmapMetadata;
                    Invariant.Assert(metadata != null);

                    #pragma warning suppress 6506 // Invariant.Assert(metadata != null);
                    metadata.VerifyAccess();

                    #pragma warning suppress 6506 // Invariant.Assert(metadata != null);
                    lock (metadata._syncObject)
                    {
                        lock (_syncObject)
                        {
                            UnsafeNativeMethods.WICMetadataQueryWriter.SetMetadataByName(
                                _metadataHandle,
                                query,
                                ref propVar).ThrowOnFailureExtended();
                        }
                    }
                }
                else
                {
                    lock (_syncObject)
                    {
                        UnsafeNativeMethods.WICMetadataQueryWriter.SetMetadataByName(
                            _metadataHandle,
                            query,
                            ref propVar).ThrowOnFailureExtended();
                    }
                }
            }
            finally
            {
                propVar.Clear();
            }

            WritePostscript();
        }

        public object GetQuery(String query)
        {
            HRESULT result;

            ArgumentNullException.ThrowIfNull(query);

            EnsureBitmapMetadata();

            PROPVARIANT propVar = new PROPVARIANT();

            try
            {
                propVar.Init(null);

                lock (_syncObject)
                {
                    result = UnsafeNativeMethods.WICMetadataQueryReader.GetMetadataByName(
                        _metadataHandle,
                        query,
                        ref propVar);
                }

                if (result != HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND)
                {
                    result.ThrowOnFailureExtended();

                    object objValue = propVar.ToObject(_syncObject);

                    if (IsFrozenInternal)
                    {
                        BitmapMetadata metadata = objValue as BitmapMetadata;

                        if (metadata != null)
                        {
                            metadata.Freeze();
                        }
                    }

                    return objValue;
                }
            }
            finally
            {
                propVar.Clear();
            }

            return null;
        }

        public void RemoveQuery(String query)
        {
            HRESULT result;

            WritePreamble();

            ArgumentNullException.ThrowIfNull(query);

            if (_readOnly)
            {
                throw new InvalidOperationException(SR.Image_MetadataReadOnly);
            }

            EnsureBitmapMetadata();

            lock (_syncObject)
            {
                result = UnsafeNativeMethods.WICMetadataQueryWriter.RemoveMetadataByName(
                    _metadataHandle,
                    query);
            }

            if (result != HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND)
            {
                result.ThrowOnFailureExtended();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureBitmapMetadata();
            return new BitmapMetadataEnumerator(_metadataHandle);
        }

        IEnumerator<String> IEnumerable<String>.GetEnumerator()
        {
            EnsureBitmapMetadata();
            return new BitmapMetadataEnumerator(_metadataHandle);
        }

        public bool ContainsQuery(String query)
        {
            HRESULT result;

            ArgumentNullException.ThrowIfNull(query);

            EnsureBitmapMetadata();

            lock (_syncObject)
            {
                result = UnsafeNativeMethods.WICMetadataQueryReader.ContainsMetadataByName(
                    _metadataHandle,
                    query,
                    IntPtr.Zero
                    );
            }

            if (result.IsWindowsCodecError())
            {
                return false;
            }
            else
            {
                result.ThrowOnFailureExtended();
            }

            return true;
        }

        #endregion

        #region Policy-driven Properties

        /// <summary>
        /// Access Author for the image
        /// </summary>
        public ReadOnlyCollection<String> Author
        {
            get
            {
                String[] strAuthors = GetQuery(policy_Author) as String[];

                return (strAuthors == null) ?
                       null
                     : new ReadOnlyCollection<String>(strAuthors);
            }
            set
            {
                String[] strAuthors = null;
                if (value != null)
                {
                    strAuthors = new String[value.Count];
                    value.CopyTo(strAuthors, 0);
                }

                SetQuery(policy_Author, strAuthors);
            }
        }

        /// <summary>
        /// Access Title for the image
        /// </summary>
        public String Title
        {
            get
            {
                return GetQuery(policy_Title) as String;
            }
            set
            {
                SetQuery(policy_Title, value);
            }
        }

        /// <summary>
        /// Access Rating for the image
        /// </summary>
        public int Rating
        {
            get
            {
                object rating = GetQuery(policy_Rating);

                if (rating != null && rating.GetType() == typeof(ushort))
                {
                    return Convert.ToInt32(rating, CultureInfo.InvariantCulture);
                }

                return 0;
            }
            set
            {
                SetQuery(policy_Rating, Convert.ToUInt16(value, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Access Subject for the image
        /// </summary>
        public String Subject
        {
            get
            {
                return GetQuery(policy_Subject) as String;
            }
            set
            {
                SetQuery(policy_Subject, value);
            }
        }

        /// <summary>
        /// Access Comment for the image
        /// </summary>
        public String Comment
        {
            get
            {
                return GetQuery(policy_Comment) as String;
            }
            set
            {
                SetQuery(policy_Comment, value);
            }
        }

        /// <summary>
        /// Access Date Taken for the image
        /// </summary>
        public String DateTaken
        {
            get
            {
                object fileTime = GetQuery(policy_DateTaken);
                if (fileTime != null && fileTime.GetType() == typeof(Runtime.InteropServices.ComTypes.FILETIME))
                {
                    Runtime.InteropServices.ComTypes.FILETIME time = (Runtime.InteropServices.ComTypes.FILETIME)fileTime;
                    DateTime dateTime = DateTime.FromFileTime( (((long)time.dwHighDateTime) << 32) + (uint)time.dwLowDateTime );
                    return dateTime.ToString();
                }
                return null;
            }
            set
            {
                DateTime dt = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                PROPVARIANT propVar= new PROPVARIANT();

                propVar.varType = (ushort)VarEnum.VT_FILETIME;
                long longFileTime = dt.ToFileTime();
                propVar.filetime.dwLowDateTime = (Int32)longFileTime;
                propVar.filetime.dwHighDateTime = (Int32)((longFileTime >> 32) & 0xFFFFFFFF);

                object objValue = propVar.ToObject(_syncObject);

                SetQuery(policy_DateTaken, objValue);
            }
        }

        /// <summary>
        /// Access Application Name for the image
        /// </summary>
        public String ApplicationName
        {
            get
            {
                return GetQuery(policy_ApplicationName) as String;
            }
            set
            {
                SetQuery(policy_ApplicationName, value);
            }
        }

        /// <summary>
        /// Access Copyright information for the image
        /// </summary>
        public String Copyright
        {
            get
            {
                return GetQuery(policy_Copyright) as String;
            }
            set
            {
                SetQuery(policy_Copyright, value);
            }
        }

        /// <summary>
        /// Access Camera Manufacturer for the image
        /// </summary>
        public String CameraManufacturer
        {
            get
            {
                return GetQuery(policy_CameraManufacturer) as String;
            }
            set
            {
                SetQuery(policy_CameraManufacturer, value);
            }
        }

        /// <summary>
        /// Access Camera Model for the image
        /// </summary>
        public String CameraModel
        {
            get
            {
                return GetQuery(policy_CameraModel) as String;
            }
            set
            {
                SetQuery(policy_CameraModel, value);
            }
        }

        /// <summary>
        /// Access Keywords for the image
        /// </summary>
        public ReadOnlyCollection<String> Keywords
        {
            get
            {
                String[] strKeywords = GetQuery(policy_Keywords) as String[];

                return (strKeywords == null) ?
                       null
                     : new ReadOnlyCollection<String>(strKeywords);
            }
            set
            {
                String[] strKeywords = null;
                if (value != null)
                {
                    strKeywords = new String[value.Count];
                    value.CopyTo(strKeywords, 0);
                }

                SetQuery(policy_Keywords, strKeywords);
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Implements common copy code for CloneCore(), CloneCurrentValueCore(), GetAsFrozenCore(), and
        /// GetCurrentValueAsFrozenCore()
        /// </summary>
        /// <param name="sourceBitmapMetadata"></param>
        private void CopyCommon(BitmapMetadata sourceBitmapMetadata)
        {
            BitmapMetadataBlockWriter blockWriter = sourceBitmapMetadata.BlockWriter;

            if (blockWriter == null)
            {
                // If source is a metadata
                InitializeFromMetadataWriter(sourceBitmapMetadata._metadataHandle, sourceBitmapMetadata._syncObject);
            }

            if (_metadataHandle == null)
            {
                if (blockWriter != null)
                {
                    InitializeFromBlockWriter(blockWriter, sourceBitmapMetadata._syncObject);
                }
                else
                {
                    InitializeFromBlockWriter(sourceBitmapMetadata.GuidFormat, false, false);

                    SetQuery("/", sourceBitmapMetadata);
                }
            }

            _fixedSize = sourceBitmapMetadata._fixedSize;
        }

        internal Guid GuidFormat
        {
            get
            {
                Guid guid = new Guid();

                EnsureBitmapMetadata();

                UnsafeNativeMethods.WICMetadataQueryReader.GetContainerFormat(
                    _metadataHandle,
                    out guid).ThrowOnFailureExtended();

                return guid;
            }
        }

        internal SafeMILHandle InternalMetadataHandle
        {
            get
            {
                return _metadataHandle;
            }
        }

        internal object SyncObject
        {
            get
            {
                return _syncObject;
            }
        }

        internal BitmapMetadataBlockWriter BlockWriter
        {
            get
            {
                return _blockWriter;
            }
        }

        private void EnsureBitmapMetadata()
        {
            ReadPreamble();

            if (_metadataHandle == null)
            {
                throw new InvalidOperationException(SR.Image_MetadataInitializationIncomplete);
            }
        }

        #endregion

        private const String policy_Author = "System.Author";
        private const String policy_Title = "System.Title";
        private const String policy_Subject = "System.Subject";
        private const String policy_Comment = "System.Comment";
        private const String policy_Keywords = "System.Keywords";
        private const String policy_DateTaken = "System.Photo.DateTaken";
        private const String policy_ApplicationName = "System.ApplicationName";
        private const String policy_Copyright = "System.Copyright";
        private const String policy_CameraManufacturer = "System.Photo.CameraManufacturer";
        private const String policy_CameraModel = "System.Photo.CameraModel";
        private const String policy_Rating = "System.SimpleRating";

        private SafeMILHandle _metadataHandle;

        private BitmapMetadataBlockWriter _blockWriter;

        private bool _readOnly;
        private bool _fixedSize;

        // Stores the last query -- this is for debugging stress failures
        private object _setQueryValue;
        private string _setQueryString;

        private object _syncObject = new Object();
    }

    #endregion
}
