// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows 
{
    using System;

    /// <summary>
    ///     This class declares that an attached property is browsable only 
    ///     for dependency objects that derive from the given type.  If more 
    ///     than one type is specified, the property is browsable if any type 
    ///     matches (logical or).  The type may also be an interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AttachedPropertyBrowsableForTypeAttribute : AttachedPropertyBrowsableAttribute 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        
        /// <summary>
        ///     Creates a new AttachedPropertyBrowsableForTypeAttribute.  Provide the type
        ///     you want the attached property to be browsable for.  Multiple
        ///     attributes may be used to provide support for more than one
        ///     type.
        /// </summary>
        public AttachedPropertyBrowsableForTypeAttribute(Type targetType)
        {
            if (targetType == null) throw new ArgumentNullException("targetType");
            _targetType = targetType;
        }
        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        /// <summary>
        ///     Returns the type passed into the constructor.
        /// </summary>
        public Type TargetType
        {
            get
            {
                return _targetType;
            }
        }
    

        /// <summary>
        ///     For AllowMultiple attributes, TypeId must be unique for
        ///     each unique instance.  The default returns the type, which
        ///     is only correct for AllowMultiple == false.
        /// </summary>
        public override object TypeId
        {
            get
            {
                return this;
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
            AttachedPropertyBrowsableForTypeAttribute other = obj as AttachedPropertyBrowsableForTypeAttribute;
            if (other == null) return false;
            return _targetType == other._targetType;
        }

        /// <summary>
        ///     Overrides Object.GetHashCode to implement correct hashing semantics.
        /// </summary>
        public override int GetHashCode() 
        {
            return _targetType.GetHashCode();
        }


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

    
        /// <summary>
        ///     Returns true if the dependency object passed to the method is a type, 
        ///     subtype or implememts the interface of any of the the types contained 
        ///     in this object.
        /// </summary>
        internal override bool IsBrowsable(DependencyObject d, DependencyProperty dp)
        {
            if (d == null) throw new ArgumentNullException("d");
            if (dp == null) throw new ArgumentNullException("dp");

            // Get the dependency object type for our target type.
            // We cannot assume the user didn't do something wrong and
            // feed us a type that is not a dependency object, but that is
            // rare enough that it is worth the try/catch here rather than
            // a double IsAssignableFrom (one here, and one in DependencyObjectType).
            // We still use a flag here rather than checking for a null
            // _dTargetType so that a bad property that throws won't consistently
            // slow the system down with ArgumentExceptions.

            if (!_dTargetTypeChecked) 
            {
                try
                {
                    _dTargetType = DependencyObjectType.FromSystemType(_targetType);
                }
                catch(ArgumentException)
                {
                }

                _dTargetTypeChecked = true;
            }


            if (_dTargetType != null && _dTargetType.IsInstanceOfType(d)) 
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if a browsable match is true if any one of multiple
        ///     instances of the same type return true for IsBrowsable.  We override
        ///     this to return true because any one of a successfull match for 
        ///     IsBrowsable is accepted.
        /// </summary>
        internal override bool UnionResults
        {
            get
            {
                return true;
            }
        }
    
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private Type _targetType;
        private DependencyObjectType _dTargetType;
        private bool _dTargetTypeChecked;
    }
}

