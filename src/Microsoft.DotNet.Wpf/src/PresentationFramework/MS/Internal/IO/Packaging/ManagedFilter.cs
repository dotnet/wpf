// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Managed equivalent of IFilter implementation
//   Defines base class and helper class for interop
//


using System.Diagnostics;
using System;
using System.Windows;                       // for ExceptionStringTable
using System.IO;                            // for FileAccess
using System.Runtime.InteropServices;
using System.Collections;
using System.Security;                      // for SecurityCritical

using MS.Internal.Interop;                  // for CHUNK_BREAKTYPE, etc.
using MS.Internal;                          // for Invariant
using MS.Win32;                             // for NativeMethods
using MS.Internal.PresentationFramework;    // SecurityHelper

// Not using the whole of System.Runtime.InteropServices.ComTypes so as to avoid collisions.
using IPersistFile = System.Runtime.InteropServices.ComTypes.IPersistFile;

namespace MS.Internal.IO.Packaging
{
    #region Managed Struct Equivalents
    /// <summary>
    /// Managed equivalent of a PROPSPEC
    /// </summary>
    internal class ManagedPropSpec
    {
        /// <summary>
        /// Property Type (int or string)
        /// </summary>
        /// <value></value>
        internal PropSpecType PropType
        {
            get
            {
                return _propType;
            }
// The following code is not being compiled, but should not be removed; since some container-filter
// plug-in (e.g. metadata) may use it in future.
#if false
            set
            {
                switch (value)
                {
                    case PropSpecType.Id: break;
                    case PropSpecType.Name: break;

                    default:
                        throw new ArgumentException(SR.Get(SRID.FilterPropSpecUnknownUnionSelector), "propSpec");
                }
                _propType = value;
            }
#endif
        }

        /// <summary>
        /// Property name (only valid if PropType is Name
        /// </summary>
        /// <value></value>
        internal string PropName
        {
            get
            {
                System.Diagnostics.Debug.Assert(_propType == PropSpecType.Name, "ManagedPropSpec.PropName - PropName only meaningful if PropType is type string");
                return _name;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _name = value;
                _id = 0;
                _propType = PropSpecType.Name;
            }
        }

        /// <summary>
        /// Property Id (only valid if PropType is Id)
        /// </summary>
        /// <value></value>
        internal uint PropId
        {
            get
            {
                System.Diagnostics.Debug.Assert(_propType == PropSpecType.Id, "ManagedPropSpec.PropId - PropId only meaningful if PropType is numeric");
                return _id;
            }
            set
            {
                _id = value;
                _name = null;
                _propType = PropSpecType.Id;
            }
        }

        /// <summary>
        /// Create a int-type PropSpec
        /// </summary>
        /// <param name="id"></param>
        internal ManagedPropSpec(uint id)
        {
            // Assign to a property rather than a field to ensure consistency through side-effects.
            PropId = id;
        }

        /// <summary>
        /// Create a ManagedPropSpec from an unmanaged one
        /// </summary>
        internal ManagedPropSpec(PROPSPEC propSpec)
        {

            // Assign to properties rather than fields to ensure consistency through side-effects.
            switch ((PropSpecType)propSpec.propType)
            {
                case PropSpecType.Id:
                    {
                        PropId = propSpec.union.propId;
                        break;
                    }

                case PropSpecType.Name:
                    {
                        PropName = Marshal.PtrToStringUni(propSpec.union.name);
                        break;
                    }
                default:
                    throw new ArgumentException(SR.Get(SRID.FilterPropSpecUnknownUnionSelector), "propSpec");
            }
        }

        /// <summary>
        /// Private properties
        /// </summary>
        private PropSpecType _propType;
        private uint _id;           // valid if we are an ID property type
        private string _name;       // valid if we are a Name property type
    }

    /// <summary>
    /// ManagedFullPropSpec
    /// </summary>
    internal class ManagedFullPropSpec
    {
        /// <summary>
        /// Guid
        /// </summary>
        /// <value></value>
        internal Guid Guid
        {
            get { return _guid; }
        }

        /// <summary>
        /// Property
        /// </summary>
        /// <value></value>
        internal ManagedPropSpec Property
        {
            get
            {
                return _property;
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="propId"></param>
        internal ManagedFullPropSpec(Guid guid, uint propId)
        {
            _guid = guid;
            _property = new ManagedPropSpec(propId);
        }

// If the following is not used once metadata filtering is implemented, remove completely from the code.
#if false
        /// <summary>
        /// Helper constructor
        /// </summary>
        /// <param name="guid">property guid</param>
        /// <param name="propName"></param>
        internal ManagedFullPropSpec(Guid guid, string propName)
        {
            _guid = guid;
            _property = new ManagedPropSpec(propName);
        }
#endif

        /// <summary>
        /// Handles native FULLPROPSPEC and does marshaling
        /// </summary>
        /// <param name="nativePropSpec"></param>
        internal ManagedFullPropSpec(FULLPROPSPEC nativePropSpec)
        {
            _guid = nativePropSpec.guid;
            _property = new ManagedPropSpec(nativePropSpec.property);
        }

        private Guid _guid;
        private ManagedPropSpec _property;
    }

    ///<summary>
    /// A descriptor for a chunk
    /// </summary>
    internal class ManagedChunk
    {
        #region Constructors
        ///<summary>Build a contents chunk, passing the contents string.</summary>
        /// <param name="index">id</param>
        /// <param name="breakType">The opening break for the chunk.</param>
        /// <param name="attribute">attribute</param>
        /// <param name="lcid">The locale ID for the chunk.</param>
        /// <param name="flags">Indicates if it is text or value chunk.</param>
        /// <remarks>
        /// All the chunks returned by the XAML filter and the container filter are text chunks.
        /// Should a future filter implementation be capable of returning value chunks, a new constructor
        /// and a Flags property will have to be defined.
        /// </remarks>
        internal ManagedChunk(uint index, CHUNK_BREAKTYPE breakType, ManagedFullPropSpec attribute, uint lcid, CHUNKSTATE flags)
        {
            // Argument errors can only be due to internal inconsistencies, since no input data makes its way here.
            Invariant.Assert(breakType >= CHUNK_BREAKTYPE.CHUNK_NO_BREAK && breakType <= CHUNK_BREAKTYPE.CHUNK_EOC);
            Invariant.Assert(attribute != null);
            // Note that lcid values potentially cover the full range of uint values
            // (see http://msdn.microsoft.com/library/default.asp?url=/library/en-us/intl/nls_8sj7.asp)
            // and so no useful validation can be made for lcid.

            _index = index;
            _breakType = breakType;
            _lcid = lcid;
            _attribute = attribute;
            _flags = flags;

            // Since pseudo-properties (a.k.a. internal values) are not supported by the XPS filters,
            // all chunks we return are expected to have idChunkSource equal to idChunk.
            // (See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/indexsrv/html/ixufilt_8ib8.asp)
            _idChunkSource = _index;
}
        #endregion Constructors

        #region Properties
        internal uint ID
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }

        /// <summary>
        /// Flags
        /// </summary>
        /// <value></value>
        internal CHUNKSTATE Flags
        {
            get
            {
                return _flags;
            }
        }

// The following code is not being compiled, but should not be removed; since some container-filter
// plug-in (e.g. metadata) may use it in future.
#if false
        ///<summary>Always null, since no named property is supported by the Indexing Services in Longhorn.</summary>
        internal string PropertyName
        {
            get
            {
                return null;
            }
        }
#endif

        ///<summary>Indicates the type of break that precedes the chunk.</summary>
        internal CHUNK_BREAKTYPE BreakType
        {
            get
            {
                return _breakType;
            }
            set
            {
                // Argument errors can only be due to internal inconsistencies,
                // since no input data makes its way here.
                Invariant.Assert(value >= CHUNK_BREAKTYPE.CHUNK_NO_BREAK 
                    && value <= CHUNK_BREAKTYPE.CHUNK_EOC);

                _breakType = value;
            }
        }

        ///<summary>Indicates the locale the chunk belongs to.</summary>
        internal uint Locale
        {
            get
            {
                return _lcid;
            }
            set
            {
                _lcid = value;
            }
        }

        internal uint ChunkSource
        {
            get
            {
                return _idChunkSource;
            }
        }

        internal uint StartSource
        {
            get
            {
                return _startSource;
            }
        }

        internal uint LenSource
        {
            get
            {
                return _lenSource;
            }
        }

        internal ManagedFullPropSpec Attribute
        {
            get
            {
                return _attribute;
            }
            set
            {
                _attribute = value;
            }
        }
        #endregion Properties

        #region Private Fields
        private uint _index;         // chunk id
        private CHUNK_BREAKTYPE _breakType;
        private CHUNKSTATE _flags;
        private uint _lcid;
        private ManagedFullPropSpec _attribute;
        private uint _idChunkSource;
        private uint _startSource = 0;
        private uint _lenSource = 0;

        #endregion Private Fields
    }
    #endregion Managed Struct Equivalents

    #region IManagedFilter

    /// <summary>
    /// Interface for managed implementations of IFilter handlers
    /// </summary>

    interface IManagedFilter
    {
        /// <summary>
        /// Init
        /// </summary>
        /// <param name="grfFlags">Usage flags.  See IFilter spec for details.</param>
        /// <param name="aAttributes">
        /// Array of Managed FULLPROPSPEC structs to restrict responses.
        /// May be null.</param>
        /// <returns>flags</returns>
        IFILTER_FLAGS Init(
            IFILTER_INIT grfFlags,
            ManagedFullPropSpec[] aAttributes);    // restrict responses to the specified attributes

        /// <summary>
        /// GetChunk
        /// </summary>
        /// <returns>
        /// The next managed chunk if it exists, null otherwise.
        /// For valid chunk, ID of returned chunk should be greater than zero.
        /// </returns>
        /// <remarks>
        /// This should not throw exception to indicate end of chunks, and should 
        /// return null instead.
        /// 
        /// This is to avoid the perf hit of throwing an exception even for cases 
        /// in which it doesn't get propagated to the unmanaged IFilter client. 
        /// 
        /// Specifically, when this ManagedFilter is for a content part in a package, 
        /// PackageFilter moves to the next part when the current part has no more 
        /// chunks, and in this case no need for an exception to be thrown 
        /// to indicate FILTER_E_END_OF_CHUNKS.
        /// </remarks>
        ManagedChunk GetChunk();

        /// <summary>
        /// GetText
        /// </summary>
        /// <param name="bufferCharacterCount">
        /// maximum number of Unicode characters to return in the String</param>
        /// <returns>string associated with the last returned Chunk</returns>
        String GetText(int bufferCharacterCount);

        /// <summary>
        /// GetValue
        /// </summary>
        /// <remarks>Only supports string types at this time</remarks>
        /// <returns>property associated with the last returned Chunk</returns>
        Object GetValue();
    }

    #endregion IManagedFilter
}
