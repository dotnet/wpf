// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Class which acts as adaptor between IManagedFilter and IFilter.
//   Used by PackageFilter for XamlFilter and CorePropertiesFilter.
//   Used by EncryptedPackageFilter for CorePropertiesFilter.
//


using System.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Windows;                     // for ExceptionStringTable
using System.Security;                    // for SecurityCritical

using MS.Internal.Interop;                // for STAT_CHUNK, etc.
using MS.Internal;                        // for Invariant
using MS.Win32;

namespace MS.Internal.IO.Packaging
{
    #region IndexingFilterMarshaler
    /// <summary>
    /// Adapter for IManagedFilter. Used to obtain an IFilter interface from an IManagedFilter.
    /// </summary>
    /// <remarks>
    /// IManagedFilter is supported by filters which have purely managed implementation and don't work
    /// with interop entities. Callers like PackageFilter and EncryptedPackageFilter which support
    /// IFilter interfaces interact with IManagedFilters like XamlFilter, PackageCorePropertiesFilter
    /// and EncryptedPackageCorePropertiesFilter through IndexingFilterMarshaler.
    /// </remarks>
    internal class IndexingFilterMarshaler : IFilter
    {
        #region Static members.
        /// <summary>
        /// pre-defined GUID for storage properties on file system files (as per MSDN)
        /// </summary>
        internal static Guid PSGUID_STORAGE = new Guid(0xb725f130, 0x47ef, 0x101a, 0xa5, 0xf1, 0x02, 0x60, 0x8c, 0x9e, 0xeb, 0xac);

        /// Cache frequently used size values to incur reflection cost just once.
        internal const Int32 _int16Size = 2;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="managedFilter">IManagedFilter implementation</param>
        internal IndexingFilterMarshaler(IManagedFilter managedFilter)
        {
            if (managedFilter == null)
            {
                throw new ArgumentNullException("managedFilter");
            }

            _implementation = managedFilter;
        }

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="cAttributes">length of aAttributes</param>
        /// <param name="aAttributes">array of FULLPROPSPEC structs</param>
        /// <returns>managed array</returns>
        internal static ManagedFullPropSpec[] MarshalFullPropSpecArray(
            uint cAttributes,    // length of aAttributes
            FULLPROPSPEC[] aAttributes) 
        {
            // If there are attributes, these override the flags
            if (cAttributes > 0)
            {
                // Attributes count and array should match.
                // This has already been checked for by XpsFilter.
                Invariant.Assert(aAttributes != null);

                ManagedFullPropSpec[] initAttributes = new ManagedFullPropSpec[checked((int)cAttributes)];

                // convert to managed equivalents to isolate the marshaling effort
                for (int i = 0; i < cAttributes; i++)
                {
                    // convert and add to local list
                    initAttributes[i] = new ManagedFullPropSpec(aAttributes[i]);
                }

                return initAttributes;
            }
            else
                return null;
        }

        /// <summary>
        /// StringToPtr
        /// </summary>
        /// <remarks>Converts a managed string into the format useful for IFilter.GetText</remarks>
        /// <param name="s">string to convert</param>
        /// <param name="bufCharacterCount">maximum number of characters to convert</param>
        /// <param name="p">pointer to write to</param>
        internal static void MarshalStringToPtr(string s, ref uint bufCharacterCount, IntPtr p)
        {
            // bufCharacterCount is never supposed to be zero at this level.
            Invariant.Assert(bufCharacterCount != 0);

            // ensure the interface rules are followed
            // string must also be null terminated so we restrict the length to buf size - 1
            if ((uint)(s.Length) > bufCharacterCount - 1)
                throw new InvalidOperationException(SR.Get(SRID.FilterGetTextBufferOverflow));

            // Return the number of characters written, including the terminating null.
            bufCharacterCount = (UInt32)s.Length + 1;

            // convert string to unmanaged string and write into provided buffer
            Marshal.Copy(s.ToCharArray(), 0, p, s.Length);

            // null terminate (16bit's of zero to replace one Unicode character)
            Marshal.WriteInt16(p, s.Length * _int16Size, 0);
        }

        /// <summary>
        /// Marshal Managed to Native PROPSPEC
        /// </summary>
        /// <param name="propSpec"></param>
        /// <param name="native"></param>
        internal static void MarshalPropSpec(ManagedPropSpec propSpec, ref PROPSPEC native)
        {
            native.propType = (uint)propSpec.PropType;
            switch (propSpec.PropType)
            {
                case PropSpecType.Id:
                    native.union.propId = (uint)propSpec.PropId;
                    break;

                case PropSpecType.Name:
                    native.union.name = Marshal.StringToCoTaskMemUni(propSpec.PropName);
                    break;

                default:
                    Invariant.Assert(false); // propSpec.PropType is set by internal code in the filter logic.
                    break;
            }
        }

        /// <summary>
        /// Marshal Managed to Native FULLPROPSPEC
        /// </summary>
        /// <param name="fullPropSpec"></param>
        /// <param name="native"></param>
        internal static void MarshalFullPropSpec(ManagedFullPropSpec fullPropSpec, ref FULLPROPSPEC native)
        {
            native.guid = fullPropSpec.Guid;
            MarshalPropSpec(fullPropSpec.Property, ref native.property);
        }

        /// <summary>
        /// GetChunk
        /// </summary>
        /// <returns>An interop STAT_CHUNK from a ManagedChunk</returns>
        internal static STAT_CHUNK MarshalChunk(ManagedChunk chunk)
        {
            STAT_CHUNK native = new STAT_CHUNK();

            native.idChunk = chunk.ID;
            Invariant.Assert(chunk.BreakType >= CHUNK_BREAKTYPE.CHUNK_NO_BREAK && chunk.BreakType <= CHUNK_BREAKTYPE.CHUNK_EOC);
            native.breakType = chunk.BreakType;
            Invariant.Assert(    
                chunk.Flags >= 0 
                && 
                chunk.Flags <= (CHUNKSTATE.CHUNK_TEXT | CHUNKSTATE.CHUNK_VALUE | CHUNKSTATE.CHUNK_FILTER_OWNED_VALUE));
            native.flags = chunk.Flags;
            native.locale = chunk.Locale;
            native.idChunkSource = chunk.ChunkSource;
            native.cwcStartSource = chunk.StartSource;
            native.cwcLenSource = chunk.LenSource;
            MarshalFullPropSpec(chunk.Attribute, ref native.attribute);

            return native;
        }

        /// <summary>
        /// MarshalPropVariant
        /// </summary>
        /// <param name="obj">Object to marshal, should be DateTime or String</param>
        /// <returns>newly allocated PROPVARIANT structure</returns>
        internal static IntPtr MarshalPropVariant(Object obj)
        {
            IntPtr pszVal = IntPtr.Zero;
            IntPtr pNative = IntPtr.Zero;

            try
            {
                PROPVARIANT v;

                if (obj is string)
                {
                    pszVal = Marshal.StringToCoTaskMemAnsi((string)obj);
                    
                    v = new PROPVARIANT();
                    v.vt = VARTYPE.VT_LPSTR;
                    v.union.pszVal = pszVal;
                }
                else if (obj is DateTime)
                {
                    v = new PROPVARIANT();
                    v.vt = VARTYPE.VT_FILETIME;
                    long longFileTime = ((DateTime)obj).ToFileTime();
                    v.union.filetime.dwLowDateTime = (Int32)longFileTime;
                    v.union.filetime.dwHighDateTime = (Int32)((longFileTime >> 32) & 0xFFFFFFFF);
                }
                else
                {
                    throw new InvalidOperationException(
                        SR.Get(SRID.FilterGetValueMustBeStringOrDateTime));
                }

                // allocate an unmanaged PROPVARIANT to return
                pNative = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(PROPVARIANT)));

                // Per MSDN, AllocCoTaskMem never returns null: check for IntPtr.Zero instead.
                Invariant.Assert(pNative != IntPtr.Zero);

                // marshal the managed PROPVARIANT into the unmanaged block and return it
                Marshal.StructureToPtr(v, pNative, false);
            }
            catch
            {
                if (pszVal != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pszVal);
                }

                if (pNative != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pNative);
                }

                throw;
            }

            return pNative;
        }
        #endregion Static members.

        #region IFilter implementation
        /// <summary>
        /// Init
        /// </summary>
        /// <param name="grfFlags">usage flags</param>
        /// <param name="cAttributes">length of aAttributes</param>
        /// <param name="aAttributes">array of FULLPROPSPEC structs</param>
        /// <returns>flags</returns>
        public IFILTER_FLAGS Init(IFILTER_INIT grfFlags,       // IFILTER_INIT value     
            uint cAttributes,               // length of aAttributes
            FULLPROPSPEC[] aAttributes)     // restrict responses to the specified attributes
        {
            ManagedFullPropSpec[] managedArray = MarshalFullPropSpecArray(
                                                 cAttributes, aAttributes);

            return _implementation.Init(grfFlags, managedArray);
        }

        /// <summary>
        /// GetChunk
        /// </summary>
        /// <returns>the next chunk</returns>
        public STAT_CHUNK GetChunk()
        {
            // Get the managed chunk
            ManagedChunk managedChunk = _implementation.GetChunk();

            if (managedChunk == null)
            {
                // End of chunks.

                if (ThrowOnEndOfChunks)
                {
                    // Throw exception.
                    throw new COMException(SR.Get(SRID.FilterEndOfChunks),
                        (int)FilterErrorCode.FILTER_E_END_OF_CHUNKS);
                }

                // Return STAT_CHUNK with idChunk as 0.

                STAT_CHUNK chunk = new STAT_CHUNK();
                chunk.idChunk = 0;
                return chunk;
}

            // Valid chunk. Return corresponding STAT_CHUNK.
            return MarshalChunk(managedChunk);
        }

        /// <summary>
        ///    GetText
        /// </summary>
        /// <param name="bufCharacterCount">Buffer size in Unicode characters (not bytes)</param>
        /// <param name="pBuffer">Pre-allocated buffer for us to write into.  String must be null-terminated.</param>
        public void GetText(ref uint bufCharacterCount, IntPtr pBuffer)
        {
            // NOTE: bufCharacterCount and pBuffer are already validated by XpsFilter.
            // In future, if this class is exposed publicly or used in a way other than 
            // through XpsFilter, proper checks needs to be present either here or in the caller.

            // get the managed string and marshal
            MarshalStringToPtr(_implementation.GetText((int)bufCharacterCount - 1),
                ref bufCharacterCount, pBuffer);
        }

        /// <summary>
        /// GetValue
        /// </summary>
        /// <returns>newly allocated PROPVARIANT structure</returns>
        public IntPtr GetValue()
        {
            return MarshalPropVariant(_implementation.GetValue());
        }

        /// <summary>
        /// BindRegion
        /// </summary>
        /// <param name="origPos"></param>
        /// <param name="riid"></param>
        /// <remarks>
        /// The MSDN specification requires this function to return E_NOTIMPL for the time being.
        /// </remarks>
        public IntPtr BindRegion(FILTERREGION origPos, ref Guid riid)
        {
            // The following exception maps to E_NOTIMPL.
            throw new NotImplementedException(SR.Get(SRID.FilterBindRegionNotImplemented));
        }
        #endregion IFilter implementation

        #region Properties

        /// <summary>
        /// If set to true, FILTER_E_END_OF_CHUNKS exception is thrown
        /// by IFilter.GetChunk() on end of chunks. 
        /// If false, a STAT_CHUNK with idChunk as 0 is returned instead.
        /// </summary>
        internal bool ThrowOnEndOfChunks
        {
            get
            {
                return _throwOnEndOfChunks;
            }

            set
            {
                _throwOnEndOfChunks = value;
            }
        }

        #endregion Properties

        #region Fields

        private IManagedFilter _implementation;
        private bool _throwOnEndOfChunks = true;

        #endregion Fields
    }
    #endregion IndexingFilterMarshaler
}
