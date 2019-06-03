// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows 
{
    using System;

    /// <summary>
    ///     This attribute declares that a property is visible when the 
    ///     property owner is a parent of another element.  For example, 
    ///     Canvas.Left is only useful on elements parented within the 
    ///     canvas.  The class supports two types of tree walks:  a shallow 
    ///     walk, the default which requires that the immediate parent be the 
    ///     owner type of the property, and a deep walk, declared by setting 
    ///     IncludeDescendants to true and requires that the owner type be 
    ///     somewhere in the parenting hierarchy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AttachedPropertyBrowsableForChildrenAttribute : AttachedPropertyBrowsableAttribute 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        
        /// <summary>
        ///     Creates a new AttachedPropertyBrowsableForChildrenAttribute.
        /// </summary>
        public AttachedPropertyBrowsableForChildrenAttribute()
        {
        }
        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        /// <summary>
        ///     Gets or sets if the property should be browsable for just the 
        ///     immediate children (false) or all children (true).
        /// </summary>
        public bool IncludeDescendants
        {
            get
            {
                return _includeDescendants;
            }
            set
            {
                _includeDescendants = value;
            }
        }
    

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     Overrides Object.Equals to implement correct equality semantics for this
        ///     attribute.
        /// </summary>
        public override bool Equals(object obj) 
        {
            AttachedPropertyBrowsableForChildrenAttribute other = obj as AttachedPropertyBrowsableForChildrenAttribute;
            if (other == null) return false;
            return _includeDescendants == other._includeDescendants;
        }

        /// <summary>
        ///     Overrides Object.GetHashCode to implement correct hashing semantics.
        /// </summary>
        public override int GetHashCode() 
        {
            return _includeDescendants.GetHashCode();
        }


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

    
        /// <summary>
        ///     Returns true if the object provided is the immediate logical 
        ///     child (if IncludeDescendants is false) or any logical child 
        ///     (if IncludeDescendants is true).
        /// </summary>
        internal override bool IsBrowsable(DependencyObject d, DependencyProperty dp)
        {
            if (d == null) throw new ArgumentNullException("d");
            if (dp == null) throw new ArgumentNullException("dp");

            DependencyObject walk = d;
            Type ownerType = dp.OwnerType;

            do
            {
                walk = FrameworkElement.GetFrameworkParent(walk);

                if (walk != null && ownerType.IsInstanceOfType(walk)) 
                {
                    return true;
                }
            }
            while (_includeDescendants && walk != null);

            return false;
        }
    
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private bool _includeDescendants;
    }
}

