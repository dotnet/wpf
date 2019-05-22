// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel
{
    using System;
    using System.Windows;

    /// <summary>
    ///     This attribute is synthesized by our DependencyObjectProvider
    ///     to relate a property descriptor back to a dependency property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class DependencyPropertyAttribute : Attribute 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        
        /// <summary>
        ///     Creates a new DependencyPropertyAttribute for the given dependency property.
        /// </summary>
        internal DependencyPropertyAttribute(DependencyProperty dependencyProperty, bool isAttached) 
        {
            if (dependencyProperty == null) throw new ArgumentNullException("dependencyProperty");

            _dp = dependencyProperty;
            _isAttached = isAttached;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        
        /// <summary>
        ///     Override of Object.Equals that returns true when the dependency
        ///     property contained within each attribute is the same.
        /// </summary>
        public override bool Equals(object value) 
        {
            DependencyPropertyAttribute da = value as DependencyPropertyAttribute;

            if (da != null && 
                object.ReferenceEquals(da._dp, _dp) && 
                da._isAttached == _isAttached) 
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Override of Object.GetHashCode();
        /// </summary>
        public override int GetHashCode() 
        {
            return _dp.GetHashCode();
        }

        #endregion Public Methods
        
        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Overrides Attribute.TypeId to be unique with respect to
        ///     other dependency property attributes.c
        /// </summary>
        public override object TypeId 
        {
            get { return typeof(DependencyPropertyAttribute); }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        
        /// <summary>
        ///     Returns whether the dependency property is an attached
        ///     property.
        /// </summary>
        internal bool IsAttached
        {
            get { return _isAttached; }
        }
        
        /// <summary>
        ///     Returns the dependency property instance this attribute is
        ///     associated with.
        /// </summary>
        internal DependencyProperty DependencyProperty 
        {
            get { return _dp; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private DependencyProperty _dp;
        private bool _isAttached;

        #endregion Private Fields
    }
}

