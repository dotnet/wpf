// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: class containing information about an automation property
//
//

using System;
using System.Windows;
using System.Windows.Automation;

namespace MS.Internal.Automation
{
    // class containing information about an automation property
    internal class AutomationPropertyInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal AutomationPropertyInfo( 
            AutomationProperty id,
            DependencyProperty dependencyProperty,
            DependencyProperty overrideDP
            )
        {
            _id = id;
            _dependencyProperty = dependencyProperty;
            _overrideDP = overrideDP;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
 
        #region Internal Properties

        internal AutomationProperty         ID                  { get { return _id; } }
        internal DependencyProperty         DependencyProperty  { get { return _dependencyProperty; } }
        internal DependencyProperty         OverrideDP          { get { return _overrideDP; } }

        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationProperty _id;
        private DependencyProperty _dependencyProperty;
        private DependencyProperty _overrideDP;

        #endregion Private Fields
    }
}
