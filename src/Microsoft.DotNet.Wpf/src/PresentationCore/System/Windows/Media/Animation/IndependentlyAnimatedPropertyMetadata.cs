// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Windows;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A class registers this type of Metadata for a property if the property
    /// can be independently animated on that class.
    /// </summary>
    internal class IndependentlyAnimatedPropertyMetadata : UIPropertyMetadata
    {
        internal IndependentlyAnimatedPropertyMetadata(object defaultValue) : base(defaultValue) {}

        internal IndependentlyAnimatedPropertyMetadata(object defaultValue, 
            PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback) 
            : base(defaultValue, propertyChangedCallback, coerceValueCallback) {}

        /// <summary>
        ///     Creates a new instance of this property metadata.  This method is used
        ///     when metadata needs to be cloned.  After CreateInstance is called the
        ///     framework will call Merge to merge metadata into the new instance.  
        ///     Deriving classes must override this and return a new instance of 
        ///     themselves.
        /// </summary>
        internal override PropertyMetadata CreateInstance() {
            return new IndependentlyAnimatedPropertyMetadata(DefaultValue);
        }
}
}
