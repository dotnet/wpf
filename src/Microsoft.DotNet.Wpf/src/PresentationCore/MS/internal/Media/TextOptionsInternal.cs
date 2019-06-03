// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: TextOptions groups attached properties that affect the way 
//              WPF displays text such as TextFormattingMode 
//              and TextRenderingMode.
//

using System;
using System.Windows;
using System.Windows.Media;
using MS.Internal.PresentationCore;

namespace MS.Internal.Media
{
    /// <summary>
    /// Provide access to text options of element in syntax of TextOptions.xxx = yyy;
    /// Actual data is stored in the owner.
    /// </summary>
    [FriendAccessAllowed]   // used by Framework
    internal static class TextOptionsInternal
    {
        #region Dependency Properties

        /// <summary> Text hinting property </summary>
        [FriendAccessAllowed]   // used by Framework
        internal static readonly DependencyProperty TextHintingModeProperty = 
                DependencyProperty.RegisterAttached(
                        "TextHintingMode",
                        typeof(TextHintingMode),
                        typeof(TextOptionsInternal),
                        new UIPropertyMetadata(TextHintingMode.Auto),
                        new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsTextHintingModeValid));                        

        #endregion Dependency Properties

        
        #region Attached Properties Setters

        [FriendAccessAllowed]   // used by Framework
        public static void SetTextHintingMode(DependencyObject element, TextHintingMode value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(TextHintingModeProperty, value);
        }

        [FriendAccessAllowed]   // used by Framework
        public static TextHintingMode GetTextHintingMode(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (TextHintingMode)element.GetValue(TextHintingModeProperty);
        }

        #endregion Attached Groperties Getters and Setters
    }
}
