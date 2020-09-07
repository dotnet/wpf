// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Localization comments related class
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

#if !PBTCOMPILER
using System.Windows;
#endif

using MS.Utility;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace MS.Internal.Globalization
{
    /// <remarks>
    /// Note: the class name and property name must be kept in sync'ed with
    /// Framework\System\Windows\Localization.cs file.
    /// Compiler checks for them by literal string comparisons.
    /// </remarks>
    internal static class LocComments
    {
        /// <summary>
        /// Helper function to determine whether this is the Localization.Attribute attached proeprty
        /// </summary>
        /// <param name="type">type name</param>
        /// <param name="property">property name</param>
        internal static bool IsLocLocalizabilityProperty(string type, string property)
        {
            return "Attributes" == property
                && "System.Windows.Localization" == type;
        }

        /// <summary>
        /// Helper function to determind whether this is the Localization.Comments attached proeprty
        /// </summary>
        /// <param name="type">type name</param>
        /// <param name="property">property name</param>
        internal static bool IsLocCommentsProperty(string type, string property)
        {
            return "Comments" == property
                && "System.Windows.Localization" == type;
        }

        /// <summary>
        /// Helper function to parse the Localization.Attributes value into multiple pairs
        /// </summary>
        /// <param name="input">content of Localization.Attributes</param>
        internal static PropertyComment[] ParsePropertyLocalizabilityAttributes(string input)
        {
            PropertyComment[] pairs = ParsePropertyComments(input);

            if (pairs != null)
            {
                for (int i = 0; i < pairs.Length; i++)
                {
                    pairs[i].Value = LookupAndSetLocalizabilityAttribute((string)pairs[i].Value);
                }
            }

            return pairs;
        }

        /// <summary>
        /// Helper function to parse the Localization.Comments value into multiple pairs
        /// </summary>
        /// <param name="input">content of Localization.Comments</param>
        internal static PropertyComment[] ParsePropertyComments(string input)
        {
            //
            // Localization comments consist of repeating "[PropertyName]([Value])"
            // e.g. $Content (abc) FontSize(def)
            //
            if (input == null) return null;

            List<PropertyComment> tokens = new List<PropertyComment>(8);
            StringBuilder tokenBuffer = new StringBuilder();
            PropertyComment currentPair = new PropertyComment();
            bool escaped = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (currentPair.PropertyName == null)
                {
                    // parsing "PropertyName" section
                    if (Char.IsWhiteSpace(input[i]) && !escaped)
                    {
                        if (tokenBuffer.Length > 0)
                        {
                            // terminate the PropertyName by an unesacped whitespace
                            currentPair.PropertyName = tokenBuffer.ToString();
                            tokenBuffer = new StringBuilder();
                        }

                        // else ignore whitespace at the beginning of the PropertyName name
                    }
                    else
                    {
                        if (input[i] == CommentStart && !escaped)
                        {
                            if (i > 0)
                            {
                                // terminate the PropertyName by an unescaped CommentStart char
                                currentPair.PropertyName = tokenBuffer.ToString();
                                tokenBuffer = new StringBuilder();
                                i--; // put back this char and continue
                            }
                            else
                            {
                                // can't begin with unescaped comment start char.
                                throw new FormatException(SR.Get(SRID.InvalidLocCommentTarget, input));
                            }
                        }
                        else if (input[i] == EscapeChar && !escaped)
                        {
                            escaped = true;
                        }
                        else
                        {
                            tokenBuffer.Append(input[i]);
                            escaped = false;
                        }
                    }
                }
                else
                {
                    // parsing the "Value" part
                    if (tokenBuffer.Length == 0)
                    {
                        if (input[i] == CommentStart && !escaped)
                        {
                            // comment must begin with unescaped CommentStart
                            tokenBuffer.Append(input[i]);
                            escaped = false;
                        }
                        else if (!Char.IsWhiteSpace(input[i]))
                        {
                            // else, only white space is allows before an unescaped comment start char
                            throw new FormatException(SR.Get(SRID.InvalidLocCommentValue, currentPair.PropertyName, input));
                        }
                    }
                    else
                    {
                        // inside the comment
                        if (input[i] == CommentEnd)
                        {
                            if (!escaped)
                            {
                                // terminated by unescaped Comment
                                currentPair.Value = tokenBuffer.ToString().Substring(1);
                                tokens.Add(currentPair);
                                tokenBuffer = new StringBuilder();
                                currentPair = new PropertyComment();
                            }
                            else
                            {
                                // continue on escaped end char
                                tokenBuffer.Append(input[i]);
                                escaped = false;
                            }
                        }
                        else if (input[i] == CommentStart && !escaped)
                        {
                            // throw if there is unescape start in comment
                            throw new FormatException(SR.Get(SRID.InvalidLocCommentValue, currentPair.PropertyName, input));
                        }
                        else
                        {
                            // comment
                            if (input[i] == EscapeChar && !escaped)
                            {
                                escaped = true;
                            }
                            else
                            {
                                tokenBuffer.Append(input[i]);
                                escaped = false;
                            }
                        }
                    }
                }
            }

            if (currentPair.PropertyName != null || tokenBuffer.Length != 0)
            {
                // unmatched PropertyName and Value pair
                throw new FormatException(SR.Get(SRID.UnmatchedLocComment, input));
            }

            return tokens.ToArray();
        }

        //------------------------------
        // Private methods
        //------------------------------
        private static LocalizabilityGroup LookupAndSetLocalizabilityAttribute(string input)
        {
            //
            // For Localization.Attributes, values are seperated by spaces, e.g.
            // $Content (Modifiable Readable)
            // We are breaking the content and convert it to corresponding enum values.
            //
            LocalizabilityGroup attributeGroup = new LocalizabilityGroup();

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsWhiteSpace(input[i]))
                {
                    if (builder.Length > 0)
                    {
                        ParseLocalizabilityString(
                            builder.ToString(),
                            attributeGroup
                            );

                        builder = new StringBuilder();
                    }
                }
                else
                {
                    builder.Append(input[i]);
                }
            }

            if (builder.Length > 0)
            {
                ParseLocalizabilityString(
                    builder.ToString(),
                    attributeGroup
                    );
            }

            return attributeGroup;
        }

        private static void ParseLocalizabilityString(
            string value,
            LocalizabilityGroup attributeGroup
            )
        {
            int enumValue;
            if (ReadabilityIndexTable.TryGet(value, out enumValue))
            {
                attributeGroup.Readability = (Readability)enumValue;
                return;
            }

            if (ModifiabilityIndexTable.TryGet(value, out enumValue))
            {
                attributeGroup.Modifiability = (Modifiability)enumValue;
                return;
            }

            if (LocalizationCategoryIndexTable.TryGet(value, out enumValue))
            {
                attributeGroup.Category = (LocalizationCategory)enumValue;
                return;
            }

            throw new FormatException(SR.Get(SRID.InvalidLocalizabilityValue, value));
        }


        private const char CommentStart = '(';
        private const char CommentEnd = ')';
        private const char EscapeChar = '\\';

        // loc comments file recognized element
        internal const string LocDocumentRoot = "LocalizableAssembly";
        internal const string LocResourcesElement = "LocalizableFile";
        internal const string LocCommentsElement = "LocalizationDirectives";
        internal const string LocFileNameAttribute = "Name";
        internal const string LocCommentIDAttribute = "Uid";
        internal const string LocCommentsAttribute = "Comments";
        internal const string LocLocalizabilityAttribute = "Attributes";

        //
        // Tables that map the enum string names into their enum indices.
        // Each index table contains the list of enum names in the exact
        // order of their enum indices. They must be consistent with the enum
        // declarations in core.
        //
        private static EnumNameIndexTable ReadabilityIndexTable = new EnumNameIndexTable(
            "Readability.",
            new string[] {
                "Unreadable",
                "Readable",
                "Inherit"
                }
            );

        private static EnumNameIndexTable ModifiabilityIndexTable = new EnumNameIndexTable(
            "Modifiability.",
            new string[] {
                "Unmodifiable",
                "Modifiable",
                "Inherit"
                }
            );

        private static EnumNameIndexTable LocalizationCategoryIndexTable = new EnumNameIndexTable(
            "LocalizationCategory.",
            new string[] {
                "None",
                "Text",
                "Title",
                "Label",
                "Button",
                "CheckBox",
                "ComboBox",
                "ListBox",
                "Menu",
                "RadioButton",
                "ToolTip",
                "Hyperlink",
                "TextFlow",
                "XmlData",
                "Font",
                "Inherit",
                "Ignore",
                "NeverLocalize"
                }
            );

        /// <summary>
        /// A simple table that maps a enum name into its enum index. The string can be
        /// the enum value's name by itself or preceeded by the enum's prefix to reduce
        /// ambiguity.
        /// </summary>
        private class EnumNameIndexTable
        {
            private string _enumPrefix;
            private string[] _enumNames;

            internal EnumNameIndexTable(
                string enumPrefix,
                string[] enumNames
                )
            {
                Debug.Assert(enumPrefix != null && enumNames != null);
                _enumPrefix = enumPrefix;
                _enumNames = enumNames;
            }

            internal bool TryGet(string enumName, out int enumIndex)
            {
                enumIndex = 0;
                if (enumName.StartsWith(_enumPrefix, StringComparison.Ordinal))
                {
                    // get the real enum name after the prefix.
                    enumName = enumName.Substring(_enumPrefix.Length);
                }

                for (int i = 0; i < _enumNames.Length; i++)
                {
                    if (string.Compare(enumName, _enumNames[i], StringComparison.Ordinal) == 0)
                    {
                        enumIndex = i;
                        return true;
                    }
                }
                return false;
            }
        }
    }

    internal class PropertyComment
    {
        string _target;
        object _value;

        internal PropertyComment() { }

        internal string PropertyName
        {
            get { return _target; }
            set { _target = value; }
        }

        internal object Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }

    internal class LocalizabilityGroup
    {
        private const int InvalidValue = -1;

        internal Modifiability Modifiability;
        internal Readability Readability;
        internal LocalizationCategory Category;

        internal LocalizabilityGroup()
        {
            Modifiability = (Modifiability)InvalidValue;
            Readability = (Readability)InvalidValue;
            Category = (LocalizationCategory)InvalidValue;
        }

#if !PBTCOMPILER
        // Helper to override a localizability attribute. Not needed for compiler
        internal LocalizabilityAttribute Override(LocalizabilityAttribute attribute)
        {
            Modifiability modifiability = attribute.Modifiability;
            Readability readability = attribute.Readability;
            LocalizationCategory category = attribute.Category;

            bool overridden = false;
            if (((int)Modifiability) != InvalidValue)
            {
                modifiability = Modifiability;
                overridden = true;
            }

            if (((int)Readability) != InvalidValue)
            {
                readability = Readability;
                overridden = true;
            }

            if (((int)Category) != InvalidValue)
            {
                category = Category;
                overridden = true;
            }

            if (overridden)
            {
                attribute = new LocalizabilityAttribute(category);
                attribute.Modifiability = modifiability;
                attribute.Readability = readability;
            }

            return attribute;
        }
#endif
    }
}


