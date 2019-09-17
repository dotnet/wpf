// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows 
{
    using System;
    using System.ComponentModel;

    /// <summary>
    ///     This attribute declares that an attached property can only be attached 
    ///     to an object that defines the given attribute on its class. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AttachedPropertyBrowsableWhenAttributePresentAttribute : AttachedPropertyBrowsableAttribute 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        
        /// <summary>
        ///     Creates a new AttachedPropertyBrowsableWhenAttributePresentAttribute.  Provide
        ///     the type of attribute that, when present on a dependency object,
        ///     should make the property browsable.
        /// </summary>
        public AttachedPropertyBrowsableWhenAttributePresentAttribute(Type attributeType)
        {
            if (attributeType == null) throw new ArgumentNullException("attributeType");

            _attributeType = attributeType;
        }
        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        /// <summary>
        ///     Returns the attribute type passed into the constructor.
        /// </summary>
        public Type AttributeType
        {
            get
            {
                return _attributeType;
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
            AttachedPropertyBrowsableWhenAttributePresentAttribute other = obj as AttachedPropertyBrowsableWhenAttributePresentAttribute;
            if (other == null) return false;
            return _attributeType == other._attributeType;
        }

        /// <summary>
        ///     Overrides Object.GetHashCode to implement correct hashing semantics.
        /// </summary>
        public override int GetHashCode() 
        {
            return _attributeType.GetHashCode();
        }


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

    
        /// <summary>
        ///     Returns true if the dependency object class defines an attribute 
        ///     of the same type contained in this attribute.  The attribute must 
        ///     differ from the "default" state of the attribute.
        /// </summary>
        internal override bool IsBrowsable(DependencyObject d, DependencyProperty dp)
        {
            if (d == null) throw new ArgumentNullException("d");
            if (dp == null) throw new ArgumentNullException("dp");

            Attribute a = TypeDescriptor.GetAttributes(d)[_attributeType];
            return (a != null && !a.IsDefaultAttribute());
        }
    
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private Type _attributeType;
    }
}

