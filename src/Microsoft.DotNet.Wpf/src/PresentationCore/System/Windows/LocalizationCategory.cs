// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: LocalizationCategory enum used by LocalizabilityAttribute 
//

#if PBTCOMPILER
namespace MS.Internal.Globalization
#else
namespace System.Windows
#endif
{
    /// <summary>
    /// the category of the string values of each localizable resource
    /// </summary>
    // NOTE: Enum values must be made in sync with the enum parsing logic in 
    // Framework/MS/Internal/Globalization/LocalizationComments.cs    
#if PBTCOMPILER    
    internal enum LocalizationCategory
#else
    public enum LocalizationCategory
#endif    
    {
        /// <summary>
        /// None. For items that don't need to have a category
        /// </summary>
        None = 0,


        //---------------------------------
        // Well known types
        //---------------------------------
        /// <summary>
        /// DecriptiveText. Use it for long piece of text
        /// </summary>
        Text,

        /// <summary>
        /// TitleText. Use it for one line of text
        /// </summary>
        Title,

        /// <summary>
        /// LabelText. Use it for short text in labling controls.
        /// </summary>
        Label,


        /// <summary>
        /// Button. For Button control and similar classes
        /// </summary>
        Button,

        /// <summary>
        /// CheckBox. For CheckBox, CheckBoxItem and similar classes
        /// </summary>
        CheckBox,

        /// <summary>
        /// ComboBox. For ComboBox, ComboBoxItem and similar classes
        /// </summary>
        ComboBox,

        /// <summary>
        /// ListBox. For ListBox, ListBoxItem and similar classes
        /// </summary>
        ListBox,

        /// <summary>
        /// Menu. For Menu, MenuItem and similar classes
        /// </summary>
        Menu,

        /// <summary>
        /// RadioButton. For RadioButton, RadioButtonList and similar classes
        /// </summary>
        RadioButton,

        /// <summary>
        /// ToolTip. For tool tip control and similar classes
        /// </summary>
        ToolTip,

        /// <summary>
        /// Hyperlink. For hyperlink and similar classes
        /// </summary>
        Hyperlink ,

        /// <summary>
        /// TextFlow. For text panel and panels that can contain text.
        /// </summary>
        TextFlow,

        /// <summary>
        /// Xml data.
        /// </summary>
        XmlData,

        /// <summary>
        /// Font related data, font name, font size, etc.
        /// </summary>
        Font,

        //---------------------------------
        // Special types
        //--------------------------------- 

        /// <summary>
        /// The category inherits from the parent node.
        /// </summary>
        Inherit,

        /// <summary>
        /// "Ignore" indicates that value in baml should be treated as if it did not exsit in baml.
        /// </summary>
        Ignore,

        /// <summary>
        /// "NeverLocalize" means that content is not localized. Content includes the subtree. 
        /// </summary>
        NeverLocalize,               
    }
}
