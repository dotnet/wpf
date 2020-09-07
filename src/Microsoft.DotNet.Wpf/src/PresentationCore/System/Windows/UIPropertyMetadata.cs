// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System;
using System.Collections.Generic;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    ///     Metadata for supported UI features
    /// </summary>
    public class UIPropertyMetadata : PropertyMetadata
    {
        /// <summary>
        ///     UI metadata construction
        /// </summary>
        public UIPropertyMetadata() :
            base()
        {
        }

        /// <summary>
        ///     UI metadata construction
        /// </summary>
        /// <param name="defaultValue">Default value of property</param>
        public UIPropertyMetadata(object defaultValue) :
            base(defaultValue)
        {
        }

        /// <summary>
        ///     UI metadata construction
        /// </summary>
        /// <param name="propertyChangedCallback">Called when the property has been changed</param>
        public UIPropertyMetadata(PropertyChangedCallback propertyChangedCallback) :
            base(propertyChangedCallback)
        {
        }        

        /// <summary>
        ///     UI metadata construction
        /// </summary>
        /// <param name="defaultValue">Default value of property</param>
        /// <param name="propertyChangedCallback">Called when the property has been changed</param>
        public UIPropertyMetadata(object defaultValue,
                                  PropertyChangedCallback propertyChangedCallback) :
            base(defaultValue, propertyChangedCallback)
        {
        }

        /// <summary>
        ///     UI metadata construction
        /// </summary>
        /// <param name="defaultValue">Default value of property</param>
        /// <param name="propertyChangedCallback">Called when the property has been changed</param>
        /// <param name="coerceValueCallback">Called on update of value</param>
        public UIPropertyMetadata(object defaultValue,
                                PropertyChangedCallback propertyChangedCallback,
                                CoerceValueCallback coerceValueCallback) :
            base(defaultValue, propertyChangedCallback, coerceValueCallback)
        {
        }

        /// <summary>
        ///     UI metadata construction
        /// </summary>
        /// <param name="defaultValue">Default value of property</param>
        /// <param name="propertyChangedCallback">Called when the property has been changed</param>
        /// <param name="coerceValueCallback">Called on update of value</param>
        /// <param name="isAnimationProhibited">Should animation be prohibited?</param>
        public UIPropertyMetadata(object defaultValue,
                                PropertyChangedCallback propertyChangedCallback,
                                CoerceValueCallback coerceValueCallback,
                                bool isAnimationProhibited) :
            base(defaultValue, propertyChangedCallback, coerceValueCallback)
        {
            WriteFlag(MetadataFlags.UI_IsAnimationProhibitedID, isAnimationProhibited);
        }


        /// <summary>
        ///     Creates a new instance of this property metadata.  This method is used
        ///     when metadata needs to be cloned.  After CreateInstance is called the
        ///     framework will call Merge to merge metadata into the new instance.  
        ///     Deriving classes must override this and return a new instance of 
        ///     themselves.
        /// </summary>
        internal override PropertyMetadata CreateInstance() {
            return new UIPropertyMetadata();
        }

        /// <summary>
        /// Set this to true for a property for which animation should be
        /// prohibited. This should not be set unless there are very strong
        /// technical reasons why a property can not be animated. In the
        /// vast majority of cases, a property that can not be properly
        /// animated means that the property implementation contains a bug.
        /// </summary>
        public bool IsAnimationProhibited
        {
            get
            {
                return ReadFlag(MetadataFlags.UI_IsAnimationProhibitedID);
            }
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.TypeMetadataCannotChangeAfterUse));
                }

                WriteFlag(MetadataFlags.UI_IsAnimationProhibitedID, value);
            }
        }
    }
}

