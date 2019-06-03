// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A Component of TextEditor supporting Cut/Copy/Paste commands
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Globalization;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.ComponentModel;
    using System.Text;
    using System.Xml;
    using System.IO;
    using System.Collections; // ArrayList
    using System.Runtime.InteropServices;

    using System.Windows.Threading;
    using System.Windows.Input;
    using System.Windows.Controls; // ScrollChangedEventArgs
    using System.Windows.Controls.Primitives;  // CharacterCasing, TextBoxBase
    using System.Windows.Media;
    using System.Windows.Markup;

    using MS.Utility;
    using MS.Win32;
    using MS.Internal.Documents;
    using MS.Internal.Commands; // CommandHelpers

    /// <summary>
    /// Text editing service for controls.
    /// </summary>
    internal static class TextEditorCopyPaste
    {
        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        // Registers all text editing command handlers for a given control type
        /// <SecurityNote>
        /// Critical - elevates to associate a protected command (paste) with keyboard
        /// TreatAsSafe - Shift+Insert is the correct key binding, and therefore is
        ///               expected by the user.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void _RegisterClassHandlers(Type controlType, bool acceptsRichContent, bool readOnly, bool registerEventListeners)
        {
            CommandHelpers.RegisterCommandHandler(controlType, ApplicationCommands.Copy, new ExecutedRoutedEventHandler(OnCopy), new CanExecuteRoutedEventHandler(OnQueryStatusCopy), KeyGesture.CreateFromResourceStrings(SR.Get(SRID.KeyCopy), SR.Get(SRID.KeyCopyDisplayString)), KeyGesture.CreateFromResourceStrings(SR.Get(SRID.KeyCtrlInsert), SR.Get(SRID.KeyCtrlInsertDisplayString)));
            if (acceptsRichContent)
            {
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.CopyFormat, new ExecutedRoutedEventHandler(OnCopyFormat), new CanExecuteRoutedEventHandler(OnQueryStatusCopyFormat), SRID.KeyCopyFormat, SRID.KeyCopyFormatDisplayString);
            }
            if (!readOnly)
            {
                CommandHelpers.RegisterCommandHandler(controlType, ApplicationCommands.Cut, new ExecutedRoutedEventHandler(OnCut), new CanExecuteRoutedEventHandler(OnQueryStatusCut), KeyGesture.CreateFromResourceStrings(SR.Get(SRID.KeyCut), SR.Get(SRID.KeyCutDisplayString)), KeyGesture.CreateFromResourceStrings(SR.Get(SRID.KeyShiftDelete), SR.Get(SRID.KeyShiftDeleteDisplayString)));
                // temp vars to reduce code under elevation
                ExecutedRoutedEventHandler ExecutedRoutedEventHandler = new ExecutedRoutedEventHandler(OnPaste);
                CanExecuteRoutedEventHandler CanExecuteRoutedEventHandler = new CanExecuteRoutedEventHandler(OnQueryStatusPaste);
                InputGesture inputGesture = KeyGesture.CreateFromResourceStrings(SR.Get(SRID.KeyShiftInsert), SR.Get(SRID.KeyShiftInsertDisplayString));
                new UIPermission(UIPermissionClipboard.AllClipboard).Assert(); //BlessedAssert
                try
                {
                    CommandHelpers.RegisterCommandHandler(controlType, ApplicationCommands.Paste, ExecutedRoutedEventHandler, CanExecuteRoutedEventHandler, inputGesture);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }
                if (acceptsRichContent)
                {
                    CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.PasteFormat, new ExecutedRoutedEventHandler(OnPasteFormat), new CanExecuteRoutedEventHandler(OnQueryStatusPasteFormat), SRID.KeyPasteFormat, SRID.KeyPasteFormatDisplayString);
                }
            }
        }

        /// <summary>
        /// Creates DataObject for Copy and Drag operations
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code calls into SetData under an assert which has the ability to set xaml content on clipboard.
        /// </SecurityNote>
        [SecurityCritical]
        internal static DataObject _CreateDataObject(TextEditor This, bool isDragDrop)
        {
            DataObject dataObject;
            // Create the data object for drag and drop.
            //  We could provide more extensibility here -
            // by allowing application to create its own DataObject.
            // Without it our extensibility looks inconsistent:
            // the interface IDataObject suggests that you can
            // create your own implementation of it, but you
            // really cannot, because there is no way of
            // using it in our TextEditor.Copy/Drag.
            (new UIPermission(UIPermissionClipboard.AllClipboard)).Assert();//BlessedAssert
            try
            {
                dataObject = new DataObject();
            }
            finally
            {
                UIPermission.RevertAssert();
            }

            // Get plain text and copy it into the data object.
            string textString = This.Selection.Text;

            if (textString != String.Empty)
            {
                // Copy plain text into data object.
                // ConfirmDataFormatSetting rasies a public event - could throw recoverable exception.
                if (ConfirmDataFormatSetting(This.UiScope, dataObject, DataFormats.Text))
                {
                    CriticalSetDataWrapper(dataObject,DataFormats.Text, textString);
                }

                // Copy unicode text into data object.
                // ConfirmDataFormatSetting rasies a public event - could throw recoverable exception.
                if (ConfirmDataFormatSetting(This.UiScope, dataObject, DataFormats.UnicodeText))
                {
                    CriticalSetDataWrapper(dataObject,DataFormats.UnicodeText, textString);
                }
            }

            // Get the rtf and xaml text and then copy it into the data object after confirm data format.
            // We do this only if our content is rich
            if (This.AcceptsRichContent)
            {
                // This ensures that in the confines of partial trust RTF is not enabled.
                // We use unmanaged code permission over clipboard permission since
                // the latter is available in intranet zone and this is something that will
                // fail in intranet too.
                if (SecurityHelper.CheckUnmanagedCodePermission())
                {
                    // In FullTrust we allow all rich formats on the clipboard

                    Stream wpfContainerMemory = null;
                    // null wpfContainerMemory on entry means that container is optional
                    // and will be not created when there is no images in the range.

                    // Create in-memory wpf package, and serialize the content of selection into it
                    string xamlTextWithImages = WpfPayload.SaveRange(This.Selection, ref wpfContainerMemory, /*useFlowDocumentAsRoot:*/false);

                    if (xamlTextWithImages.Length > 0)
                    {
                        // ConfirmDataFormatSetting raises a public event - could throw recoverable exception.
                        if (wpfContainerMemory != null && ConfirmDataFormatSetting(This.UiScope, dataObject, DataFormats.XamlPackage))
                        {
                            dataObject.SetData(DataFormats.XamlPackage, wpfContainerMemory);
                        }

                        // ConfirmDataFormatSetting raises a public event - could throw recoverable exception.
                        if (ConfirmDataFormatSetting(This.UiScope, dataObject, DataFormats.Rtf))
                        {
                            // Convert xaml to rtf text to set rtf data into data object.
                            string rtfText = ConvertXamlToRtf(xamlTextWithImages, wpfContainerMemory);

                            if (rtfText != String.Empty)
                            {
                                dataObject.SetData(DataFormats.Rtf, rtfText, true);
                            }
                        }
                    }

                    // Add a CF_BITMAP if we have only one image selected.
                    Image image = This.Selection.GetUIElementSelected() as Image;
                    if (image != null && image.Source is System.Windows.Media.Imaging.BitmapSource)
                    {
                        dataObject.SetImage((System.Windows.Media.Imaging.BitmapSource)image.Source);
                    }
                }

                // Xaml format is availabe both in Full Trust and in Partial Trust
                // Need to re-serialize xaml to avoid image references within a container:
                StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);
                TextRangeSerialization.WriteXaml(xmlWriter, This.Selection, /*useFlowDocumentAsRoot:*/false, /*wpfPayload:*/null);
                string xamlText = stringWriter.ToString();
                //  Use WpfPayload.SaveRangeAsXaml method to produce correct image.Source properties.

                if (xamlText.Length > 0)
                {
                    // ConfirmDataFormatSetting rasies a public event - could throw recoverable exception.
                    if (ConfirmDataFormatSetting(This.UiScope, dataObject, DataFormats.Xaml))
                    {
                        // Place Xaml data onto the dataobject using safe setter
                        CriticalSetDataWrapper(dataObject, DataFormats.Xaml, xamlText);

                        // The dataobject itself must hold an information about permission set
                        // of the source appdomain. Set it there:

                        // Package permission set for the current appdomain
                        PermissionSet psCurrentAppDomain = SecurityHelper.ExtractAppDomainPermissionSetMinusSiteOfOrigin();
                        string permissionSetCurrentAppDomain = psCurrentAppDomain.ToString();
                        CriticalSetDataWrapper(dataObject, DataFormats.ApplicationTrust, permissionSetCurrentAppDomain);
                    }
                }
            }

            // Notify application about our data object preparation completion
            DataObjectCopyingEventArgs dataObjectCopyingEventArgs = new DataObjectCopyingEventArgs(dataObject, /*isDragDrop:*/isDragDrop);
            This.UiScope.RaiseEvent(dataObjectCopyingEventArgs);
            if (dataObjectCopyingEventArgs.CommandCancelled)
            {
                dataObject = null;
            }

            return dataObject;
        }

        /// <summary>
        /// Paste contents of data object into text selection
        /// </summary>
        /// <param name="This"></param>
        /// <param name="dataObject">
        /// data object containing data to paste
        /// </param>
        /// <param name="isDragDrop">
        /// </param>
        /// <returns>
        /// true if successful, false otherwise
        /// </returns>
        /// <SecurityNote>
        /// To disable paste in partial trust case,
        /// this function checks if the current call stack has the all clipboard permission.
        /// Critical: This code calls into AppDomain methods and enables xaml cut and paste
        /// TreatAsSafe: It has a demand for All Clipboard permissions
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal static bool _DoPaste(TextEditor This, IDataObject dataObject, bool isDragDrop)
        {
            // Don't try anything if the caller doesn't have the rights to read from the clipboard...
            //
            if (!SecurityHelper.CallerHasAllClipboardPermission()) return false;

            Invariant.Assert(dataObject != null);

            // Choose what format we are going to paste
            string formatToApply;
            bool pasted;

            pasted = false;

            // Get the default paste content applying format
            formatToApply = GetPasteApplyFormat(This, dataObject);

            DataObjectPastingEventArgs dataObjectPastingEventArgs;

            try
            {
                // Let the application to participate in Paste process
                dataObjectPastingEventArgs = new DataObjectPastingEventArgs(dataObject, isDragDrop, formatToApply);
            }
            catch (ArgumentException)
            {
                // Clipboard can be changed by set new or empty data during creating
                // DataObjectPastingEvent that check the representing of the
                // formatToApply. Do nothing if we encounter AgrumentException.
                return pasted;
            }

            // Public event call - could raise recoverable exception.
            This.UiScope.RaiseEvent(dataObjectPastingEventArgs);

            if (!dataObjectPastingEventArgs.CommandCancelled)
            {
                // When custom handler decides to suggest its own data,
                // it must create a new instance of DataObject and put it
                // into DataObjectPastingEventArgs.DataObject property.
                // Exisiting DataObject is on global Clipboard and can not be changed.
                // Here we need to get this potentially changed instance
                // of DataObject
                IDataObject dataObjectToApply = dataObjectPastingEventArgs.DataObject;

                formatToApply = dataObjectPastingEventArgs.FormatToApply;

                // Paste the content data(Text, Unicode, Xaml and Rtf) to the current text selection
                pasted = PasteContentData(This, dataObject, dataObjectToApply, formatToApply);
            }

            return pasted;
        }

        // Get the default paste content applying format
        internal static string GetPasteApplyFormat(TextEditor This, IDataObject dataObject)
        {
            string formatToApply;

            // Currently we won't allow DataFormats.Xaml on the partial trust.
            // GetDataPresent(DataFormats.Xaml)have a chance to register Xaml format
            // by calling the unmanaged code which is RegisterClipboardFormat.

            bool hasUnmanagedCodePermission = SecurityHelper.CheckUnmanagedCodePermission();

            if (This.AcceptsRichContent && hasUnmanagedCodePermission && dataObject.GetDataPresent(DataFormats.XamlPackage))
            {
                formatToApply = DataFormats.XamlPackage;
            }
            else if (This.AcceptsRichContent && dataObject.GetDataPresent(DataFormats.Xaml))
            {
                formatToApply = DataFormats.Xaml;
            }
            else if (This.AcceptsRichContent && hasUnmanagedCodePermission && dataObject.GetDataPresent(DataFormats.Rtf))
            {
                formatToApply = DataFormats.Rtf;
            }
            else if (dataObject.GetDataPresent(DataFormats.UnicodeText))
            {
                formatToApply = DataFormats.UnicodeText;
            }
            else if (dataObject.GetDataPresent(DataFormats.Text))
            {
                formatToApply = DataFormats.Text;
            }
            else if (This.AcceptsRichContent && hasUnmanagedCodePermission && dataObject is DataObject && ((DataObject)dataObject).ContainsImage())
            {
                formatToApply = DataFormats.Bitmap;
            }
            else
            {
                // Even if we do not see any recognizable formats,
                // we continue the process because application custom
                // paste needs it and may do something useful.
                formatToApply = String.Empty;
            }

            return formatToApply;
        }

        /// <summary>
        /// Cut worker.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Sets data on the clipboard and accepts a parameter which indicates 
        ///            whether or not this action was user-initiated.
        /// </SecurityNote>
        [SecurityCritical]
        internal static void Cut(TextEditor This, bool userInitiated)
        {
            if (userInitiated)
            {
                // Fail silently if the app explicitly denies clipboard access.
                try
                {
                    new UIPermission(UIPermissionClipboard.OwnClipboard).Demand();
                }
                catch (SecurityException)
                {
                    return;
                }
            }
            else if (!SecurityHelper.CallerHasAllClipboardPermission())
            {
                // Fail silently if we don't have clipboard permission.
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            TextEditorTyping._BreakTypingSequence(This);

            if (This.Selection != null && !This.Selection.IsEmpty)
            {
                // Copy content onto the clipboard

                // Note: _CreateDataObject raises a public event which might throw a recoverable exception.
                DataObject dataObject = TextEditorCopyPaste._CreateDataObject(This, /*isDragDrop:*/false);

                if (dataObject != null)
                {
                    try
                    {
                        // The copy command was not terminated by application
                        // One of reason should be the opening fail of Clipboard by the destroyed hwnd.
                        Clipboard.CriticalSetDataObject(dataObject, true);
                    }
                    catch (ExternalException)
                        when (!FrameworkCompatibilityPreferences.ShouldThrowOnCopyOrCutFailure)
                    {
                        // Clipboard is failed to set the data object.
                        return;
                    }

                    // Delete selected content
                    using (This.Selection.DeclareChangeBlock())
                    {
                        // Forget previously suggested horizontal position
                        TextEditorSelection._ClearSuggestedX(This);
                        This.Selection.Text = String.Empty;

                        // Clear springload formatting
                        if (This.Selection is TextSelection)
                        {
                            ((TextSelection)This.Selection).ClearSpringloadFormatting();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copy worker.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Sets data on the clipboard and accepts a parameter which indicates 
        ///            whether or not this action was user-initiated.
        /// </SecurityNote>
        [SecurityCritical]
        internal static void Copy(TextEditor This, bool userInitiated)
        {
            if (userInitiated)
            {
                // Fail silently if the app explicitly denies clipboard access.
                try
                {
                    new UIPermission(UIPermissionClipboard.OwnClipboard).Demand();
                }
                catch (SecurityException)
                {
                    return;
                }
            }
            else if (!SecurityHelper.CallerHasAllClipboardPermission())
            {
                // Fail silently if we don't have clipboard permission.
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            TextEditorTyping._BreakTypingSequence(This);

            if (This.Selection != null && !This.Selection.IsEmpty)
            {
                // Note: _CreateDataObject raises a public event which might throw a recoverable exception.
                DataObject dataObject = TextEditorCopyPaste._CreateDataObject(This, /*isDragDrop:*/false);

                if (dataObject != null)
                {
                    try
                    {
                        // The copy command was not terminated by application
                        // One of reason should be the opening fail of Clipboard by the destroyed hwnd.
                        Clipboard.CriticalSetDataObject(dataObject, true);
                    }
                    catch (ExternalException) 
                        when (!FrameworkCompatibilityPreferences.ShouldThrowOnCopyOrCutFailure)
                    {
                        // Clipboard is failed to set the data object.
                        return;
                    }
                }
            }

            // Do not clear springload formatting
        }

        /// <summary>
        /// Paste worker.
        /// </summary>
        /// <SecurityNote>
        /// Critical:To disable paste in partial trust case,
        /// TreatAsSafe: this function checks if the current call stack has the all clipboard permission.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void Paste(TextEditor This)
        {
            // Don't try anything if the caller doesn't have the rights to read from the clipboard...
            if (!SecurityHelper.CallerHasAllClipboardPermission())
            {
                return;
            }

            if (This.Selection.IsTableCellRange)
            {
                //  We do not support clipboard for table selection so far
                // Word behavior: When source range is text segment then this segment is pasted
                // into each table cell overriding its current content.
                // If source range is table range then it is repeated on target -
                // cell by cell in circular manner.
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            TextEditorTyping._BreakTypingSequence(This);

            // Get DataObject from the Clipboard
            IDataObject dataObject;
            try
            {
                dataObject = Clipboard.GetDataObject();
            }
            catch (ExternalException)
            {
                // Clipboard is failed to get the data object.
                // One of reason should be the opening fail of Clipboard by the destroyed hwnd.
                dataObject = null;
                //  must re-throw ???
            }

            bool forceLayoutUpdate = This.Selection.CoversEntireContent;

            if (dataObject != null)
            {
                using (This.Selection.DeclareChangeBlock())
                {
                    // Forget previously suggested horizontal position
                    TextEditorSelection._ClearSuggestedX(This);

                    // _DoPaste raises a public event -- could raise recoverable exception.
                    if (TextEditorCopyPaste._DoPaste(This, dataObject, /*isDragDrop:*/false))
                    {
                        // Collapse selection to the end
                        // Use backward direction to stay oriented towards pasted content
                        This.Selection.SetCaretToPosition(This.Selection.End, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/true);

                        // Clear springload formatting
                        if (This.Selection is TextSelection)
                        {
                            ((TextSelection)This.Selection).ClearSpringloadFormatting();
                        }
                    }
                } // PUBLIC EVENT RAISED HERE AS CHANGEBLOCK CLOSES!
            }

            // If we replaced the entire document content, background layout will
            // kick in.  Force it to complete now.
            if (forceLayoutUpdate)
            {
                This.Selection.ValidateLayout();
            }
        }

        // Converts xaml content to rtf content.
        internal static string ConvertXamlToRtf(string xamlContent, Stream wpfContainerMemory)
        {
            // Create XamlRtfConverter to process the converting from Xaml to Rtf
            XamlRtfConverter xamlRtfConverter = new XamlRtfConverter();
            if (wpfContainerMemory != null)
            {
                xamlRtfConverter.WpfPayload = WpfPayload.OpenWpfPayload(wpfContainerMemory);
            }

            // Process Xaml-Rtf converting
            string rtfContent = xamlRtfConverter.ConvertXamlToRtf(xamlContent);

            return rtfContent;
        }

        // Converts an rtf content to xaml content.
        internal static MemoryStream ConvertRtfToXaml(string rtfContent)
        {
            MemoryStream memoryStream = new MemoryStream();
            WpfPayload wpfPayload = WpfPayload.CreateWpfPayload(memoryStream);
            using (wpfPayload.Package)
            {
                using (Stream xamlStream = wpfPayload.CreateXamlStream())
                {
                    // Create XamlRtfConverter to process the converting from Rtf to Xaml
                    XamlRtfConverter xamlRtfConverter = new XamlRtfConverter();
                    xamlRtfConverter.WpfPayload = wpfPayload;

                    string xamlContent = xamlRtfConverter.ConvertRtfToXaml(rtfContent);
                    if (xamlContent != string.Empty)
                    {
                        StreamWriter streamWriter = new StreamWriter(xamlStream);
                        using (streamWriter)
                        {
                            streamWriter.Write(xamlContent);
                        }
                    }
                    else
                    {
                        memoryStream = null;
                    }
                } // This closes xamlStream
            } // This closes the package

            return memoryStream;
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Cut command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusCut(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            // Ignore the cut event if the editor is on PasswordBox control.
            if (This.UiScope is PasswordBox)
            {
                args.CanExecute = false;
                args.Handled = true;
                return;
            }

            args.CanExecute = !This.Selection.IsEmpty;
            args.Handled = true;
        }

        /// <summary>
        /// Cut command event handler.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Calls TextEditorCopyPaste.Cut, using the UserInitiated
        ///             bit in the event args, to set data on the clipboard.
        /// TreatAsSafe - The bit is protected by the UserIniatedRoutedEvent permission and
        ///               the content being set is based on the active selection.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private static void OnCut(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            // Ignore the cut event if the editor is on PasswordBox control.
            if (This.UiScope is PasswordBox)
            {
                return;
            }

            Cut(This, args.UserInitiated);
        }

        /// <summary>
        /// Copy command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusCopy(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled)
            {
                return;
            }

            // Ignore the copy event if the editor is on PasswordBox control.
            if (This.UiScope is PasswordBox)
            {
                args.CanExecute = false;
                args.Handled = true;
                return;
            }

            args.CanExecute = !This.Selection.IsEmpty;
            args.Handled = true;
        }

        /// <summary>
        /// Copy command event handler.
        /// This method is used both in Copy, Cut and DragDrop commands.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Calls TextEditorCopyPaste.Copy, using the UserInitiated
        ///             bit in the event args, to set data on the clipboard.
        /// TreatAsSafe - The bit is protected by the UserIniatedRoutedEvent permission and
        ///               the content being set is based on the active selection.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private static void OnCopy(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled)
            {
                return;
            }

            // Ignore the copy event if the editor is on PasswordBox control.
            if (This.UiScope is PasswordBox)
            {
                return;
            }

            Copy(This, args.UserInitiated);
        }

        /// <summary>
        /// Paste command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusPaste(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            args.Handled = true;

            try
            {
                if (SecurityHelper.CallerHasAllClipboardPermission())
                {
                    // Define what format our paste mechanism recognizes on the clipbord appropriate for this selection
                    string formatToApply = GetPasteApplyFormat(This, Clipboard.GetDataObject());

                    args.CanExecute = formatToApply.Length > 0;
                }
                else
                {
                    // Simplified version of clipboard sniffing for partial trust
                    args.CanExecute = Clipboard.IsClipboardPopulated();
                }
            }
            catch (ExternalException)
            {
                // Clipboard is failed to get the data object.
                // One of reason should be the opening fail of Clipboard while other
                // process opens the clipboard or missing close of Clipboard.
                args.CanExecute = false;
            }
        }

        /// <summary>
        /// Paste command event handler.
        /// </summary>
        private static void OnPaste(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            Paste(This);
        }

        /// <summary>
        /// StartInputCorrection command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusCopyFormat(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled)
            {
                return;
            }

            //  Provide an implementation for this command
            args.CanExecute = false;
            args.Handled = true;
        }

        private static void OnCopyFormat(object sender, ExecutedRoutedEventArgs args)
        {
            //  Provide an implementation for this command
        }

        /// <summary>
        /// StartInputCorrection command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusPasteFormat(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            //  Provide an implementation for this command
            args.CanExecute = false;
            args.Handled = true;
        }

        private static void OnPasteFormat(object sender, ExecutedRoutedEventArgs args)
        {
            //  Provide an implementation for this command
        }

        /// <SecurityNote>
        ///     Critical: This code calls into CriticalSetData which circumvents all checks for setting data
        /// </SecurityNote>
        /// <summary>
        ///     This code is used to call into an internal overload to set data which circumvents the demand for
        ///     all clipboard permission. Although this is not the cleanest we prefer to cast it to DataObject
        ///     and call the critical overload to reduce the scope of the code that gets called here.
        ///     This saves us one high level assert.
        /// </summary>
        /// <param name="dataObjectValue"></param>
        /// <param name="format"></param>
        /// <param name="content"></param>
        [SecurityCritical]
        private static void CriticalSetDataWrapper(IDataObject dataObjectValue, string format, string content)
        {
            if (dataObjectValue is DataObject)
            {
                ((DataObject)dataObjectValue).CriticalSetData(format, content, format == DataFormats.ApplicationTrust ? /*autoConvert:*/false : true);
            }
        }

        /// <summary>
        /// Paste the content data(Text, Unicode, Xaml and Rtf) to the current text selection
        /// </summary>
        /// <param name="This"></param>
        /// <param name="dataObject">
        /// data object containing data to paste
        /// </param>
        /// <param name="dataObjectToApply">
        /// </param>
        /// <param name="formatToApply">
        /// </param>
        /// <returns>
        /// true if successful, false otherwise
        /// </returns>
        /// <SecurityNote>
        /// This function paste the content data and also can set new apply format with
        /// checking the unmanaged code permission if the content data is failed to paste.
        /// Critical: This function falls back to DataFormats.Rtf in case Xaml paste fails. It calls Critical WpfPayload.SaveImage.
        /// TreatAsSafe: In partial trust we revert to DataFormats.UnicodeText or DataFormats.Text
        ///              format and hence the risk is mitigated of having Rtf paste enabled
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private static bool PasteContentData(TextEditor This, IDataObject dataObject, IDataObject dataObjectToApply, string formatToApply)
        {
            // CF_BITMAP - pasting a single image.
            if (formatToApply == DataFormats.Bitmap && dataObjectToApply is DataObject)
            {
                // This demand is present to explicitly disable RTF independant of any
                // asserts in the confines of partial trust
                // We check unmanaged code instead of all clipboard because in paste
                // there is a high level assert for all clipboard in commandmanager.cs
                if (This.AcceptsRichContent && This.Selection is TextSelection &&
                    SecurityHelper.CheckUnmanagedCodePermission())
                {
                    System.Windows.Media.Imaging.BitmapSource bitmapSource = GetPasteData(dataObjectToApply, DataFormats.Bitmap) as System.Windows.Media.Imaging.BitmapSource;

                    if (bitmapSource != null)
                    {
                        // Pack the image into a WPF container
                        MemoryStream packagedImage = WpfPayload.SaveImage(bitmapSource, WpfPayload.ImageBmpContentType);

                        // Place it onto a data object
                        dataObjectToApply = new DataObject();
                        formatToApply = DataFormats.XamlPackage;
                        dataObjectToApply.SetData(DataFormats.XamlPackage, packagedImage);
                    }
                }
            }

            if (formatToApply == DataFormats.XamlPackage)
            {
                // This demand is present to explicitly disable RTF independant of any
                // asserts in the confines of partial trust
                // We check unmanaged code instead of all clipboard because in paste
                // there is a high level assert for all clipboard in commandmanager.cs
                if (This.AcceptsRichContent && This.Selection is TextSelection &&
                    SecurityHelper.CheckUnmanagedCodePermission())
                {
                    object pastedData = GetPasteData(dataObjectToApply, DataFormats.XamlPackage);

                    MemoryStream pastedMemoryStream = pastedData as MemoryStream;
                    if (pastedMemoryStream != null)
                    {
                        object element = WpfPayload.LoadElement(pastedMemoryStream);
                        if ((element is Section || element is Span) && PasteTextElement(This, (TextElement)element))
                        {
                            return true;
                        }
                        else if (element is FrameworkElement)
                        {
                            ((TextSelection)This.Selection).InsertEmbeddedUIElement((FrameworkElement)element);
                            return true;
                        }
                    }
                }

                // Fall to Xaml:
                dataObjectToApply = dataObject; // go back to source data object
                if (dataObjectToApply.GetDataPresent(DataFormats.Xaml))
                {
                    formatToApply = DataFormats.Xaml;
                }
                else if (SecurityHelper.CheckUnmanagedCodePermission() && dataObjectToApply.GetDataPresent(DataFormats.Rtf))
                {
                    formatToApply = DataFormats.Rtf;
                }
                else if (dataObjectToApply.GetDataPresent(DataFormats.UnicodeText))
                {
                    formatToApply = DataFormats.UnicodeText;
                }
                else if (dataObjectToApply.GetDataPresent(DataFormats.Text))
                {
                    formatToApply = DataFormats.Text;
                }
            }

            if (formatToApply == DataFormats.Xaml)
            {
                if (This.AcceptsRichContent && This.Selection is TextSelection)
                {
                    object pastedData = GetPasteData(dataObjectToApply, DataFormats.Xaml);

                    if (pastedData != null && PasteXaml(This, pastedData.ToString()))
                    {
                        return true;
                    }
                }

                // Fall to Rtf:
                dataObjectToApply = dataObject; // go back to source data object
                if (SecurityHelper.CheckUnmanagedCodePermission() && dataObjectToApply.GetDataPresent(DataFormats.Rtf))
                {
                    formatToApply = DataFormats.Rtf;
                }
                else if (dataObjectToApply.GetDataPresent(DataFormats.UnicodeText))
                {
                    formatToApply = DataFormats.UnicodeText;
                }
                else if (dataObjectToApply.GetDataPresent(DataFormats.Text))
                {
                    formatToApply = DataFormats.Text;
                }
            }

            if (formatToApply == DataFormats.Rtf)
            {
                // This demand is present to explicitly disable RTF independant of any
                // asserts in the confines of partial trust
                // We check unmanaged code instead of all clipboard because in paste
                // there is a high level assert for all clipboard in commandmanager.cs
                if (This.AcceptsRichContent && SecurityHelper.CheckUnmanagedCodePermission())
                {
                    object pastedData = GetPasteData(dataObjectToApply, DataFormats.Rtf);

                    // Convert rtf to xaml text to paste rtf data into the target.
                    if (pastedData != null)
                    {
                        MemoryStream memoryStream = ConvertRtfToXaml(pastedData.ToString());
                        if (memoryStream != null)
                        {
                            TextElement textElement = WpfPayload.LoadElement(memoryStream) as TextElement;
                            if ((textElement is Section || textElement is Span) && PasteTextElement(This, textElement))
                            {
                                return true;
                            }
                        }
                    }
                }

                // Fall to plain text:
                dataObjectToApply = dataObject; // go back to source data object
                if (dataObjectToApply.GetDataPresent(DataFormats.UnicodeText))
                {
                    formatToApply = DataFormats.UnicodeText;
                }
                else if (dataObjectToApply.GetDataPresent(DataFormats.Text))
                {
                    formatToApply = DataFormats.Text;
                }
            }

            if (formatToApply == DataFormats.UnicodeText)
            {
                object pastedData = GetPasteData(dataObjectToApply, DataFormats.UnicodeText);
                if (pastedData == null)
                {
                    if (dataObjectToApply.GetDataPresent(DataFormats.Text))
                    {
                        formatToApply = DataFormats.Text; // fall to plain text
                        dataObjectToApply = dataObject; // go back to source data object
                    }
                }
                else
                {
                    // Dont attempt to recover if pasting Unicode text fails because our only fallback is mbcs text,
                    // which will either evaluate identically (at best) or
                    // produce a string with unexpected text (worse!) from WideCharToMultiByte conversion.
                    return PastePlainText(This, pastedData.ToString());
                }
            }

            if (formatToApply == DataFormats.Text)
            {
                object pastedData = GetPasteData(dataObjectToApply, DataFormats.Text);
                if (pastedData != null && PastePlainText(This, pastedData.ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the paste data from the specified DataObject and data format.
        /// </summary>
        /// <SecurityNote>
        /// Critical: This function calls the critical methods which access the unmanaged code
        /// to get the pasted data from DataObject
        /// </SecurityNote>
        [SecurityCritical]
        private static object GetPasteData(IDataObject dataObject, string dataFormat)
        {
            object pastedData;

            try
            {
                // We don't need to verify data present here. First, GetPasteApplyFormat()
                // is already verified the data present, so reduce the perf. Second, we can't
                // guarantee the presenting data for the some specified data after raising
                // DataObjectPastingEventArgs which case is that set FormatToApply first then
                // set the DataObject that doesn't have FormatToApply data format.
                //Invariant.Assert(dataObject.GetDataPresent(dataFormat));

                pastedData = dataObject.GetData(dataFormat, true);
            }
            // DataObject data can have the invalid value that throw the Exception.
            // In case of OutOfMemoryException, ExternalException(and Win32Exception),
            // we return null quietly and do nothing for paste.
            // For example(Bug#1391689) , IE set the invalid Rich Text Format data that bring
            // CLR OutOfMemoryException.
            catch (OutOfMemoryException)
            {
                pastedData = null;
            }
            catch (ExternalException)
            {
                pastedData = null;
            }

            return pastedData;
        }

        // Paste flow content into the current text selection
        // Returns false if pasting was not successful - assuming that the caller will choose another format for pasting
        private static bool PasteTextElement(TextEditor This, TextElement sectionOrSpan)
        {
            bool success = false;
            This.Selection.BeginChange();
            try
            {
                ((TextRange)This.Selection).SetXmlVirtual(sectionOrSpan);

                // Merge new Lists with surrounding Lists.
                TextRangeEditLists.MergeListsAroundNormalizedPosition((TextPointer)This.Selection.Start);
                TextRangeEditLists.MergeListsAroundNormalizedPosition((TextPointer)This.Selection.End);

                // Merge flow direction of the new content if it matches its surroundings.
                TextRangeEdit.MergeFlowDirection((TextPointer)This.Selection.Start);
                TextRangeEdit.MergeFlowDirection((TextPointer)This.Selection.End);

                success = true;
            }
            finally
            {
                This.Selection.EndChange();
            }

            return success;
        }

        // Paste xaml content into the current text selection
        // Returns false if pasting was not successful - assuming that the caller will choose another format for pasting
        private static bool PasteXaml(TextEditor This, string pasteXaml)
        {
            bool success;

            if (pasteXaml.Length == 0)
            {
                success = false;
            }
            else
            {
                try
                {
                    // Parse the fragment into a separate subtree
                    bool useRestrictiveXamlReader = !Clipboard.UseLegacyDangerousClipboardDeserializationMode();
                    object xamlObject = XamlReader.Load(new XmlTextReader(new System.IO.StringReader(pasteXaml)), useRestrictiveXamlReader);
                    TextElement flowContent = xamlObject as TextElement;

                    success = flowContent == null ? false : PasteTextElement(This, flowContent);
                }
                catch (XamlParseException e)
                {
                    // Clipboard data can have the invalid xaml content that will throw
                    // the XamlParseException.
                    // In case of XamlParseException, we shouldn't paste anything and quiet.
                    // Xaml invalid character range is from 0x00 to 0x20. (e.g. &#0x03)
                    //  We need some indication of a failure. Silence here is very confusing...
                    Invariant.Assert(e != null); //to make compiler happy about not using a variable e. This variable is useful in debugging process though - to see a reason of a parsing failure
                    success = false;
                }
            }

            return success;
        }

        // Helper for plain text filtering when pasted into rich or plain destination
        private static bool PastePlainText(TextEditor This, string pastedText)
        {
            pastedText = This._FilterText(pastedText, This.Selection);

            if (pastedText.Length > 0)
            {
                if (This.AcceptsRichContent && This.Selection.Start is TextPointer)
                {
                    // Clear selection content
                    This.Selection.Text = String.Empty;

                    // Ensure that text is insertable at current selection
                    TextPointer start = TextRangeEditTables.EnsureInsertionPosition((TextPointer)This.Selection.Start);

                    // Store boundaries of inserted text
                    start = start.GetPositionAtOffset(0, LogicalDirection.Backward);
                    TextPointer end = start.GetPositionAtOffset(0, LogicalDirection.Forward);

                    // For rich text we need to remove control characters and
                    // replace linebreaks by paragraphs
                    int currentLineStart = 0;
                    for (int i = 0; i < pastedText.Length; i++)
                    {
                        if (pastedText[i] == '\r' || pastedText[i] == '\n')
                        {
                            end.InsertTextInRun(pastedText.Substring(currentLineStart, i - currentLineStart));
                            if (!This.AcceptsReturn)
                            {
                                return true; // All lined except for the first one are ignored when TextBox does not accept Return key
                            }

                            if (end.HasNonMergeableInlineAncestor)
                            {
                                // We cannot split a Hyperlink or other non-mergeable Inline element,
                                // so insert a space character instead (similar to embedded object).
                                // Note that this means, Paste operation would loose
                                // paragraph break information in this case.
                                end.InsertTextInRun(" ");
                            }
                            else
                            {
                                end = end.InsertParagraphBreak();
                            }

                            if (pastedText[i] == '\r' && i + 1 < pastedText.Length && pastedText[i + 1] == '\n')
                            {
                                i++;
                            }
                            currentLineStart = i + 1;
                        }
                    }
                    end.InsertTextInRun(pastedText.Substring(currentLineStart, pastedText.Length - currentLineStart));

                    // Select all pasted content
                    This.Selection.Select(start, end);
                }
                else
                {
                    // For plain text we insert the content as is (including control characters)
                    This.Selection.Text = pastedText;
                }
                return true;
            }

            return false;
        }

        // Event firing helper for DataObjectSettingData event
        private static bool ConfirmDataFormatSetting(FrameworkElement uiScope, IDataObject dataObject, string format)
        {
            DataObjectSettingDataEventArgs dataObjectSettingDataEventArgs;

            dataObjectSettingDataEventArgs = new DataObjectSettingDataEventArgs(dataObject, format);

            uiScope.RaiseEvent(dataObjectSettingDataEventArgs);

            return !dataObjectSettingDataEventArgs.CommandCancelled;
        }

        #endregion Private methods
    }
}
