// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  Interacts with user on file based information for XpsViewer.

using System;
using System.IO;
using System.Security;
using System.Windows.Forms;
using System.Windows.TrustUI;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Interacts with user on file based information for XpsViewer.
/// </summary>
/// <remarks>
/// Responsibility:
/// Should be the only class that interacts with the user with or for file
/// location information.
/// </remarks>
internal static class FilePresentation
{
    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Prompts the user for the save location for the current XpsDocument.
    /// </summary>
    /// <param name="fileToken">The token for the current document</param>
    /// <returns></returns>
    internal static bool ShowSaveFileDialog(ref CriticalFileToken fileToken)
    {
        string extension = SR.Get(SRID.FileManagementSaveExt);

        Trace.SafeWrite(Trace.File, "Showing SafeFileDialog.");
        
        bool result = false;

        SaveFileDialog save = new SaveFileDialog();

        if (fileToken != null)
        {
            save.FileName = fileToken.Location.LocalPath;
        }

        save.Filter = SR.Get(SRID.FileManagementSaveFilter);
        save.DefaultExt = extension;

        DialogResult dialogResult;

        // We need to invoke the ShowDialog method specifying a parent window.
        // We need to specify a parent window in order to avoid a Winforms
        // Common Dialog issue where the wrong window is used as the parent
        // which causes the Save dialog to be incorrectly localized.

        // Get the root browser window, if it exists.
        IWin32Window rbw = null;
        if (DocumentApplicationDocumentViewer.Instance != null)
        {
            rbw = DocumentApplicationDocumentViewer.Instance.RootBrowserWindow;
        }

        if (rbw != null)
        {
            dialogResult = save.ShowDialog(rbw);
        }
        else
        {
            dialogResult = save.ShowDialog();
        }

        if (dialogResult == DialogResult.OK)
        {
            string filePath = save.FileName;

            // Add .xps extension if not already present.
            // This must be done manually since the file save dialog will automatically
            // add the extension only if the filename doesn't have a known extension.
            // For instance, homework.1 would become homework.1.xps, but if the user
            // gets up to homework.386, then the dialog would just pass it through as
            // is, requiring us to append the extension here.
            if (!extension.Equals(
                Path.GetExtension(filePath), 
                StringComparison.OrdinalIgnoreCase))
            {
                filePath = filePath + extension;
            }

            Uri file = new Uri(filePath);

            // as this is the only place we can verify the user authorized this
            // particular file we construct the token here
            fileToken = new CriticalFileToken(file);
            result = true;
            Trace.SafeWrite(Trace.File, "A save location was selected.");
        }

        return result;
    }

    /// <summary>
    /// Notifies the user that the selected destination file is read-only, so
    /// we cannot save to that location.
    /// </summary>
    internal static void ShowDestinationIsReadOnly()
    {
        System.Windows.MessageBox.Show(
            SR.Get(SRID.FileManagementDestinationIsReadOnly),
            SR.Get(SRID.FileManagementTitleError),
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Exclamation
            );
    }

    /// <summary>
    /// Notifies the user of the failure to create temporary files, which
    /// prevents editing.
    /// </summary>
    internal static void ShowNoTemporaryFileAccess()
    {
        System.Windows.MessageBox.Show(
            SR.Get(SRID.FileManagementNoTemporaryFileAccess),
            SR.Get(SRID.FileManagementTitleError),
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Exclamation
            );
    }

    /// <summary>
    /// Notifies the user of the failure to open from a location.
    /// </summary>
    internal static void ShowNoAccessToSource()
    {
        System.Windows.MessageBox.Show(
            SR.Get(SRID.FileManagementNoAccessToSource),
            SR.Get(SRID.FileManagementTitleError),
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Exclamation
            );
    }

    /// <summary>
    /// Notifies the user of the failure to save to a location.
    /// </summary>
    internal static void ShowNoAccessToDestination()
    {
        System.Windows.MessageBox.Show(
            SR.Get(SRID.FileManagementNoAccessToDestination),
            SR.Get(SRID.FileManagementTitleError),
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Exclamation
            );
    }
    #endregion Internal Methods
}
}
