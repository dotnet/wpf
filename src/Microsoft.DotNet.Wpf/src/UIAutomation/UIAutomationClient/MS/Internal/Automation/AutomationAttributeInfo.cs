// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: class containing information about an automation text atibute
//

using System;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace MS.Internal.Automation
{
    // class containing information about an automation property
    internal class AutomationAttributeInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal AutomationAttributeInfo( 
            AutomationPropertyConverter converter,
            AutomationTextAttribute id,
            Type type
            )
        {
            _id = id;
            _type = type;
            _converter = converter;
        }
        
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
 
        #region Internal Properties

        internal AutomationTextAttribute     ID                { get { return _id; } }
        internal AutomationPropertyConverter ObjectConverter   { get { return _converter; } }
        internal Type                        Type              { get { return _type; } }

        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationTextAttribute _id;
        private Type _type;
        private AutomationPropertyConverter _converter;

        #endregion Private Fields
    }
}
