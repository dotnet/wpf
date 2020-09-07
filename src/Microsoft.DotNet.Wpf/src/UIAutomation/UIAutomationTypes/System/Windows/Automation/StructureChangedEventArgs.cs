// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Structure changed event args class
//
//  
//

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using MS.Internal.Automation;

namespace System.Windows.Automation 
{
    /// <summary>
    /// Delegate to handle logical structure change events
    /// </summary>
#if (INTERNAL_COMPILE)
    internal delegate void StructureChangedEventHandler(object sender, StructureChangedEventArgs e);
#else
    public delegate void StructureChangedEventHandler(object sender, StructureChangedEventArgs e);
#endif

    /// <summary>
    /// Logical structure change flags
    /// </summary>
    [ComVisible(true)]
    [Guid("e4cfef41-071d-472c-a65c-c14f59ea81eb")]
#if (INTERNAL_COMPILE)
    internal enum StructureChangeType
#else
    public enum StructureChangeType
#endif
    {
        /// <summary>Logical child added</summary>
        ChildAdded,
        /// <summary>Logical child removed</summary>
        ChildRemoved,
        /// <summary>Logical children invalidated</summary>
        ChildrenInvalidated,
        /// <summary>Logical children were bulk added</summary>
        ChildrenBulkAdded,
        /// <summary>Logical children were bulk removed</summary>
        ChildrenBulkRemoved,
        /// <summary>The order of the children below their parent has changed.</summary>
        ChildrenReordered,
    }

    /// <summary>
    /// Structure changed event args class
    /// </summary>
    /// <ExternalAPI/> 
#if (INTERNAL_COMPILE)
    internal sealed class StructureChangedEventArgs : AutomationEventArgs
#else
    public sealed class StructureChangedEventArgs : AutomationEventArgs
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Constructor for logical structure changed event args.
        /// </summary>
        /// <ExternalAPI/>  
        public StructureChangedEventArgs(StructureChangeType structureChangeType, int [] runtimeId) 
            : base(AutomationElementIdentifiers.StructureChangedEvent) 
        {
            if (runtimeId == null)
            {
                throw new ArgumentNullException("runtimeId");
            }
            _structureChangeType = structureChangeType;
            _runtimeID = (int [])runtimeId.Clone();
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Returns the PAW runtime identifier
        /// </summary>
        /// <ExternalAPI/> 
        public int [] GetRuntimeId()
        { 
            return (int [])_runtimeID.Clone();
        }

        /// <summary>
        /// Returns the the type of tree change:
        /// </summary>
        /// <ExternalAPI Inherit="true"/>        
        public StructureChangeType StructureChangeType 
        { 
            get { return _structureChangeType; } 
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private StructureChangeType _structureChangeType;
        private int [] _runtimeID;
        
        #endregion Private Fields
    }
}
