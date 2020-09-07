// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Automation;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Flags that affect how a property value is compared in a PropertyCondition
    /// </summary>
    [Flags]
#if (INTERNAL_COMPILE)
    internal enum PropertyConditionFlags
#else
    public enum PropertyConditionFlags
#endif
    {
        ///<summary>Properties are to be compared using default options (eg. case-sensitive comparison for strings)</summary>
        None = 0x00,
        ///<summary>For string comparisons, specifies that a case-insensitive comparison should be used</summary>
        IgnoreCase = 0x01,
    }


    /// <summary>
    /// Condition that checks whether a property has the specified value
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class PropertyCondition : Condition
#else
    public class PropertyCondition : Condition
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Constructor to create a condition that checks whether a property has the specified value
        /// </summary>
        /// <param name="property">The property to check</param>
        /// <param name="value">The value to check the property for</param>
        public PropertyCondition( AutomationProperty property, object value )
        {
            Init(property, value, PropertyConditionFlags.None);
        }

        /// <summary>
        /// Constructor to create a condition that checks whether a property has the specified value
        /// </summary>
        /// <param name="property">The property to check</param>
        /// <param name="value">The value to check the property for</param>
        /// <param name="flags">Flags that affect the comparison</param>
        public PropertyCondition( AutomationProperty property, object value, PropertyConditionFlags flags )
        {
            Init(property, value, flags);
        }



        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Returns the property that this condition is checking for
        /// </summary>
        public AutomationProperty Property
        {
            get
            {
                return _property;
            }
        }

        /// <summary>
        /// Returns the value of the property that this condition is checking for
        /// </summary>
        public object Value
        {
            get
            {
                return _val;
            }
        }

        /// <summary>
        /// Returns the flags used in this property comparison
        /// </summary>
        public PropertyConditionFlags Flags
        {
            get
            {
                return _flags;
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        void Init(AutomationProperty property, object val, PropertyConditionFlags flags )
        {
            Misc.ValidateArgumentNonNull(property, "property");

            AutomationPropertyInfo info;
            if (!Schema.GetPropertyInfo(property, out info))
            {
                throw new ArgumentException(SR.Get(SRID.UnsupportedProperty));
            }

            // Check type is appropriate: NotSupported is allowed against any property,
            // null is allowed for any reference type (ie not for value types), otherwise
            // type must be assignable from expected type.
            Type expectedType = info.Type;
            if (val != AutomationElement.NotSupported &&
                ((val == null && expectedType.IsValueType)
                || (val != null && !expectedType.IsAssignableFrom(val.GetType()))))
            {
                throw new ArgumentException(SR.Get(SRID.PropertyConditionIncorrectType, property.ProgrammaticName, expectedType.Name));
            }

            if ((flags & PropertyConditionFlags.IgnoreCase) != 0)
            {
                Misc.ValidateArgument(val is string, SRID.IgnoreCaseRequiresString);
            }

            // Some types are handled differently in managed vs unmanaged - handle those here...
            if (val is AutomationElement)
            {
                // If this is a comparison against a Raw/LogicalElement,
                // save the runtime ID instead of the element so that we
                // can take it cross-proc if needed.
                val = ((AutomationElement)val).GetRuntimeId();
            }
            else if (val is ControlType)
            {
                // If this is a control type, use the ID, not the CLR object
                val = ((ControlType)val).Id;
            }
            else if (val is Rect)
            {
                Rect rc = (Rect)val;
                val = new double[] { rc.Left, rc.Top, rc.Width, rc.Height };
            }
            else if (val is Point)
            {
                Point pt = (Point)val;
                val = new double[] { pt.X, pt.Y };
            }
            else if (val is CultureInfo)
            {
                val = ((CultureInfo)val).LCID;
            }

            _property = property;
            _val = val;
            _flags = flags;
            SetMarshalData(new UiaCoreApi.UiaPropertyCondition(_property.Id, _val, _flags));
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationProperty _property;
        private object _val;
        private PropertyConditionFlags _flags;

        #endregion Private Fields
    }
}
