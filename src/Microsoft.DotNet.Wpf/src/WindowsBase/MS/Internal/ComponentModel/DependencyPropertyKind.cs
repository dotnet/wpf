// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Windows;

    //
    // Determines what kind of DP we are dealing with.  DependencyPropertyKind
    // is associated with a target type and a DP.  It calculates IsAttached
    // and IsDirect on demand using the following rules:
    //
    // IsAttached is true iff:
    //
    //      1.  The target type is not a registered owner of the DP, or
    //          the target type is the DP's owner type.
    //
    //      2.  The DP's owner type offers a public static Get method
    //          with the same name as the DP.  The method must
    //          have the correct signature (one parameter that derives 
    //          from DependencyObject).  
    //
    // IsDirect is true iff:
    //
    //      1.  The target type is a registered owner of the DP.
    //
    //      2.  The target type offers a public CLR property of the
    //          same name as the DP.
    //
    // If neither of these is true the property is assumed to be internal
    // and therefore inaccessible to anyone via reflection.
    //
    // A property cannot be both attached and direct.  If a property defines
    // both accessors direct accessors take precidence.
    //
    internal class DependencyPropertyKind 
    {
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------

        //
        // Creates a new instance
        //
        internal DependencyPropertyKind(DependencyProperty dp, Type targetType)
        {
            _dp = dp;
            _targetType = targetType;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //
        // Returns true if the property is internal.  
        //
        internal bool IsInternal
        {
            get
            {
                if (!_isInternalChecked) 
                {
                    // The property is internal if it has no public
                    // static Get method or no public CLR accessor.  If we already
                    // calculated it to be a direct or attached property, bail because
                    // it's clearly not internal.

                    if (!_isAttached && !_isDirect) 
                    {
                        if (DependencyObjectPropertyDescriptor.GetAttachedPropertyMethod(_dp) == null &&
                            _dp.OwnerType.GetProperty(_dp.Name, _dp.PropertyType) == null)
                        {
                            _isInternal = true;
                        }
                    }

                    _isInternalChecked = true;
                }

                return _isInternal;
            }
        }

        //
        // Returns true if the property is attached for the target type.
        //
        internal bool IsAttached
        {
            get
            {
                if (!_isAttachedChecked) 
                {
                    // A property cannot be both attached and direct,
                    // so if this property is already direct we have our
                    // answer.  Note that we check the IsDirect property
                    // here to force evaluation.  Direct takes precidence
                    // over attached on the same object, so we need to 
                    // force the check if it hasn't been done.

                    if (!IsDirect) 
                    {
                        // If the attached property is AddOwnered to this type, we
                        // don't treat it as attached because, by definition, attached
                        // properties can only have one owner, and any AddOwnered version
                        // beceomes a direct property.

                        if (_dp.OwnerType == _targetType || _dp.OwnerType.IsAssignableFrom(_targetType) || DependencyProperty.FromName(_dp.Name, _targetType) != _dp) 
                        {
                            if (DependencyObjectPropertyDescriptor.GetAttachedPropertyMethod(_dp) != null)
                            {
                                _isAttached = true;
                            }
                        }
                    }

                    _isAttachedChecked = true;
                }

                return _isAttached;
            }
        }

        //
        // Returns true if the property is direct for the target type.
        //
        internal bool IsDirect
        {
            get
            {
                if (!_isDirectChecked) 
                {
                    // If we've already calculated attached, we 
                    // know the answer if _isAttached is true because
                    // a property cannot be both.

                    // No need to also check _isAttachedChecked since this will 
                    // only be true if the check has been done.
                    if (!_isAttached) 
                    {
                        if (DependencyProperty.FromName(_dp.Name, _targetType) == _dp) 
                        {
                            if (_targetType.GetProperty(_dp.Name, _dp.PropertyType) != null)
                            {
                                _isDirect = true;
                                _isAttachedChecked = true;
                            }
                        }
                    }

                    _isDirectChecked = true;
                }

                return _isDirect;
            }
        }

    
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------


        private readonly DependencyProperty _dp;
        private readonly Type _targetType;
        private bool _isAttached;
        private bool _isAttachedChecked;
        private bool _isInternal;
        private bool _isInternalChecked;
        private bool _isDirect;
        private bool _isDirectChecked;
    }
}

