// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for ItemContainer Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;
using System.Runtime.InteropServices;
using System.Globalization;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents Containers that maintains items and support item look up by propety value.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class ItemContainerPattern: BasePattern
#else
    public class ItemContainerPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private ItemContainerPattern(AutomationElement el, SafePatternHandle hPattern)
            : base(el, hPattern)
        {
            _hPattern = hPattern;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>ItemContainer pattern</summary>
        public static readonly AutomationPattern Pattern = ItemContainerPatternIdentifiers.Pattern;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Find item by specified property/value. It will return
        /// placeholder which depending upon it's virtualization state may
        /// or may not have the information of the complete peer/Wrapper.
        /// 
        /// Throws ArgumentException if the property requested is not one that the
        /// container supports searching over. Supports Name property, AutomationId,
        /// IsSelected and ControlType.
        /// 
        /// This method is expected to be relatively slow, since it may need to
        /// traverse multiple objects in order to find a matching one.
        /// When used in a loop to return multiple items, no specific order is
        /// defined so long as each item is returned only once (ie. loop should
        /// terminate). This method is also item-centric, not UI-centric, so items
        /// with multiple UI representations need only be returned once.
        ///
        /// A special propertyId of 0 means ‘match all items’. This can be used
        /// with startAfter=NULL to get the first item, and then to get successive
        /// items.
        /// </summary>
        /// <param name="startAfter">this represents the item after which the container wants to begin search</param>
        /// <param name="property">corresponds to property for whose value it want to search over.</param>
        /// <param name="value">value to be searched for, for specified property</param>
        /// <returns>The first item which matches the searched criterion, if no item matches, it returns null  </returns>
        public AutomationElement FindItemByProperty(AutomationElement startAfter, AutomationProperty property, object value)
        {
           SafeNodeHandle hNode;
           
           // Invalidate the "value" passed against the "property" before passing it to UIACore, Don't invalidate if search is being done for "null" property
           // FindItemByProperty supports find for null property.
           if (property != null)
           {
               value = PropertyValueValidateAndMap(property, value);
           }
           
           if (startAfter != null)
           {
               if (property != null)
                   hNode = UiaCoreApi.ItemContainerPattern_FindItemByProperty(_hPattern, startAfter.RawNode, property.Id, value);
               else
                   hNode = UiaCoreApi.ItemContainerPattern_FindItemByProperty(_hPattern, startAfter.RawNode, 0, null);
           }
           else
           {
               if (property != null)
                   hNode = UiaCoreApi.ItemContainerPattern_FindItemByProperty(_hPattern, new SafeNodeHandle(), property.Id, value);
               else
                   hNode = UiaCoreApi.ItemContainerPattern_FindItemByProperty(_hPattern, new SafeNodeHandle(), 0, null);
           }
               

           AutomationElement wrappedElement = AutomationElement.Wrap(hNode);
           return wrappedElement;
        }

        #endregion Public Methods

       //------------------------------------------------------
       //
       //  Private Methods
       //
       //------------------------------------------------------

       #region Private Methods

        private object PropertyValueValidateAndMap(AutomationProperty property, object value)
        {
            AutomationPropertyInfo info;
            if (!Schema.GetPropertyInfo(property, out info))
            {
                throw new ArgumentException(SR.Get(SRID.UnsupportedProperty));
            }

            // Check type is appropriate: NotSupported is allowed against any property,
            // null is allowed for any reference type (ie not for value types), otherwise
            // type must be assignable from expected type.
            Type expectedType = info.Type;
            if (value != AutomationElement.NotSupported &&
                ((value == null && expectedType.IsValueType)
                || (value != null && !expectedType.IsAssignableFrom(value.GetType()))))
            {
                throw new ArgumentException(SR.Get(SRID.PropertyConditionIncorrectType, property.ProgrammaticName, expectedType.Name));
            }

            // Some types are handled differently in managed vs unmanaged - handle those here...
            if (value is AutomationElement)
            {
                // If this is a comparison against a Raw/LogicalElement,
                // save the runtime ID instead of the element so that we
                // can take it cross-proc if needed.
                value = ((AutomationElement)value).GetRuntimeId();
            }
            else if (value is ControlType)
            {
                // If this is a control type, use the ID, not the CLR object
                value = ((ControlType)value).Id;
            }
            else if (value is Rect)
            {
                Rect rc = (Rect)value;
                value = new double[] { rc.Left, rc.Top, rc.Width, rc.Height };
            }
            else if (value is Point)
            {
                Point pt = (Point)value;
                value = new double[] { pt.X, pt.Y };
            }
            else if (value is CultureInfo)
            {
                value = ((CultureInfo)value).LCID;
            }

            return value;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        static internal object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new ItemContainerPattern(el, hPattern);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;

        #endregion Private Fields
    }
}

