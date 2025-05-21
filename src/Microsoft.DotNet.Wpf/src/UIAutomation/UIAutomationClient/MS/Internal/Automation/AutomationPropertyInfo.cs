// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: class containing information about an automation property

using System;
using System.Windows.Automation;

namespace MS.Internal.Automation
{
    // This is used to cast the VARIANT-based objects that we get back from the unmanaged
    // API to our own types - eg enums need to be cast from int VT_I4s to real enums.
    internal delegate object AutomationPropertyConverter( object valueAsObject );

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
            AutomationPropertyConverter converter,
            AutomationProperty id,
            Type type,
            object defaultValue
            )
        {
            _id = id;
            _type = type;
            _defaultValue = defaultValue;
            _converter = converter;
        }
        
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
 
        #region Internal Properties

        internal AutomationProperty          ID                { get { return _id; } }
        internal object                      DefaultValue      { get { return _defaultValue; } }
        internal AutomationPropertyConverter ObjectConverter   { get { return _converter; } }
        internal Type                        Type              { get { return _type; } }

        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationProperty _id;
        private Type _type;
        private object _defaultValue;
        private AutomationPropertyConverter _converter;

        #endregion Private Fields
    }
}
