// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  An XpsDocument stream represents the stream data for the document
//  regardless of implementation of the backing streams.  It has logical
//  operations that allow elevating from Read to SafeWrite and editing in place.

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Windows.TrustUI;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// An XpsDocument stream represents the stream data for the document
/// regardless of implementation of the backing streams.  It has logical
/// operations that allow elevating from Read to SafeWrite and editing in place.
/// </summary>
/// <remarks>
/// Responsibility:
/// The class must hide the location and implemenation complexity of
/// performing simple logical operations needed by the system.
/// 
/// Design Comments:
/// The need for this is primarly driven from two factors:
/// 
///  - Package which does not allow use to discard changes
/// 
///  - RightsManagement where key changes make it impossible to edit a 
///    document in place
/// </remarks>
internal sealed class DocumentStream : StreamProxy, IDisposable
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Constructs an DocumentStream.
    /// </summary>
    /// <param name="xpsFileToken">The file to manage.</param>
    /// <param name="mode">The mode to open the file in.</param>
    /// <param name="access">The access to open the file with.</param>
    /// <param name="original">If a temporary file; the file we are based on.
    /// </param>
    private DocumentStream(
        CriticalFileToken xpsFileToken,
        Stream dataSource,
        DocumentStream original)
        : base(dataSource)
    {
        _original = original;
        _xpsFileToken = xpsFileToken;
    }
    #endregion Constructors

    #region StreamProxy Overrides
    //--------------------------------------------------------------------------
    // StreamProxy Overrides
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will close the document stream and clean up any temporary
    /// files.
    /// </summary>
    /// <remarks>
    /// We are overriding StreamProxy's implementation which calls
    /// Close on the target of the proxy (FileStream), which means
    /// our Dispose(boolean) would not get called.  This way the
    /// 'this' in Stream.Close will refer to 'DocumentStream'
    /// not target's (FileStream) and we will get called.
    /// </remarks>
    public override void Close()
    {
        this.Dispose();
    }
    #endregion StreamProxy Overrides

    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will create an XpsDocument by copying this one to a target file and
    /// returning a stream corresponding to the new file.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <exception cref="System.IO.InvalidDataException"/>
    /// <param name="copiesToken">The token for the target file.</param>
    /// <returns>An DocumentStream.</returns>
    internal DocumentStream Copy(CriticalFileToken copiesToken)
    {
        DocumentStream result;
        FileStream target = null;

        bool isFileSource = (_xpsFileToken != null);

        Invariant.Assert(copiesToken != null, "No target file to which to copy.");

        ThrowIfInvalidXpsFileForSave(copiesToken.Location);

        string sourcePath = string.Empty;
        string copiesPath = copiesToken.Location.LocalPath;

        // if the source is a file, we need to release our lock on the source
        // file and also assert for permissions to read the file
        if (isFileSource)
        {
            Target.Close();
            sourcePath = _xpsFileToken.Location.LocalPath;
        }

        try
        {
            // if the source is a file, file copy is the fastest
            if (isFileSource)
            {
                File.Copy(sourcePath, copiesPath, true);

                // If the original file was marked read-only, the copy will be read-only as
                // well.  However, the copy is done for the purpose of creating a new file,
                // so it should not be marked read-only.
                FileAttributes attrib = File.GetAttributes(copiesPath);
                if ((attrib & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(copiesPath, attrib ^ FileAttributes.ReadOnly);
                }

                // open the destination file that was just created by File.Copy
                target = new FileStream(
                    copiesPath,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            else
            {
                // open the destination file for create; we will copy the
                // source stream's data to the new stream outside the assert
                target = new FileStream(
                    copiesPath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
        }
        // Since we have already closed the original file, we need to reopen it if we
        // fail to copy the file or open the new file.  After doing so, we rethrow the 
        // original exception so it can be handled at a higher level.
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
        catch
        {
            if (isFileSource)
            {
                Trace.SafeWrite(
                    Trace.File,
                    "File copy failed -- reopening original file.");
                try
                {
                    Target = new FileStream(
                        sourcePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read);
                }
                // If we fail to reopen the original file, rethrow an exception to
                // indicate this specific error.
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
                catch (Exception e)
                {
                    Trace.SafeWrite(
                        Trace.File,
                        "Unable to reopen original file.");

                    throw new UnauthorizedAccessException(
                        SR.Get(SRID.DocumentStreamCanNoLongerOpen), e);
                }
            }
            throw;
        }

        if (isFileSource)
        {
            Trace.SafeWrite(Trace.File, "Performed a file copy from source.");

            // reacquire our stream
            ReOpenWriteable();
        }
        else
        {
            // if the source wasn't a file, we want to copy the stream now
            StreamHelper.CopyStream(this, target);
            Trace.SafeWrite(Trace.File, "Performed a stream copy from source.");
        }

        //----------------------------------------------------------------------
        // Create the DocumentStream
        result = new DocumentStream(copiesToken, target, this);

        result.DeleteOnClose = false;

        Trace.SafeWrite(Trace.File, "Created copy to file {0}.", copiesToken.Location);

        return result;
    }

    /// <summary>
    /// Will create an DocumentStream backed by a tempoary file.
    /// </summary>
    /// <remarks>
    /// We prefer to create the temporary file in the same location as the
    /// source to inherit the folders attributes and security.
    /// 
    /// If we can not we will use a system generated file.
    /// </remarks>
    /// <param name="copyoriginal">When true we will copy the source file if
    /// possible.  You must check for a non-zero result on the returning stream
    /// to determin success.</param>
    /// <returns>An DocumentStream.</returns>
    internal DocumentStream CreateTemporary(bool copyOriginal)
    {
        CriticalFileToken tempToken = null;
        DocumentStream result = null;

        FileStream temporary = null;
        bool isFileSource = (_xpsFileToken != null);

        //----------------------------------------------------------------------
        // Open File in Same Location (if possible)
        if (isFileSource)
        {
            MakeTempFile(true, out temporary, out tempToken);
        }

        //----------------------------------------------------------------------
        // Open File in System Generated Location
        if (tempToken == null)
        {
            // TODO: Should we prompt user asking if it is okay in the case
            // where the source is a local file, as it may mean degraded
            // security?  We could check if this would be the case by
            // comparing ACLs & attributes before prompting

            MakeTempFile(false, out temporary, out tempToken);
        }

        //----------------------------------------------------------------------
        // File Was Opened
        if ((temporary != null) && (tempToken != null))
        {
            //------------------------------------------------------------------
            // Copy Data
            if (copyOriginal)
            {
                // We use a native File.Copy if possible because this is 
                // most performant.  This is only possible if the source is file
                // based.
                if (isFileSource)
                {
                    string sourcePath = _xpsFileToken.Location.LocalPath;
                    string tempPath = tempToken.Location.LocalPath;

                    temporary.Close();

                    File.Copy(sourcePath, tempPath, true);

                    temporary = new FileStream(
                        tempPath,
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.None);

                    // we did the copy
                    copyOriginal = false;

                    Trace.SafeWrite(Trace.File, "Performed a file copy from source.");
                }
                else
                {
                    StreamHelper.CopyStream(
                        this, temporary);
                    Trace.SafeWrite(Trace.File, "Performed a stream copy from source.");
                }
            }

            //------------------------------------------------------------------
            // Create the DocumentStream
            result = new DocumentStream(
                tempToken, temporary, this);

            result.DeleteOnClose = true;

            Trace.SafeWrite(Trace.File, "Created temporary file {0}.", tempToken.Location);
        }
        else
        {
            // rescind consent if any was given
            tempToken = null;
            Trace.SafeWrite(Trace.File, "Unable to create a temporary file.  Caller is expected to disable edits.");
        }
        return result;
    }

    /// <summary>
    /// Checks if there is a read-only file at the path stored in the given
    /// CriticalFileToken.
    /// </summary>
    /// <param name="fileToken">A token containing the path of the file</param>
    /// <returns>True if a file exists and is read-only</returns>
    internal static bool IsReadOnly(CriticalFileToken fileToken)
    {
        string path = fileToken.Location.LocalPath;
        FileAttributes attributes = FileAttributes.Normal;

        if (File.Exists(path))
        {
            attributes = File.GetAttributes(path);
        }

        return ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
    }

    /// <summary>
    /// Will open an XpsDocument and return this stream.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <exception cref="System.IO.InvalidDataException"/>
    /// <param name="document">The document to open.</param>
    /// <param name="writeable">When true will open the file writeable.
    /// Default is read-only.</param>
    /// <returns>An DocumentStream.</returns>
    internal static DocumentStream Open(
        FileDocument document, bool writeable)
    {
        return Open(document.SourceToken, writeable);
    }

    /// <summary>
    /// Will open an XpsDocument and return this stream.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <exception cref="System.IO.InvalidDataException"/>
    /// <param name="xpsFileToken">The token for the file to open.</param>
    /// <param name="writeable">When true will open the file writeable.
    /// Default is read-only.</param>
    /// <returns>An DocumentStream.</returns>
    internal static DocumentStream Open(CriticalFileToken xpsFileToken, bool writeable)
    {
        ThrowIfInvalidXpsFileForOpen(xpsFileToken.Location);

        FileAccess access = FileAccess.Read;

        if (writeable)
        {
            access |= FileAccess.Write;
        }

        string sourcePath = xpsFileToken.Location.LocalPath;

        FileStream dataSource = new FileStream(
            sourcePath,
            FileMode.OpenOrCreate,
            access,
            (access == FileAccess.Read) ? FileShare.Read : FileShare.None);


        Trace.SafeWrite(Trace.File, "Opened file {0}.", sourcePath);

        return new DocumentStream(
            xpsFileToken, dataSource, null);
    }

    /// <summary>
    /// Will open an create a DocumentStream using the provided stream.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <param name="existing">The existing stream to use.</param>
    /// <returns>An DocumentStream.</returns>
    internal static DocumentStream Open(Stream existing)
    {
        if (existing == null)
        {
            throw new ArgumentNullException("existing");
        }

        Trace.SafeWrite(
            Trace.File,
            "Opened {0}#{1} as existing stream.",
            existing,
            existing.GetHashCode());

        return new DocumentStream(
            null, existing, null);
    }

    /// <summary>
    /// Will re-open the file writeable and update the underlying stream.
    /// </summary>
    /// <returns>
    /// True if the operation succeeded.
    /// </returns>
    internal bool ReOpenWriteable()
    {
        if ((_xpsFileToken == null) || (!_xpsFileToken.Location.IsFile))
        {
            return false;
        }

        bool success = false;

        //----------------------------------------------------------------------
        // Release Existing Locks (so we open with write)
        if (Target != null)
        {
            Target.Close();
        }

        //----------------------------------------------------------------------
        // Open Writable (if it fails re-open for Read)
        FileStream fs = null;
        // want to use same local values for assert and open
        string path = _xpsFileToken.Location.LocalPath;

        Exception exception = null;

        try
        {
            fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read | FileAccess.Write,
                FileShare.None);

            success = true;
        }
        // thrown on file is locked by others or we can't modify the file
        catch (IOException ioe)
        {
            exception = ioe;
        }
        // thrown on file is locked by others or we can't modify the file
        catch (UnauthorizedAccessException uae)
        {
            exception = uae;
        }

        if (!success)
        {
            if (exception != null)
            {
                Trace.SafeWrite(
                    Trace.File,
                    "Failed to reopen {0} writeable.\n{1}",
                    path,
                    exception);
            }

            // on failure try to regain our read access as we had before
            fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }

        Invariant.Assert(
            fs != null,
            "ReOpenWriteable: Can no longer open file.");

        if (success)
        {
            Trace.SafeWrite(Trace.File, "Reopened {0} writeable.", path);
        }
        else
        {
            Trace.SafeWrite(Trace.File, "Reopened {0} read only.", path);
        }

        //----------------------------------------------------------------------
        // Update Reference
        Target = fs;

        return success;
    }

    /// <summary>
    /// Will swap the temporary file with the original file.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <exception cref="System.InvalidDataException"/>
    /// <exception cref="System.InvalidOperationException"/>
    /// <returns>False if the operation failed.</returns>
    /// <remarks>
    /// This method is inplace to work around issues with re-publishing
    /// XpsDocuments into the same file.  The intended use for the method is 
    /// to logically allow in place editing for the user.
    /// 
    /// After use this object is unusable and should be disposed as the
    /// temporary file is gone; it has become the original. In the event of an
    /// error while swapping the file, the file no longer becomes the original,
    /// but this object still becomes unusable.
    /// </remarks>
    internal bool SwapWithOriginal()
    {
        bool success = false;

        if (_original == null)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.DocumentStreamMustBeTemporary));
        }
        if (_original._xpsFileToken == null)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.DocumentStreamMustBeFileSource));
        }
        if (_xpsFileToken == null)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.DocumentStreamMustBeFileSource));
        }

        Trace.SafeWrite(
            Trace.File,
            "Begining file swap between {0} and {1}.",
            _xpsFileToken.Location,
            _original._xpsFileToken.Location);

        ThrowIfInvalidXpsFileForSave(_xpsFileToken.Location);
        ThrowIfInvalidXpsFileForOpen(_original._xpsFileToken.Location);

        string original = _original._xpsFileToken.Location.LocalPath;

        //----------------------------------------------------------------------
        // Capture Attributes & ACLs (so we can apply to copied file)

        FileInfo originalInfo = new FileInfo(original);
        FileAttributes originalAttribs = originalInfo.Attributes;
        FileSecurity originalSecurity = originalInfo.GetAccessControl(AccessControlSections.Access);

        //----------------------------------------------------------------------
        // Release Existing Locks (so we can manipulate files)

        // Cache the original stream position so we can restore it on failure
        long originalPosition = _original.Position;
        _original.Target.Close();

        Target.Close();

        //----------------------------------------------------------------------
        // Swap Files On Disk
        try
        {
            RobustFileMove();
            success = true;
        }
        catch (IOException)
        {
            Trace.SafeWrite(
                Trace.File, "File could not be swapped with original.");
        }
        catch (UnauthorizedAccessException)
        {
            Trace.SafeWrite(
                Trace.File, "File could not be swapped with original.");
        }

        //----------------------------------------------------------------------
        // Set Attributes & ACLs
        // (logically user expected an edit thus attributes should be same)
        if (success)
        {
            try
            {
                originalInfo.Attributes = originalAttribs;
                originalInfo.SetAccessControl(originalSecurity);
            }
            // A failure to set attributes or ACLs is not fatal to the application, so we
            // do not want it to crash the application for the user, and thus catch all
            // exceptions here.
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
            catch
            {
                // TODO: 1603621 back out changes in case of failure 
                Trace.SafeWrite(
                    Trace.File,
                    "File attributes or permissions could not be set.");
            }

            Trace.SafeWrite(
                Trace.File, "File was successfully swapped with original.");
        }
        else
        {
            try
            {
                // try to regain our read access as we had before
                _original.Target = new FileStream(
                    original,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
            }
            // In this case we catch all exceptions and then rethrow a new exception --
            // always using the same exception as a wrapper allows higher level
            // routines to respond to a failure specifically in this section.
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
            catch (Exception e)
            {
                Trace.SafeWrite(
                    Trace.File,
                    "SwapWithOriginal has left us in an unusable state.");

                throw new UnauthorizedAccessException(
                    SR.Get(SRID.DocumentStreamCanNoLongerOpen), e);
            }

            // CanRead will be true only if the stream was opened; Close set it
            // to false earlier
            if (_original.CanRead)
            {
                // Restore the position to prevent errors for callers dependent
                // on position
                _original.Position = originalPosition;
            }

            Trace.SafeWrite(
                Trace.File, "File swap was unsuccessful; restored original stream.");
        }

        return success;
    }
    #endregion Internal Methods

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// When true will delete the file on close.
    /// </summary>
    internal bool DeleteOnClose
    {
        get { return _deleteOnClose; }

        set { _deleteOnClose = value; }
    }
    #endregion Internal Properties

    #region IDisposable Members
    //--------------------------------------------------------------------------
    // IDisposable Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// IDisposable implementation
    /// </summary>
    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); 
    }


    protected override void Dispose(bool disposing)
    {
        Trace.SafeWrite(
            Trace.File,
            "{0}({1}).Dispose({2}) called {3} delete.",
            this,
            (_xpsFileToken != null) ? _xpsFileToken.Location.ToString() : string.Empty,
            disposing,
            DeleteOnClose && disposing ? "should" : "should not");

        try
        {
            // this is StreamProxy who first disposes its Stream
            // base, then the target of proxy (FileStream) in this
            // case thus releasing the locks
            base.Dispose(disposing);
        }
        finally
        {
            if (disposing)
            {
                if (DeleteOnClose)
                {
                    string path = _xpsFileToken.Location.LocalPath;
                    try
                    {
                        Trace.SafeWrite(
                            Trace.File,
                            "Attempting delete of {0}.",
                            path);

                        File.Delete(path);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        Trace.SafeWrite(
                            Trace.File,
                            "Delete of temporary file {0} failed.\n{1}",
                            path,
                            uae);

                        // do nothing intentionally if we can't we should still
                        // shutdown gracefully
                    }
                }
            }
        }
    }
    #endregion IDisposable Members

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will create a temporary file.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <exception cref="System.IO.InvalidDataException"/>
    /// <param name="inSameFolder">When true will attempt to use the same folder
    /// as the orginal file.</param>
    /// <param name="temporary">The stream for the temporary file.</param>
    /// <param name="tempToken">The file token for the temporary file.</param>
    private void MakeTempFile(
        bool inSameFolder, 
        out FileStream temporary,
        out CriticalFileToken tempToken)
    {
        temporary = null;
        tempToken = null;

        // Will retry three times for a temp file in the folder.
        // The user could have two copies open and be saving before we
        // would fall back.  The reason for not making this a large
        // number is the user may not have access to the folder the attemps
        // would be futile and degrade the experience with delays
        for (int i = 0; i <= 3; i++)
        {
            Uri location = MakeTemporaryFileName(inSameFolder, i);
            string tempPath = location.LocalPath;

            ThrowIfInvalidXpsFileForSave(location);

            try
            {
                temporary = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None);

                File.SetAttributes(
                    tempPath,
                    FileAttributes.Hidden | FileAttributes.Temporary);

                tempToken = new CriticalFileToken(location);
            }
            catch (IOException io)
            {
                Trace.SafeWrite(
                    Trace.File,
                    "Temporary file {0} is likely in use.\n{1}",
                    temporary,
                    io);
            }
            catch (UnauthorizedAccessException io)
            {
                Trace.SafeWrite(
                    Trace.File,
                    "Temporary file {0} is likely in use.\n{1}",
                    temporary,
                    io);
            }
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
            catch (Exception exception)
            {
                // not editing is not a critical failure for the application
                // so we can handle this method failing any event
                Trace.SafeWrite(
                    Trace.File,
                    "Giving up on temp file.\n{0}",
                    exception);
                break;
            }
            if (tempToken != null)
            {
                break;
            }

#if DEBUG
            Invariant.Assert(
                ((i != 3) || (temporary != null) || (inSameFolder)),
                "Unable to create a temp file.\n"
                + "Unless IE Cache is read-only we have a defect.");
#endif
        }
    }

    /// <summary>
    /// Will create temporary file name based on this file.
    /// </summary>
    /// <param name="generation"></param>
    /// <returns></returns>
    private Uri MakeTemporaryFileName(bool inSameFolder, int generation)
    {
        string path, file;

        if (inSameFolder)
        {
            path = _xpsFileToken.Location.LocalPath;
            file = Path.GetFileNameWithoutExtension(path);
        }
        else
        {
            path = MS.Win32.WinInet.InternetCacheFolder.LocalPath;
            file = this.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        string temp = string.Format(
            CultureInfo.CurrentCulture,
            "{0}{1}~{2}{3}{4}",
            Path.GetDirectoryName(path),
            Path.DirectorySeparatorChar,
            generation == 0 ? 
                string.Empty :
                generation.ToString(CultureInfo.CurrentCulture)
                + "~",
            file,
            XpsFileExtension);
        return new Uri(temp);
    }

    /// <summary>
    /// Will safely move the temp file to the comparee file.
    /// </summary>
    /// <remarks>
    /// Design is simple Source needs to be moved to Target ensuring
    /// no data loss on errror.
    /// 
    /// If Source exists rename it (creating Backup)
    /// Move Source to Target
    /// If error occurs Move Backup to Source (resore from Backup)
    /// If all was good (delete Backup)
    /// 
    /// This design incures trival I/O costs by using moves.
    /// </remarks>
    private void RobustFileMove()
    {
        string sourceFile = _xpsFileToken.Location.LocalPath;
        string targetFile = _original._xpsFileToken.Location.LocalPath;
        string backupFile = targetFile + ".bak";

        bool backupExists = false;
        FileAttributes targetAttributes = FileAttributes.Normal;

        // back up the file if we will be overwriting
        if (File.Exists(targetFile))
        {
            Trace.SafeWrite(
                Trace.File,
                "Attempting backup of {0} due to overwrite case.",
                targetFile);

            // GetTempPath will create a zero byte file
            // and move will fail if it exists

            // If we already have a backup file, we'll delete it first.
            if( File.Exists( backupFile ) )
            {
                // Reset attributes so we can delete even read-only files
                File.SetAttributes(backupFile, FileAttributes.Normal);
                File.Delete(backupFile);
            }

            // Save the original file attributes so we can restore them if
            // the temp file copy fails.
            targetAttributes = File.GetAttributes(targetFile);
            File.Move(targetFile, backupFile);              

            Trace.SafeWrite(
                Trace.File,
                "Created backup of {0} at {1}.",
                targetFile,
                backupFile);
            backupExists = true;
        }

        try
        {
            // Set the attributes on the backup to Normal so that we can
            // successfully delete it after the copy.
            File.SetAttributes(backupFile, FileAttributes.Normal);

            // try to move (save) the temp file to its final location
            File.Move(sourceFile, targetFile);
            Trace.SafeWrite(
                Trace.File,
                "Moved(Saved) {0} as {1}.",
                sourceFile,
                targetFile);                
        }
        catch
        {
            // catching everything as we do not care why we failed
            // regardless we want to restore the original file

            if (backupExists)
            {
                // restore original on failure
                File.Move(backupFile, targetFile);                    
                Trace.SafeWrite(
                    Trace.File,
                    "Restored {0} from {1}, due to exception.",
                    targetFile,
                    backupFile);

                File.SetAttributes(targetFile, targetAttributes);
            }
            // pass the error up as we can't fix it
            throw;
        }

        try
        {
            // Try to delete the backup file
            if (backupExists)
            {                    
                File.Delete(backupFile); 
                backupExists = false;
                Trace.SafeWrite(
                    Trace.File,
                    "Removed backup {0}.",
                    backupFile);
            }
        }
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
        catch(Exception e)
        {
            // We were unable to delete the backup file -- this is not fatal.
            Trace.SafeWrite(
                Trace.File,
                    "Unable to remove backup {0}.\n{1}",
                backupFile,
                e);
        }            
    }

    /// <summary>
    /// Will throw if location is not a XpsDocument file.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <exception cref="System.InvalidOperationException"/>
    /// <exception cref="System.InvalidDataException"/>
    /// <param name="location"></param>
    private static void ThrowIfInvalidXpsFileForSave(Uri location)
    {
        ThrowIfInvalidXpsFileForOpen(location);

        if (!Path.GetExtension(location.LocalPath).Equals(
            XpsFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                SR.Get(SRID.DocumentStreamMustBeXpsFile));
        }
    }

    /// <summary>
    /// Will throw if location is not a valid file.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"/>
    /// <exception cref="System.InvalidOperationException"/>
    private static void ThrowIfInvalidXpsFileForOpen(Uri location)
    {
        if (location == null)
        {
            throw new ArgumentNullException("location");
        }
        if (!location.IsFile)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.DocumentStreamMustBeFileSource));
        }
    }
    #endregion

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    internal static readonly string XpsFileExtension =
        SR.Get(SRID.FileManagementSaveExt);

    bool _deleteOnClose;

    /// <summary>
    /// If not null, this file is a temporary file based on this one.
    /// </summary>
    /// <remarks>
    /// Temporary files should be deleted on disposed, the value of this field
    /// is used for that decision.
    /// </remarks>
    DocumentStream _original;

    /// <summary>
    /// The file we are managing.
    /// </summary>
    CriticalFileToken _xpsFileToken;
    #endregion
}
}
