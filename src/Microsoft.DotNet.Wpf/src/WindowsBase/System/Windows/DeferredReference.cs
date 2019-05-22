// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Proxy object passed to the property system to delay load values.
//

namespace System.Windows
{
    using MS.Internal.WindowsBase;  // FriendAccessAllowed

    // Proxy object passed to the property system to delay load values.
    //
    // The property system will make a GetValue callback (dereferencing the
    // reference) inside DependencyProperty.GetValue calls, or before
    // coercion callbacks to derived classes.
    //
    // DeferredReference instances are passed directly to ValidateValue
    // callbacks (which always go to the DependencyProperty owner class),
    // and also to CoerceValue callbacks on the owner class only.  THEREFORE,
    // IT IS CRITICAL THAT WE NEVER ASSIGN A DEFERREDREFERENCE INSTANCE
    // TO A NON-INTERNAL CLASS.
    [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
    internal abstract class DeferredReference
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Deferences a property value on demand.
        internal abstract object GetValue(BaseValueSourceInternal valueSource);

        // Gets the type of the value it represents
        internal abstract Type GetValueType();

        #endregion Internal Methods
    }

    internal class DeferredMutableDefaultReference : DeferredReference
    {
        #region Constructor

        internal DeferredMutableDefaultReference(PropertyMetadata metadata, DependencyObject d, DependencyProperty dp)
        {
            _sourceObject = d;
            _sourceProperty = dp;
            _sourceMetadata = metadata;
        }

        #endregion Constructor

        #region Methods

        internal override object GetValue(BaseValueSourceInternal valueSource)
        {
            return _sourceMetadata.GetDefaultValue(_sourceObject, _sourceProperty);
        }

        // Gets the type of the value it represents
        internal override Type GetValueType()
        {
            return _sourceProperty.PropertyType;
        }

        #endregion Methods

        #region Properties

        internal PropertyMetadata SourceMetadata
        {
            get { return _sourceMetadata; }
        }

        protected DependencyObject SourceObject
        {
            get { return _sourceObject; }
        }

        protected DependencyProperty SourceProperty
        {
            get { return _sourceProperty; }
        }

        #endregion Properties

        #region Data

        private readonly PropertyMetadata _sourceMetadata;
        private readonly DependencyObject _sourceObject;
        private readonly DependencyProperty _sourceProperty;

        #endregion Data
    }
}
