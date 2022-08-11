// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  This class represents a package which behaves similar to a Word document.
//
//   - original package is treated as read-only
//   - edits in the meantime are done lazily to a temporary package
//   - when the user Commits their changes the temporary package will be filled
//     and then the temporary file will be copied to the comparee location
//   - if the user discards the package, original will be untouched and the
//     temporary in theory, given the original could be recovered
//
//   - normalizedUri is used only for look ups, the original Uri is what we
//     should always store </description>
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Security;
using System.Windows.TrustUI;

using MS.Internal;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// This class represents a Package which does not alter the original
/// and writes the changes to a temporary package (when provided) as a
/// type of change log; leaving the original untouched.
/// </summary>
/// <remarks>
/// In the descriptions below the following terms are used:
/// 
///   Proxy: This is the reference being given to callers that
///   contains underlying objects.
///   Active: The underlying object that the proxy should pass calls to.
///   Temp: This is the writeable object that contains changes.
///   Original: This is the read only object that has the source data.
/// </remarks>
internal class TransactionalPackage : Package, IDisposable
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //-------------------------------------------------------------------------

    /// <summary>
    /// Requires an existing open Package; and returns a package which will
    /// capture changes with out applying them to the original.
    /// See the class description for details.
    /// </summary>
    /// <exception cref="System.ArgumentNullException" />
    /// <example>
    /// Package package = new TransactionalPackage(
    ///     Package.Open(source, FileMode.Open, FileAccess.Read));
    /// </example>
    /// <param name="originalPackage">An open package.</param>
    internal TransactionalPackage(Stream original)
        : base(FileAccess.ReadWrite)
    {
        if (original == null)
        {
            throw new ArgumentNullException("original");
        }

        Package originalPackage = Package.Open(original);

        _originalPackage = new SecurityCriticalDataForSet<Package>(originalPackage);
        _tempPackage = new SecurityCriticalDataForSet<Package>(null);
    }
    #endregion Constructors

    #region Internal Methods
    //-------------------------------------------------------------------------
    // Internal Methods
    //-------------------------------------------------------------------------

    /// <exception cref="System.ArgumentNullException" />
    internal void EnableEditMode(Stream workspace)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException("workspace");
        }

        if (!workspace.CanWrite)
        {
            throw new ArgumentException(
                SR.Get(SRID.PackagingWriteNotSupported),
                "workspace");
        }

        Package temporaryPackage = Package.Open(
            workspace, FileMode.Create, FileAccess.ReadWrite);

        _tempPackage = new SecurityCriticalDataForSet<Package>(temporaryPackage);
    }

    /// <exception cref="System.ArgumentNullException" />
    /// <exception cref="System.ArgumentException" />
    internal virtual void MergeChanges(Stream target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (!target.CanWrite)
        {
            throw new InvalidOperationException();
        }

        if (_tempPackage.Value != null)
        {
            Package destination = Package.Open(
                target, FileMode.Open, FileAccess.ReadWrite);

            foreach (PackagePart part in _tempPackage.Value.GetParts())
            {
                if (destination.PartExists(part.Uri))
                {
                    Trace.SafeWrite(
                        Trace.Packaging,
                        "Over writing existing part {0}({1}).",
                        part.Uri,
                        part.ContentType);

                    CopyPackagePartStream(
                        part, destination.GetPart(part.Uri));
                }
                else
                {
                    Trace.SafeWrite(
                        Trace.Packaging,
                        "Creating new part from edited part {0}({1}).",
                        part.Uri,
                        part.ContentType);

                    CopyPackagePartStream(
                        part,
                        destination.CreatePart(
                            part.Uri, part.ContentType));
                }
            }
            destination.Flush();
            destination.Close();
            Trace.SafeWrite(Trace.Packaging, "Merge package closed.");
        }
    }

    internal void Rebind(Stream newOriginal)
    {
        if (newOriginal == null)
        {
            throw new ArgumentNullException("newOriginal");
        }

        // close this as we will open a new one
        _originalPackage.Value.Close();
        _trashCan.Add(_originalPackage.Value);
        _isDirty = false;

        Package newPackage = Package.Open(newOriginal, FileMode.Open, FileAccess.Read);

        // remap parts for people who keep references around after we rebind
        foreach (PackagePart part in newPackage.GetParts())
        {
            Uri normalizedPartUri = PackUriHelper.GetNormalizedPartUri(part.Uri);
            if (_activeParts.ContainsKey(normalizedPartUri))
            {
                _activeParts[normalizedPartUri].Target = newPackage.GetPart(part.Uri);
            }
        }

        _originalPackage.Value = newPackage;
    }

    #endregion Internal Methods

    //-------------------------------------------------------------------------
    // Internal Properties
    //-------------------------------------------------------------------------
    #region Internal Properties

    /// <summary>
    /// Indicates whether the TransactionalPackage has been dirtied.
    /// </summary>
    internal bool IsDirty
    {
        get { return _isDirty; }
    }

    #endregion Internal Properties


    #region Protected Methods - Package Overrides
    //-------------------------------------------------------------------------
    // Protected Methods - Package Overrides
    //-------------------------------------------------------------------------

    /// <summary>
    /// Creates a new PackagePart.
    /// </summary>
    /// <remarks>
    /// When creating a new PackagePart we must:
    ///    a) ensure the part does not exist in package
    ///    b) ensure there is a writable package
    ///    c) create a temp part
    ///    d) update active part reference to the temp part
    /// 
    /// What if a PackagePart with the same Uri already exists?
    ///   Package.CreatePart checks for this.
    ///
    /// Do we need to worry about updating relationships and other parts?
    ///   Relationships are a part and are thus intrinsically handled.
    /// </remarks>
    /// <param name="partUri">Uri for the part to create.</param>
    /// <param name="contentType">Content type string.</param>
    /// <param name="compressionOption">Compression options.</param>
    /// <returns>A new PackagePart.</returns>
    protected override PackagePart CreatePartCore(
        Uri partUri, string contentType, CompressionOption compressionOption)
    {
        // Skipping parameter validation as it is done by CreatePart.

        EnsureTempPackage();

        // the underlying temp package does all the physical work
        PackagePart result = _tempPackage.Value.CreatePart(
            partUri, contentType, compressionOption);

        Uri normalizedPartUri = PackUriHelper.GetNormalizedPartUri(partUri);

        result = new WriteableOnDemandPackagePart(
            this, result, TempPackagePartFactory);

        _activeParts.Add(normalizedPartUri, (WriteableOnDemandPackagePart)result);

        Trace.SafeWrite(
            Trace.Packaging,
            "New part {0}({1})#{2} created.",
            result.Uri,
            result.ContentType,
            result.GetHashCode());

        return result;
    }

    /// <summary>
    /// Deletes a PackagePart.
    /// </summary>
    /// <remarks>
    /// When deleting a PackagePart we must:
    ///   a) ensure there is a writable package
    ///   b) remove the temp part
    /// 
    /// What if the part was already deleted?
    /// What if delete is the first operation?
    ///   Then a relationship part in the temorary package would be active;
    ///   and the case(s) would be handled by Package.DeletePart.
    /// 
    /// What if delete is called after a part is created / edited?
    ///   No different then the other cases.
    /// 
    /// What if we call DeletePart on temp Package?
    ///   Unsure why relationships are not updated twice; once by base
    ///   accessing relationship part, then a second when the underlying
    ///   implementor does for Package.DeletePart.
    /// 
    /// Note: We should explore only cleaning up the stream; as the rest is
    /// likely handed by base interacting with the relationship parts.
    /// </remarks>
    /// <param name="partUri">Uri for the part to delete.</param>
    protected override void DeletePartCore(Uri partUri)
    {
        // Skipping parameter validation as it is done by CreatePart.
        if (_tempPackage.Value.PartExists(partUri))
        {
            _tempPackage.Value.DeletePart(partUri);

            Trace.SafeWrite(Trace.Packaging, "Part {0} deleted.", partUri);
        }

        Uri normalizedPartUri = PackUriHelper.GetNormalizedPartUri(partUri);
        if (_activeParts.ContainsKey(normalizedPartUri))
        {
            _activeParts.Remove(normalizedPartUri);
        }
    }

    /// <summary>
    /// Release underlying resources; in this case our packages.
    /// </summary>
    /// <param name="disposing">Indicates if we are disposing.</param>
    protected override void Dispose(bool disposing)
    {
        Trace.SafeWrite(Trace.Packaging, "Dispose was called with {0}.", disposing);

        if (disposing)
        {
            if (_tempPackage.Value != null)
            {
                ((IDisposable)_tempPackage.Value).Dispose();
                _tempPackage.Value = null;
            }

            if (_originalPackage.Value != null)
            {
                ((IDisposable)_originalPackage.Value).Dispose();
                _originalPackage.Value = null;
            }

            _activeParts.Clear();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Flushes our underlying packages.
    /// </summary>
    /// <remarks>
    /// Only the original package will have data integrity at this point.
    /// This is by design as the scope of the class is to be a change log.
    /// </remarks>
    protected override void FlushCore()
    {
        if (_tempPackage.Value != null)
        {
            _tempPackage.Value.Flush();
        }
    }

    /// <summary>
    /// Returns an existing PackagePart.
    /// </summary>
    /// <remarks>
    /// When getting an existing PackagePart we must:
    ///   a) Create a proxy (WriteableOnDemandPackagePart) if there is not 
    ///      already one; if there is we just return it.
    ///   b) We must return the same instance of the proxy for any request
    ///      for that part, as new instances will not have any way of 
    ///      knowing which internal part is 'active' (original/temp) within
    ///      the proxy.
    /// 
    /// What if the part does not exist?
    /// What if the part has been deleted?
    ///   These cases are handled by Package.GetPart.
    /// 
    /// What if the part ends up being edited?
    ///   That is the reason for the proxy, on edit the internal reference 
    ///   will be updated to the temp object and it will service the call.
    /// 
    /// What if a part has already been edited?
    ///   That is why we must return the active part.
    /// </remarks>
    /// <param name="partUri">The Uri of the part to return.</param>
    /// <returns>An existing PackagePart.</returns>
    protected override PackagePart GetPartCore(Uri partUri)
    {
        // Skipping parameter validation as it is done by CreatePart.

        PackagePart result = null;

        Uri normalizedPartUri = PackUriHelper.GetNormalizedPartUri(partUri);
#if DEBUG
        if (_activeParts.ContainsKey(normalizedPartUri))
        {
            Trace.SafeWrite(
                Trace.Packaging,
                "WARNING: GetPartCore called multiple times for {0}.",
                partUri);
        }
#endif

        // We can get the part from three places, which we check in this order:
        // 1) Our saved list of active parts. It's important to get it from
        //    here if possible because all references should point to a single
        //    instance of our WriteableOnDemandPackagePart class.
        // 2) The temp package. If there is a change to a part it will be here,
        //    and we want to return the user's changes.
        // 3) The original package.

        // Even if the part exists in our list of active parts, as part of our
        // contract with the Packaging team we still must use PartExists to
        // check if the part is actually present in either the temporary or the
        // original package. If the part does not exist in either the original
        // or the temporary package, this method will return null.

        bool canGetFromTempPackage =
            (_tempPackage.Value != null) && (_tempPackage.Value.PartExists(partUri));
        bool canGetFromOriginalPackage =
            canGetFromTempPackage ? false : _originalPackage.Value.PartExists(partUri);

        if (_activeParts.ContainsKey(normalizedPartUri)
            && (canGetFromTempPackage || canGetFromOriginalPackage))
        {
            result = _activeParts[normalizedPartUri];
        }
        else if (canGetFromTempPackage)
        {
            result = _tempPackage.Value.GetPart(partUri);

            result = new WriteableOnDemandPackagePart(
                this, result, TempPackagePartFactory);

            _activeParts.Add(normalizedPartUri, (WriteableOnDemandPackagePart)result);

            Trace.SafeWrite(
                Trace.Packaging,
                "GetPartCore returned {0}({1})#{2} a temp part.",
                partUri,
                result.ContentType,
                result.GetHashCode());
        }
        else if (canGetFromOriginalPackage)
        {
            PackagePart original = _originalPackage.Value.GetPart(partUri);
            result = new WriteableOnDemandPackagePart(
                this, original, TempPackagePartFactory);

            _activeParts.Add(normalizedPartUri, (WriteableOnDemandPackagePart)result);

            Trace.SafeWrite(
                Trace.Packaging,
                "GetPartCore returned {0}({1})#{2} a new proxy.",
                partUri,
                result.ContentType,
                result.GetHashCode());
        }

        return result;
    }

    /// <summary>
    /// Will return all the PackageParts in the Package.
    /// </summary>
    /// <remarks>
    /// WARNING: This implementation is based on GetParts implementations
    /// current behavior.  We only expect to be called once on open and then
    /// our base implementation should handle things.
    /// </remarks>
    /// <returns>All PackageParts.</returns>
    protected override PackagePart[] GetPartsCore()
    {
        // need to call get parts from the underlying reading package
        PackagePartCollection parts = _originalPackage.Value.GetParts();
        // // a temporary list of proxied package parts which will be use to fill return value
        List<PackagePart> _proxiedParts = new List<PackagePart>();

        // for all parts not in the active list create a proxy for them
        // and add to active table
        foreach (PackagePart part in parts)
        {
            _proxiedParts.Add(GetPartCore(part.Uri));
        }

        // return the active table
        PackagePart[] result = new PackagePart[_proxiedParts.Count];
        _proxiedParts.CopyTo(result, 0);

        return result;
    }
    #endregion Protected Methods - Package Overrides

    #region Protected Properties
    //-------------------------------------------------------------------------
    // Protected Properties
    //-------------------------------------------------------------------------

    protected SecurityCriticalDataForSet<Package> TempPackage
    {
        get
        {
            return _tempPackage;
        }
    }
    #endregion Protected Properties

    #region Private Methods
    //-------------------------------------------------------------------------
    // Private Methods
    //-------------------------------------------------------------------------

    /// <summary>
    /// A simple stream copy from one PackagePart to another.
    /// </summary>
    /// <param name="original">The source PackagePart.</param>
    /// <param name="copy">The comparee PackagePart.</param>
    private static void CopyPackagePartStream(PackagePart original, PackagePart copy)
    {
        Stream source = original.GetStream(FileMode.Open, FileAccess.Read);
        Stream target = copy.GetStream(FileMode.Create, FileAccess.ReadWrite);

        StreamHelper.CopyStream(source, target);

        source.Close();
        target.Close();
    }

    /// <exception cref="System.InvalidOperationException" />
    private void EnsureTempPackage()
    {
        // if we can not edit ask for it
        if (_tempPackage.Value == null)
        {
            DocumentManager.CreateDefault().EnableEdit(null);
        }

        // if we still don't have it fail
        if (_tempPackage.Value == null)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.PackagingWriteNotSupported));
        }
    }

    /// <summary>
    /// Will create the temporary package part.
    /// </summary>
    /// <exception cref="System.ArgumentNullException" />
    /// <remarks>
    /// Designed for use as a call back from WriteableOnDemandPackagePart.
    /// On being called we must:
    ///   a) ensuring there is a writable package
    ///   b) create the 'temp' part
    ///   c) copy the original part
    /// </remarks>
    /// <param name="packagePart">The original PackagePart to copy.</param>
    /// <returns>A writeable PackagePart.</returns>
    private PackagePart TempPackagePartFactory(PackagePart packagePart)
    {
        if (packagePart == null)
        {
            throw new ArgumentNullException("packagePart");
        }

        EnsureTempPackage();

        Uri partUri = packagePart.Uri;

        PackagePart temp = null;

        if (!_tempPackage.Value.PartExists(partUri))
        {
            Trace.SafeWrite(
                Trace.Packaging,
                "Temporary part {0} does not exist.",
                partUri);

            _isDirty = true;

            temp = _tempPackage.Value.CreatePart(
                partUri,
                packagePart.ContentType,
                packagePart.CompressionOption);

            Trace.SafeWrite(
                Trace.Packaging,
                "Temporary part {0}({1}) created.",
                temp.Uri,
                temp.ContentType);

            CopyPackagePartStream(packagePart, temp);
        }
        else
        {
            temp = _tempPackage.Value.GetPart(partUri);

            Trace.SafeWrite(
                Trace.Packaging,
                "Temporary part {0}({1}) existed.",
                temp.Uri,
                temp.ContentType);
        }

        return temp;
    }
    #endregion Private Methods

    #region Private Fields
    //-------------------------------------------------------------------------
    // Private Fields
    //-------------------------------------------------------------------------

    /// <summary>
    /// Parts that have proxies constructed; this occurs when the are
    /// referenced.
    /// </summary>
    private Dictionary<Uri, WriteableOnDemandPackagePart> _activeParts =
        new Dictionary<Uri, WriteableOnDemandPackagePart>();

    /// <summary>
    /// The original Package; this one is to be treated as read-only.
    /// </summary>
    private SecurityCriticalDataForSet<Package> _originalPackage;
    /// <summary>
    /// The temporary Package; this is the one we work in.
    /// </summary>
    private SecurityCriticalDataForSet<Package> _tempPackage;

    private List<Package> _trashCan = new List<Package>();

    private bool _isDirty;

    #endregion Private Fields
}
}
