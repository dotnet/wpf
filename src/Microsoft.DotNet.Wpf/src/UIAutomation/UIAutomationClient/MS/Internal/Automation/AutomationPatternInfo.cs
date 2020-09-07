// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: class containing information about an automation property

using System;
using System.Windows.Automation;

namespace MS.Internal.Automation
{
    // struct containing information about an automation property
    internal delegate object WrapObjectClientSide(AutomationElement el, SafePatternHandle hPattern, bool cached);

    internal class AutomationPatternInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        public AutomationPatternInfo( 
            AutomationPattern id,
            AutomationProperty [ ] properties,
            WrapObjectClientSide clientSideWrapper )
        {
            _id = id;
            _properties = properties;
            _clientSideWrapper = clientSideWrapper;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
 
        #region Internal Properties

        internal AutomationPattern         ID                    { get { return _id; } }
        internal AutomationProperty [ ]    Properties            { get { return _properties; } }
        internal WrapObjectClientSide      ClientSideWrapper     { get { return _clientSideWrapper; } }
        
        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPattern _id;
        private AutomationProperty [ ] _properties;
        private WrapObjectClientSide _clientSideWrapper;

        #endregion Private Fields
    }
}
