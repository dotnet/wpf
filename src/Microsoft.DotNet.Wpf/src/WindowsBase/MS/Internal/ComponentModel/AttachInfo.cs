// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows;

    
    /// <summary>
    ///     This class contains information about an attached property.  It can 
    ///     tell you if the property can be attached to a given object.
    ///
    ///    This class is thread-safe.
    /// </summary>
    internal class AttachInfo
    {
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------

        internal AttachInfo(DependencyProperty dp)
        {
            _dp = dp;
        }


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        
    
        // 
        // Returns the static Get method associated with this attached
        // property.  This can return null if the attached property 
        // has no static get method.
        //
        private MethodInfo AttachMethod
        {
            get
            {
                if (!_getMethodChecked) 
                {
                    _getMethod = DependencyObjectPropertyDescriptor.GetAttachedPropertyMethod(_dp);
                    _getMethodChecked = true;
                }

                return _getMethod;
            }
        }

        //
        // Returns an array of attributes of type AttachedPropertyBrowsableAttribute.
        //
        private AttachedPropertyBrowsableAttribute[] Attributes
        {
            get
            {
                if (!_attributesChecked) 
                {
                    MethodInfo m = AttachMethod;
                    object[] attributes = null;

                    if (m != null) 
                    {
                        AttachedPropertyBrowsableAttribute[] browsableAttributes = null;

                        // No need to inherit attributes here because the method is static
                        attributes = m.GetCustomAttributes(DependencyObjectPropertyDescriptor.AttachedPropertyBrowsableAttributeType, false);

                        // Walk attributes and see if there is a AttachedPropertyBrowsableForTypeAttribute 
                        // present.  If there isn't, fabricate one, but only if there is
                        // at least one AttachedPropertyBrowsableAttribute.  We require at
                        // least one attribute to consider an attached property.
                        bool seenTypeAttribute = false;
                        for (int idx = 0; idx < attributes.Length; idx++) 
                        {
                            if (attributes[idx] is AttachedPropertyBrowsableForTypeAttribute) 
                            {
                                seenTypeAttribute = true;
                                break;
                            }
                        }

                        if (!seenTypeAttribute && attributes.Length > 0) 
                        {
                            browsableAttributes = new AttachedPropertyBrowsableAttribute[attributes.Length + 1];
                            for(int idx = 0; idx < attributes.Length; idx++) 
                            {
                                browsableAttributes[idx] = (AttachedPropertyBrowsableAttribute)attributes[idx];
                            }
                            browsableAttributes[attributes.Length] = ParameterTypeAttribute;
                        }
                        else
                        {
                            browsableAttributes = new AttachedPropertyBrowsableAttribute[attributes.Length];
                            for(int idx = 0; idx < attributes.Length; idx++) 
                            {
                                browsableAttributes[idx] = (AttachedPropertyBrowsableAttribute)attributes[idx];
                            }
                        }

                        // Update the _attributes last, this is how we maintain thread-safety.  There's a slight chance
                        // that this routine will run simultaneously on two threads, but they will produce the same result
                        // anyway.
                        
                        _attributes = browsableAttributes;
                    }

                    _attributesChecked = true;
                }

                return _attributes;
            }
        }

        //
        // Returns a custom attached property browsable attribute that
        // verifies the parameter type is compatible.  This may return null
        // if we've done the work and verified that there is no need for
        // such an attribute.
        //
        private AttachedPropertyBrowsableAttribute ParameterTypeAttribute
        {
            get
            {
                if (!_paramTypeAttributeChecked) 
                {
                    MethodInfo m = AttachMethod;

                    if (m != null) 
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        // The AttachMethod property should have already done the correct
                        // work to ensure this method has the right signature.  Assert here
                        // that we assume this is the case.
                        Debug.Assert(parameters != null && parameters.Length == 1, "GetAttachedPropertyMethod should return a method with one parameter.");
                        TypeDescriptionProvider typeProvider = TypeDescriptor.GetProvider(_dp.OwnerType);
                        _paramTypeAttribute = new AttachedPropertyBrowsableForTypeAttribute(typeProvider.GetRuntimeType(parameters[0].ParameterType));
                    }

                    _paramTypeAttributeChecked = true;
                }

                return _paramTypeAttribute;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        //
        // Returns true if the DP can logically be attached to the
        // given instance.  This will return false if the property
        // is not an attached property or if the property's 
        // AttachedPropertyBrowsableAttributes are not compatible with
        // this instance.
        //
        internal bool CanAttach(DependencyObject instance)
        {
            Debug.Assert(instance != null, "Caller should validate non-null instance before calling CanAttach.");

            if (AttachMethod != null) 
            {
                int numAttrs = 0;

                AttachedPropertyBrowsableAttribute[] attrs = Attributes;
                if (attrs != null) 
                {
                    numAttrs = attrs.Length;
                    for(int idx = 0; idx < numAttrs; idx++)
                    {
                        AttachedPropertyBrowsableAttribute attr = attrs[idx];
                    
                        if (!attr.IsBrowsable(instance, _dp)) 
                        {
                            // The attribute isn't browsable.  If UnionResults is turned on, we must
                            // look for another matching attribute of the same type.  I don't expect
                            // a very large list of attributes here, so I'd rather go n^2 than
                            // create a list of UnionResults attributes somewhere I need to reconcile.

                            bool isBrowsable = false;

                            if (attr.UnionResults) 
                            {
                                Type attrType = attr.GetType();

                                for(int idx2 = 0; idx2 < numAttrs; idx2++)
                                {
                                    AttachedPropertyBrowsableAttribute subAttr = attrs[idx2];
                                
                                    if (attrType == subAttr.GetType() && subAttr.IsBrowsable(instance, _dp)) 
                                    {
                                        isBrowsable = true;
                                        break;
                                    }
                                }
                            }

                            if (!isBrowsable) 
                            {
                                return false;
                            }
                        }
                    }
                }

                // We got through all matches, this property can be attached
                // to the given instance provided we found at least one
                // attribute. If we didn't, that means the property should
                // be treated as non-visible.

                return numAttrs > 0;
            }

            // No AttachMethod means this property is not an attached
            // property, so it cannot be attached elsewhere.

            return false;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------


        private readonly DependencyProperty _dp;
        private MethodInfo _getMethod;
        private AttachedPropertyBrowsableAttribute[] _attributes;
        private AttachedPropertyBrowsableAttribute _paramTypeAttribute;

        private bool _attributesChecked;
        private bool _getMethodChecked;
        private bool _paramTypeAttributeChecked;
    }
}

