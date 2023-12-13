// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Windows;

    /// <summary>
    ///     This is a reflection binder that is used to 
    ///     find the right method match for attached properties.
    ///     The default binder in the CLR will not find a 
    ///     method match unless the parameters we provide are
    ///     exact matches.  This binder will use compatible type
    ///     matching to find a match for any parameters that are
    ///     compatible.
    /// </summary>
    internal class AttachedPropertyMethodSelector : Binder 
    {
        /// <summary>
        ///     The only method we implement.  Our goal here is to find a method that best matches the arguments passed.
        ///     We are doing this only with the intent of pulling attached property metadata off of the method.
        ///     If there are ambiguous methods, we simply take the first one as all "Get" methods for an attached
        ///     property should have identical metadata.
        /// </summary>
        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
        {
            // Short circuit for cases where someone didn't pass in a types array.
            if (types == null) 
            {
                if (match.Length > 1) 
                {
                    throw new AmbiguousMatchException();
                }
                else
                {
                    return match[0];
                }
            }

            for(int idx = 0; idx < match.Length; idx++)
            {
                MethodBase candidate = match[idx];
                ParameterInfo[] parameters = candidate.GetParameters();
                if (ParametersMatch(parameters, types)) 
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        ///     This method checks that the parameters passed in are
        ///     compatible with the provided parameter types.
        /// </summary>
        private static bool ParametersMatch(ParameterInfo[] parameters, Type[] types) 
        {
            if (parameters.Length != types.Length) 
            {
                return false;
            }

            // IsAssignableFrom is not cheap.  Do this in two passes.
            // Our first pass checks for exact type matches.  Only on
            // the second pass do we do an IsAssignableFrom.

            bool compat = true;
            for(int idx = 0; idx < parameters.Length; idx++)
            {
                ParameterInfo p = parameters[idx];
                Type t = types[idx];

                if (p.ParameterType != t) 
                {
                    compat = false;
                    break;
                }
            }

            if (compat) 
            {
                return true;
            }

            // Second pass uses IsAssignableFrom to check for compatible types.
            compat = true;
            for(int idx = 0; idx < parameters.Length; idx++)
            {
                ParameterInfo p = parameters[idx];
                Type t = types[idx];

                if (!t.IsAssignableFrom(p.ParameterType))
                {
                    compat = false;
                    break;
                }
            }

            return compat;
        }

        /// <summary>
        ///     We do not implement this.
        /// </summary>
        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
        {
            // We are only a method binder.
            throw new NotImplementedException();
        }

        /// <summary>
        ///     We do not implement this.
        /// </summary>
        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture) 
        {
            // We are only a method binder.
            throw new NotImplementedException();
        }


        /// <summary>
        ///     We do not implement this.
        /// </summary>
        public override object ChangeType(object value, Type type, CultureInfo culture)
        {
            // We are only a method binder.
            throw new NotImplementedException();
        }
        
        /// <summary>
        ///     We do not implement this.
        /// </summary>
        public override void ReorderArgumentArray(ref object[] args, object state)
        {
            // We are only a method binder.
            throw new NotImplementedException();
        }
         
        /// <summary>
        ///     We do not implement this.
        /// </summary>
        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
        {
            // We are only a method binder.
            throw new NotImplementedException();
        }
    }
}

