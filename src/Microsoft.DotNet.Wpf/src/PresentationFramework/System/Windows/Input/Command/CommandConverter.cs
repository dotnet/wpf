// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Type Converter implementation for RoutedCommand
//
// For type converter spec please reference typeconverter.asp
//

using System;
using System.ComponentModel; // for TypeConverter
using System.Globalization; // for CultureInfo
using System.Reflection;
using MS.Utility;
using MS.Internal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Documents; // EditingCommands
using System.ComponentModel.Design.Serialization;

namespace System.Windows.Input
{
    /// <summary>
    /// CommandConverter - Converting between a string and an instance of Command, vice-versa
    /// </summary>
    public sealed class CommandConverter : TypeConverter
    {
        //------------------------------------------------------
        //
        // Constructors
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        // Public Methods
        //
        //------------------------------------------------------
        #region Public Methods
        ///<summary>
        ///CanConvertFrom()
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="sourceType">type to convert from</param>
        ///<returns>true if the given type can be converted, flase otherwise</returns>
        public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
        {
            // We can only handle string.
            if (sourceType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///<summary>
        ///TypeConverter method override.
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="destinationType">Type to convert to</param>
        ///<returns>true if conversion is possible</returns>
        public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
        {
            // We can only convert a "known" command into a string.  This logic
            // is mirrored in ConvertTo.
            //
            // Example: <Button Command="Copy"/>
            if (destinationType == typeof(string) )
            {
                RoutedCommand command = context != null ? context.Instance as RoutedCommand : null;

                if (command != null && command.OwnerType != null && IsKnownType(command.OwnerType))
                {
                    return true;
                }
            }

            return false;
        }



        ///<summary>
        ///ConvertFrom() -TypeConverter method override. using the givein name to return Command
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="culture">CultureInfo</param>
        ///<param name="source">Object to convert from</param>
        ///<returns>instance of Command</returns>
        public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object source )
        {
            if (source != null && source is string)
            {
                if ((string)source != String.Empty)
                {
                    String typeName, localName;

                    // Parse "ns:Class.Command" into "ns:Class", and "Command".
                    ParseUri((string)source, out typeName, out localName);

                    // Based on the prefix & type name, figure out the owner type.
                    Type ownerType = GetTypeFromContext(context, typeName);

                    // Find the command (this is shared with CommandValueSerializer).
                    ICommand command = ConvertFromHelper( ownerType, localName );

                    if (command != null)
                    {
                        return command;
                    }
                }
                else
                {
                    return null; // String.Empty <==> null , (for roundtrip cases where Command property values are null)
                }
            }
            throw GetConvertFromException(source);
        }



        internal static ICommand ConvertFromHelper(Type ownerType, string localName )
        {
            ICommand command = null;

            // If no namespaceUri or no prefix or no typename, defaulted to Known Commands.
            // there is no typename too, check for default in Known Commands.

            if (IsKnownType(ownerType) || ownerType == null )// not found
            {
                command = GetKnownCommand(localName, ownerType);
            }


            if( command == null && ownerType != null ) // not a known command

            {
                // Get them from Properties
                PropertyInfo propertyInfo = ownerType.GetProperty(localName, BindingFlags.Public | BindingFlags.Static);
                if (propertyInfo != null)
                    command = propertyInfo.GetValue(null, null) as ICommand;

                if (command == null)
                {
                    // Get them from Fields (ScrollViewer.PageDownCommand is a static readonly field
                    FieldInfo fieldInfo = ownerType.GetField(localName, BindingFlags.Static | BindingFlags.Public);
                    if (fieldInfo != null)
                        command = fieldInfo.GetValue(null) as ICommand;
                }
            }

            return command;
        }




        ///<summary>
        ///ConvertTo() - Serialization purposes, returns the string from Command.Name by adding ownerType.FullName
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="culture">CultureInfo</param>
        ///<param name="value">the object to convert from</param>
        ///<param name="destinationType">the type to convert to</param>
        ///<returns>string object, if the destination type is string
        /// </returns>
        public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType )
        {
            if (null == destinationType)
            {
                throw new ArgumentNullException("destinationType");
            }

            // We can only convert a "known" command into a string.  This logic
            // is mirrored in CanConvertTo.
            //
            // Example: <Button Command="Copy"/>
            if (destinationType == typeof(string))
            {
                RoutedCommand command = value as RoutedCommand;

                if (command != null && command.OwnerType != null && IsKnownType(command.OwnerType))
                {
                    return command.Name;
                }
                else
                {
                    // Should never happen.  Condition checked in CanConvertTo.
                    return String.Empty;
                }
            }
            
            throw GetConvertToException(value, destinationType);
        }
        #endregion Public Methods

        internal static bool IsKnownType( Type commandType )
        {
            if (commandType == typeof(ApplicationCommands) ||
            commandType == typeof(EditingCommands) ||
            commandType == typeof(NavigationCommands) ||
            commandType == typeof(ComponentCommands) ||
            commandType == typeof(MediaCommands))
            {
                return true;
            }
            return false;
        }

        //Utility helper to get the required information from the parserContext
        private Type GetTypeFromContext( ITypeDescriptorContext context, string typeName )
        {
            // Parser Context must exist to get the namespace info from prefix, if not, we assume it is known command.
            if (null != context && typeName != null)
            {
                IXamlTypeResolver xamlTypeResolver = (IXamlTypeResolver)context.GetService(typeof(IXamlTypeResolver));

                if( null != xamlTypeResolver )
                {
                    return xamlTypeResolver.Resolve( typeName );
                }
            }
            return null;
        }

        private void ParseUri( string source, out string typeName, out string localName )
        {
            typeName = null;
            localName = ((string)source).Trim();

            // split CommandName from its TypeName (e.g. ScrollViewer.PageDownCommand to Scrollviewerand PageDownCommand)
            int Offset = localName.LastIndexOf('.');
            if (Offset >= 0)
            {
                typeName = localName.Substring(0, Offset);
                localName = localName.Substring(Offset + 1);
            }
        }

        private static RoutedUICommand GetKnownCommand( string localName, Type ownerType )
        {
            RoutedUICommand knownCommand = null;
            bool searchAll = false;

            if (ownerType == null)
                searchAll = true;

            if (ownerType == typeof(NavigationCommands) || ((null == knownCommand) && searchAll))
            {
                switch (localName)
                {
                    case "BrowseBack":
                        knownCommand = NavigationCommands.BrowseBack;
                        break;
                    case "BrowseForward":
                        knownCommand = NavigationCommands.BrowseForward;
                        break;
                    case "BrowseHome":
                        knownCommand = NavigationCommands.BrowseHome;
                        break;
                    case "BrowseStop":
                        knownCommand = NavigationCommands.BrowseStop;
                        break;
                    case "Refresh":
                        knownCommand = NavigationCommands.Refresh;
                        break;
                    case "Favorites":
                        knownCommand = NavigationCommands.Favorites;
                        break;
                    case "Search":
                        knownCommand = NavigationCommands.Search;
                        break;
                    case "IncreaseZoom":
                        knownCommand = NavigationCommands.IncreaseZoom;
                        break;
                    case "DecreaseZoom":
                        knownCommand = NavigationCommands.DecreaseZoom;
                        break;
                    case "Zoom":
                        knownCommand = NavigationCommands.Zoom;
                        break;
                    case "NextPage":
                        knownCommand = NavigationCommands.NextPage;
                        break;
                    case "PreviousPage":
                        knownCommand = NavigationCommands.PreviousPage;
                        break;
                    case "FirstPage":
                        knownCommand = NavigationCommands.FirstPage;
                        break;
                    case "LastPage":
                        knownCommand = NavigationCommands.LastPage;
                        break;
                    case "GoToPage":
                        knownCommand = NavigationCommands.GoToPage;
                        break;
                    case "NavigateJournal":
                        knownCommand = NavigationCommands.NavigateJournal;
                        break;
                }
            }

            if (ownerType == typeof(ApplicationCommands) || ((null == knownCommand) && searchAll))
            {
                switch (localName)
                {
                    case "Cut":
                        knownCommand = ApplicationCommands.Cut;
                        break;
                    case "Copy":
                        knownCommand = ApplicationCommands.Copy;
                        break;
                    case "Paste":
                        knownCommand = ApplicationCommands.Paste;
                        break;
                    case "Undo":
                        knownCommand = ApplicationCommands.Undo;
                        break;
                    case "Redo":
                        knownCommand = ApplicationCommands.Redo;
                        break;
                    case "Delete":
                        knownCommand = ApplicationCommands.Delete;
                        break;
                    case "Find":
                        knownCommand = ApplicationCommands.Find;
                        break;
                    case "Replace":
                        knownCommand = ApplicationCommands.Replace;
                        break;
                    case "Help":
                        knownCommand = ApplicationCommands.Help;
                        break;
                    case "New":
                        knownCommand = ApplicationCommands.New;
                        break;
                    case "Open":
                        knownCommand = ApplicationCommands.Open;
                        break;
                    case "Save":
                        knownCommand = ApplicationCommands.Save;
                        break;
                    case "SaveAs":
                        knownCommand = ApplicationCommands.SaveAs;
                        break;
                    case "Close":
                        knownCommand = ApplicationCommands.Close;
                        break;
                    case "Print":
                        knownCommand = ApplicationCommands.Print;
                        break;
                    case "CancelPrint":
                        knownCommand = ApplicationCommands.CancelPrint;
                        break;
                    case "PrintPreview":
                        knownCommand = ApplicationCommands.PrintPreview;
                        break;
                    case "Properties":
                        knownCommand = ApplicationCommands.Properties;
                        break;
                    case "ContextMenu":
                        knownCommand = ApplicationCommands.ContextMenu;
                        break;
                    case "CorrectionList":
                        knownCommand = ApplicationCommands.CorrectionList;
                        break;
                    case "SelectAll":
                        knownCommand = ApplicationCommands.SelectAll;
                        break;
                    case "Stop":
                        knownCommand = ApplicationCommands.Stop;
                        break;
                    case "NotACommand":
                        knownCommand = ApplicationCommands.NotACommand;
                        break;
                }
            }

            if (ownerType == typeof(ComponentCommands) || ((null == knownCommand) && searchAll))
            {
                switch (localName)
                {
                    case "ScrollPageLeft":
                        knownCommand = ComponentCommands.ScrollPageLeft;
                        break;
                    case "ScrollPageRight":
                        knownCommand = ComponentCommands.ScrollPageRight;
                        break;
                    case "ScrollPageUp":
                        knownCommand = ComponentCommands.ScrollPageUp;
                        break;
                    case "ScrollPageDown":
                        knownCommand = ComponentCommands.ScrollPageDown;
                        break;
                    case "ScrollByLine":
                        knownCommand = ComponentCommands.ScrollByLine;
                        break;
                    case "MoveLeft":
                        knownCommand = ComponentCommands.MoveLeft;
                        break;
                    case "MoveRight":
                        knownCommand = ComponentCommands.MoveRight;
                        break;
                    case "MoveUp":
                        knownCommand = ComponentCommands.MoveUp;
                        break;
                    case "MoveDown":
                        knownCommand = ComponentCommands.MoveDown;
                        break;
                    case "ExtendSelectionUp":
                        knownCommand = ComponentCommands.ExtendSelectionUp;
                        break;
                    case "ExtendSelectionDown":
                        knownCommand = ComponentCommands.ExtendSelectionDown;
                        break;
                    case "ExtendSelectionLeft":
                        knownCommand = ComponentCommands.ExtendSelectionLeft;
                        break;
                    case "ExtendSelectionRight":
                        knownCommand = ComponentCommands.ExtendSelectionRight;
                        break;
                    case "MoveToHome":
                        knownCommand = ComponentCommands.MoveToHome;
                        break;
                    case "MoveToEnd":
                        knownCommand = ComponentCommands.MoveToEnd;
                        break;
                    case "MoveToPageUp":
                        knownCommand = ComponentCommands.MoveToPageUp;
                        break;
                    case "MoveToPageDown":
                        knownCommand = ComponentCommands.MoveToPageDown;
                        break;
                    case "SelectToHome":
                        knownCommand = ComponentCommands.SelectToHome;
                        break;
                    case "SelectToEnd":
                        knownCommand = ComponentCommands.SelectToEnd;
                        break;
                    case "SelectToPageDown":
                        knownCommand = ComponentCommands.SelectToPageDown;
                        break;
                    case "SelectToPageUp":
                        knownCommand = ComponentCommands.SelectToPageUp;
                        break;
                    case "MoveFocusUp":
                        knownCommand = ComponentCommands.MoveFocusUp;
                        break;
                    case "MoveFocusDown":
                        knownCommand = ComponentCommands.MoveFocusDown;
                        break;
                    case "MoveFocusBack":
                        knownCommand = ComponentCommands.MoveFocusBack;
                        break;
                    case "MoveFocusForward":
                        knownCommand = ComponentCommands.MoveFocusForward;
                        break;
                    case "MoveFocusPageUp":
                        knownCommand = ComponentCommands.MoveFocusPageUp;
                        break;
                    case "MoveFocusPageDown":
                        knownCommand = ComponentCommands.MoveFocusPageDown;
                        break;
                }
            }

            if (ownerType == typeof(EditingCommands) || ((null == knownCommand )&& searchAll))
            {
                switch (localName)
                {
                    case "ToggleInsert":
                        knownCommand = EditingCommands.ToggleInsert;
                        break;
                    case "Delete":
                        knownCommand = EditingCommands.Delete;
                        break;
                    case "Backspace":
                        knownCommand = EditingCommands.Backspace;
                        break;
                    case "DeleteNextWord":
                        knownCommand = EditingCommands.DeleteNextWord;
                        break;
                    case "DeletePreviousWord":
                        knownCommand = EditingCommands.DeletePreviousWord;
                        break;
                    case "EnterParagraphBreak":
                        knownCommand = EditingCommands.EnterParagraphBreak;
                        break;
                    case "EnterLineBreak":
                        knownCommand = EditingCommands.EnterLineBreak;
                        break;
                    case "TabForward":
                        knownCommand = EditingCommands.TabForward;
                        break;
                    case "TabBackward":
                        knownCommand = EditingCommands.TabBackward;
                        break;
                    case "MoveRightByCharacter":
                        knownCommand = EditingCommands.MoveRightByCharacter;
                        break;
                    case "MoveLeftByCharacter":
                        knownCommand = EditingCommands.MoveLeftByCharacter;
                        break;
                    case "MoveRightByWord":
                        knownCommand = EditingCommands.MoveRightByWord;
                        break;
                    case "MoveLeftByWord":
                        knownCommand = EditingCommands.MoveLeftByWord;
                        break;
                    case "MoveDownByLine":
                        knownCommand = EditingCommands.MoveDownByLine;
                        break;
                    case "MoveUpByLine":
                        knownCommand = EditingCommands.MoveUpByLine;
                        break;
                    case "MoveDownByParagraph":
                        knownCommand = EditingCommands.MoveDownByParagraph;
                        break;
                    case "MoveUpByParagraph":
                        knownCommand = EditingCommands.MoveUpByParagraph;
                        break;
                    case "MoveDownByPage":
                        knownCommand = EditingCommands.MoveDownByPage;
                        break;
                    case "MoveUpByPage":
                        knownCommand = EditingCommands.MoveUpByPage;
                        break;
                    case "MoveToLineStart":
                        knownCommand = EditingCommands.MoveToLineStart;
                        break;
                    case "MoveToLineEnd":
                        knownCommand = EditingCommands.MoveToLineEnd;
                        break;
                    case "MoveToDocumentStart":
                        knownCommand = EditingCommands.MoveToDocumentStart;
                        break;
                    case "MoveToDocumentEnd":
                        knownCommand = EditingCommands.MoveToDocumentEnd;
                        break;
                    case "SelectRightByCharacter":
                        knownCommand = EditingCommands.SelectRightByCharacter;
                        break;
                    case "SelectLeftByCharacter":
                        knownCommand = EditingCommands.SelectLeftByCharacter;
                        break;
                    case "SelectRightByWord":
                        knownCommand = EditingCommands.SelectRightByWord;
                        break;
                    case "SelectLeftByWord":
                        knownCommand = EditingCommands.SelectLeftByWord;
                        break;
                    case "SelectDownByLine":
                        knownCommand = EditingCommands.SelectDownByLine;
                        break;
                    case "SelectUpByLine":
                        knownCommand = EditingCommands.SelectUpByLine;
                        break;
                    case "SelectDownByParagraph":
                        knownCommand = EditingCommands.SelectDownByParagraph;
                        break;
                    case "SelectUpByParagraph":
                        knownCommand = EditingCommands.SelectUpByParagraph;
                        break;
                    case "SelectDownByPage":
                        knownCommand = EditingCommands.SelectDownByPage;
                        break;
                    case "SelectUpByPage":
                        knownCommand = EditingCommands.SelectUpByPage;
                        break;
                    case "SelectToLineStart":
                        knownCommand = EditingCommands.SelectToLineStart;
                        break;
                    case "SelectToLineEnd":
                        knownCommand = EditingCommands.SelectToLineEnd;
                        break;
                    case "SelectToDocumentStart":
                        knownCommand = EditingCommands.SelectToDocumentStart;
                        break;
                    case "SelectToDocumentEnd":
                        knownCommand = EditingCommands.SelectToDocumentEnd;
                        break;
                    case "ToggleBold":
                        knownCommand = EditingCommands.ToggleBold;
                        break;
                    case "ToggleItalic":
                        knownCommand = EditingCommands.ToggleItalic;
                        break;
                    case "ToggleUnderline":
                        knownCommand = EditingCommands.ToggleUnderline;
                        break;
                    case "ToggleSubscript":
                        knownCommand = EditingCommands.ToggleSubscript;
                        break;
                    case "ToggleSuperscript":
                        knownCommand = EditingCommands.ToggleSuperscript;
                        break;
                    case "IncreaseFontSize":
                        knownCommand = EditingCommands.IncreaseFontSize;
                        break;
                    case "DecreaseFontSize":
                        knownCommand = EditingCommands.DecreaseFontSize;
                        break;

                    // BEGIN Application Compatibility Note
                    // The following commands are internal, but they are exposed publicly
                    // from our command converter.  We cannot change this behavior
                    // because it is well documented.  For example, in the
                    // "WPF XAML Vocabulary Specification 2006" found here:
                    // http://msdn.microsoft.com/en-us/library/dd361848(PROT.10).aspx
                    case "ApplyFontSize":
                        knownCommand = EditingCommands.ApplyFontSize;
                        break;
                    case "ApplyFontFamily":
                        knownCommand = EditingCommands.ApplyFontFamily;
                        break;
                    case "ApplyForeground":
                        knownCommand = EditingCommands.ApplyForeground;
                        break;
                    case "ApplyBackground":
                        knownCommand = EditingCommands.ApplyBackground;
                        break;
                    // END Application Compatibility Note
                    
                    case "AlignLeft":
                        knownCommand = EditingCommands.AlignLeft;
                        break;
                    case "AlignCenter":
                        knownCommand = EditingCommands.AlignCenter;
                        break;
                    case "AlignRight":
                        knownCommand = EditingCommands.AlignRight;
                        break;
                    case "AlignJustify":
                        knownCommand = EditingCommands.AlignJustify;
                        break;
                    case "ToggleBullets":
                        knownCommand = EditingCommands.ToggleBullets;
                        break;
                    case "ToggleNumbering":
                        knownCommand = EditingCommands.ToggleNumbering;
                        break;
                    case "IncreaseIndentation":
                        knownCommand = EditingCommands.IncreaseIndentation;
                        break;
                    case "DecreaseIndentation":
                        knownCommand = EditingCommands.DecreaseIndentation;
                        break;
                    case "CorrectSpellingError":
                        knownCommand = EditingCommands.CorrectSpellingError;
                        break;
                    case "IgnoreSpellingError":
                        knownCommand = EditingCommands.IgnoreSpellingError;
                        break;
                }
            }

            if (ownerType == typeof(MediaCommands) || ((null == knownCommand) && searchAll))
            {
                switch (localName)
                {
                    case "Play":
                        knownCommand = MediaCommands.Play;
                        break;
                    case "Pause":
                        knownCommand = MediaCommands.Pause;
                        break;
                    case "Stop":
                        knownCommand = MediaCommands.Stop;
                        break;
                    case "Record":
                        knownCommand = MediaCommands.Record;
                        break;
                    case "NextTrack":
                        knownCommand = MediaCommands.NextTrack;
                        break;
                    case "PreviousTrack":
                        knownCommand = MediaCommands.PreviousTrack;
                        break;
                    case "FastForward":
                        knownCommand = MediaCommands.FastForward;
                        break;
                    case "Rewind":
                        knownCommand = MediaCommands.Rewind;
                        break;
                    case "ChannelUp":
                        knownCommand = MediaCommands.ChannelUp;
                        break;
                    case "ChannelDown":
                        knownCommand = MediaCommands.ChannelDown;
                        break;
                    case "TogglePlayPause":
                        knownCommand = MediaCommands.TogglePlayPause;
                        break;
                    case "IncreaseVolume":
                        knownCommand = MediaCommands.IncreaseVolume;
                        break;
                    case "DecreaseVolume":
                        knownCommand = MediaCommands.DecreaseVolume;
                        break;
                    case "MuteVolume":
                        knownCommand = MediaCommands.MuteVolume;
                        break;
                    case "IncreaseTreble":
                        knownCommand = MediaCommands.IncreaseTreble;
                        break;
                    case "DecreaseTreble":
                        knownCommand = MediaCommands.DecreaseTreble;
                        break;
                    case "IncreaseBass":
                        knownCommand = MediaCommands.IncreaseBass;
                        break;
                    case "DecreaseBass":
                        knownCommand = MediaCommands.DecreaseBass;
                        break;
                    case "BoostBass":
                        knownCommand = MediaCommands.BoostBass;
                        break;
                    case "IncreaseMicrophoneVolume":
                        knownCommand = MediaCommands.IncreaseMicrophoneVolume;
                        break;
                    case "DecreaseMicrophoneVolume":
                        knownCommand = MediaCommands.DecreaseMicrophoneVolume;
                        break;
                    case "MuteMicrophoneVolume":
                        knownCommand = MediaCommands.MuteMicrophoneVolume;
                        break;
                    case "ToggleMicrophoneOnOff":
                        knownCommand = MediaCommands.ToggleMicrophoneOnOff;
                        break;
                    case "Select":
                        knownCommand = MediaCommands.Select;
                        break;
                }
            }


            #if DEBUG
            if( knownCommand == null )
            {
                if( ownerType != null )
                    VerifyCommandDoesntExist( ownerType, localName );
                else
                {
                    VerifyCommandDoesntExist( typeof(NavigationCommands), localName );
                    VerifyCommandDoesntExist( typeof(ApplicationCommands), localName );
                    VerifyCommandDoesntExist( typeof(MediaCommands), localName );
                    VerifyCommandDoesntExist( typeof(EditingCommands), localName );
                    VerifyCommandDoesntExist( typeof(ComponentCommands), localName );
                }

            }
            #endif



            return knownCommand;
        }

        internal static object GetKnownControlCommand(Type ownerType, string commandName)
        {
            if (ownerType == typeof(ScrollBar))
            {
                switch (commandName)
                {
                    case "LineUpCommand":
                        return ScrollBar.LineUpCommand;

                    case "LineDownCommand":
                        return ScrollBar.LineDownCommand;

                    case "LineLeftCommand":
                        return ScrollBar.LineLeftCommand;

                    case "LineRightCommand":
                        return ScrollBar.LineRightCommand;

                    case "PageUpCommand":
                        return ScrollBar.PageUpCommand;

                    case "PageDownCommand":
                        return ScrollBar.PageDownCommand;

                    case "PageLeftCommand":
                        return ScrollBar.PageLeftCommand;

                    case "PageRightCommand":
                        return ScrollBar.PageRightCommand;
                }
            }
            else if (ownerType == typeof(Slider))
            {
                switch (commandName)
                {
                    case "IncreaseLarge":
                        return Slider.IncreaseLarge;

                    case "DecreaseLarge":
                        return Slider.DecreaseLarge;
                }
            }

            return null;
        }

        #if DEBUG
        static void VerifyCommandDoesntExist( Type type, string name )
        {
            PropertyInfo propertyInfo = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            System.Diagnostics.Debug.Assert( propertyInfo == null, "KnownCommand isn't known to CommandConverter.GetKnownCommand" );

            FieldInfo fieldInfo = type.GetField(name, BindingFlags.Static | BindingFlags.Public);
            System.Diagnostics.Debug.Assert( fieldInfo == null, "KnownCommand isn't known to CommandConverter.GetKnownCommand" );
         }
        #endif
    }
}
