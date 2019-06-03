// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// <description>
// Exposes IAttachmentExecute in a CLR friendly design.
// </description>
//
//
//
//

using System;
using System.Runtime.InteropServices;
using System.Security;

using MS.Internal.WindowsBase;

namespace MS.Internal.Security
{
/// <summary>
/// Exposes IAttachmentExecute in a CLR friendly design.
/// </summary>
/// <remarks>
/// Only implemented the single method we are using SaveWithUI.
/// </remarks>
[FriendAccessAllowed]
internal sealed class AttachmentService : IDisposable
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <SecurityNote>
    /// Critical:
    ///  1) Sets _native
    ///  2) Calls into _native which is a security suppressed interface
    ///
    /// TreatAsSafe:
    ///  1) This is the only constructor we are safe to set it here to a new 
    ///     instance of the interface.
    ///  2) Setting the identity of the client once is a safe use of the
    ///     interface.
    /// </SecurityNote>
    [SecurityCritical, SecurityTreatAsSafe]
    private AttachmentService()
    {
        _native = (ISecuritySuppressedIAttachmentExecute)new AttachmentServices();
        _native.SetClientGuid(ref _clientId);
    }
    #endregion Constructors

    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// This method will invoke IAttachment.SaveWithUI; see MSDN documentation.
    /// </summary>
    /// <SecurityNote>
    /// Critical:
    ///  1) Calls into _native which is a security suppressed interface; the
    ///     method called may alter the file
    ///  2) The data provided to the _native method is used for security
    ///     decisions
    ///
    /// NotSafe:
    ///  1) Only the caller can assert that altering this file is done with
    ///     user consent
    ///  2) Only the caller can atest to the veracity of the values being used
    ///     for security decisions
    /// </SecurityNote>
    [SecurityCritical]
    internal static void SaveWithUI(IntPtr parent, Uri source, Uri target)
    {
        using (AttachmentService service = new AttachmentService())
        {
            ISecuritySuppressedIAttachmentExecute native = service._native;
            
            // Call SetSource since web sources is verifiable.
            native.SetSource(source.OriginalString);

            // Call SetLocalPath since function has copied the file into a 
            // location selected by the user.
            native.SetLocalPath(target.LocalPath);

            // Do not call SetFileName since we have the local path.
            // Do not call SetReferrer since we do not have a better zone 
            // than the default (Restricted sites).

            // Call Safe to have 'Mark of the Web' added.
            native.SaveWithUI(parent);
        }
    }
    #endregion Internal Methods

    #region IDisposable Members
    //--------------------------------------------------------------------------
    // IDisposable Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// Exposes IAttachmentExecute in a CLR friendly design.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <SecurityNote>
    /// Critical:
    ///  1) Accesses (get and set) _native
    ///  2) Calls Marshal.ReleaseComObject
    /// 
    /// TreatAsSafe:
    ///  1) Does not leak _native and set's it to null (safe)
    ///  2) Target of Marshal.ReleaseComObject is an object we created
    /// </SecurityNote>
    [SecurityCritical, SecurityTreatAsSafe]
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_native != null)
            {
                Marshal.ReleaseComObject(_native);
                _native = null;
            }
        }
    }

    #endregion IDisposable Members

    #region Finalizers
    //--------------------------------------------------------------------------
    // Finalizers
    //--------------------------------------------------------------------------

    /// <summary>
    /// Exposes IAttachmentExecute in a CLR friendly design.
    /// </summary>
   ~AttachmentService()
    {
        Dispose(true);
    }
    #endregion Finalizers

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    /// <SecurityNote>
    /// Critical:
    ///  1) Is the target of a call to Marshal.ReleaseComObject
    ///  2) It must not change between calls as a sequence of calls to this
    ///     value is used to set the InternetZone of a locally saved file
    ///  3) It represents a security suppressed interface (which is critical)
    /// </SecurityNote>
    [SecurityCritical]
    private ISecuritySuppressedIAttachmentExecute _native;

    private readonly Guid _clientId = new Guid("{D5734190-005C-4d76-B0DD-2FA89BE0B622}");
    #endregion Private Fields

    #region Private Unmanaged Interfaces
    //--------------------------------------------------------------------------
    //  Private Unmanaged Interfaces
    //--------------------------------------------------------------------------

    [ComImport, Guid("4125DD96-E03A-4103-8F70-E0597D803B9C")]
    private class AttachmentServices
    {
    }

    //  IAttachmentExecute - COM object designed to help client applications
    //      safely manage saving and opening attachments for users.
    //      clients are assumed to have some policy/settings already
    //      to determine the support and behavior for attachments.
    //      this API assumes that the client is interactive with the user
    [Guid("73DB1241-1E85-4581-8E4F-A81E1D0F8C57")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    private interface ISecuritySuppressedIAttachmentExecute
    {
        //
        //  ClientTitle - (optional) caller specific title for the prompt
        //    if unset, the prompts come with a default title of "File Download"
        int SetClientTitle(string pszTitle);

        //  ClientGuid - (optional) for storing user specific settings
        //      someprompts are allowed to be avoided in the future if the user
        //      chooses.  that choice is stored on per-client basis indexed by the ClientGuid
        //
        //      Specific Example: In the User Trust Prompt there is a check box that is checked
        //      by default, but may be unchecked by the user.  this option is stored under the ClientGuid
        //      based on the file type.
        //
        //      ClearClientState() will reset any user options stored on the clients behalf.
        /// <SecurityNote>
        /// Critical:
        ///  1) SUC'd
        /// </SecurityNote>
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        int SetClientGuid(ref Guid guid);

        //  EVIDENCE properties

        //  LocalPath - (REQUIRED) path that would be passed to ShellExecute()
        //      if FileName was already used for the Check() and Prompt() calls,
        //      and the LocalPath points to a different handler than predicted,
        //      previous trust may be revoked, and the Policy and User trust re-verified.
        /// <SecurityNote>
        /// Critical:
        ///  1) SUC'd
        /// </SecurityNote>
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        int SetLocalPath(string pszLocalPath);

        //  FileName - (optional) proposed name (not path) to be used to construct LocalPath
        //      optionally use this if the caller wants to perform Check() before copying
        //      the file to the LocalPath.  (eg, Check() proposed download)
        int SetFileName(string pszFileName);

        //  Source - (optional) alternate identity path or URL for a file transfer
        //      used as the primary Zone determinant.  if this is NULL default to the Restricted Zone.
        //      may also be used in the Prompt() UI for the "From" field
        //      may also be sent to handlers that can process URLs
        /// <SecurityNote>
        /// Critical:
        ///  1) SUC'd
        /// </SecurityNote>
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        int SetSource(string pszSource);

        //  Referrer - (optional) Zone determinant for container or link types
        //      only used for Zone/Policy
        //      container formats like ZIP and OLE packager use the Referrer to
        //      indicate indirect inheritance and avoid Zone elevation.
        //      Shortcuts can also use it to limit elevation based on parameters
        int SetReferrer(string pszReferrer);

        //  CheckPolicy() - examines available evidence and checks the resultant policy
        //      * requires FileName or LocalPath
        //
        //  Returns S_OK for enable
        //          S_FALSE for prompt
        //          FAILURE for disable
        //
        int CheckPolicy();

        //  Prompt() - application can force UI at an earlier point,
        //      even before the file has been copied to disk
        //      * requires FileName or LocalPath
        int Prompt(IntPtr hwnd, ATTACHMENT_PROMPT prompt, out ATTACHMENT_ACTION paction);

        //  Save() - should always be called if LocalPath is in not in a temp dir
        //      * requires valid LocalPath
        //      * called after the file has been copied to LocalPath
        //      * may run virus scanners or other trust services to validate the file.
        //          these services may delete or alter the file
        //      * may attach evidence to the LocalPath
        int Save();

        //  Execute() - will call Prompt() if necessary, with the EXEC action
        //      * requires valid LocalPath
        //      * called after the file has been copied to LocalPath
        //      * may run virus scanners or other trust services to validate the file.
        //          these services may delete or alter the file
        //      * may attach evidence to the LocalPath
        //
        //      phProcess - if non-NULL Execute() will be synchronous and return an HPROCESS if available
        //                  if null Execute() will be async, implies that you have a message pump and a long lived window
        //
        int Execute(IntPtr hwnd, string pszVerb, out IntPtr phProcess);

        //   SaveWithUI() - superset of Save() that can show modal error UI, but still does not call Prompt()
        //      * requires valid LocalPath
        //      * called after the file has been copied to LocalPath
        //      * may run virus scanners or other trust services to validate the file.
        //          these services may delete or alter the file
        //      * may attach evidence to the LocalPath
        /// <SecurityNote>
        /// Critical:
        ///  1) SUC'd
        /// </SecurityNote>
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        int SaveWithUI(IntPtr hwnd);

        //  ClearClientState() - removes any state that is stored based on the ClientGuid
        //      * requires SetClientGuid() to be called first
        int ClearClientState();
    }

    private enum ATTACHMENT_PROMPT
    {
        ATTACHMENT_PROMPT_NONE              = 0x0000,
        ATTACHMENT_PROMPT_SAVE              = 0x0001,
        ATTACHMENT_PROMPT_EXEC              = 0x0002,
        ATTACHMENT_PROMPT_EXEC_OR_SAVE      = 0x0003,
    }

    private enum ATTACHMENT_ACTION
    {
        ATTACHMENT_ACTION_CANCEL            = 0x0000,
        ATTACHMENT_ACTION_SAVE              = 0x0001,
        ATTACHMENT_ACTION_EXEC              = 0x0002,
    }

    #endregion Private Unmanaged Interface imports
}
}
