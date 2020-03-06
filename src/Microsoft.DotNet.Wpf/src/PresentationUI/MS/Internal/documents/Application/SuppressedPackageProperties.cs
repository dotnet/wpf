// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  Responsible for suppressing the Assert for unmanaged code permission and
//  replacing it with SecurityCritical attribute.  

using System;
using System.IO.Packaging;
using System.Security;

namespace MS.Internal.Documents.Application
{

/// <summary>
/// Responsible for suppressing the Assert for unmanaged code permission and
/// replacing it with SecurityCritical attribute.  
/// </summary>
/// <remarks>
/// This is implemented as a decorating proxy where all calls are passed to
/// a target PackageProperties object.  The primary mitigation for the
/// asserts is that the class sets the target from EncryptedPackageEnvelope
/// the known good source for the target; as well the entire class is
/// SecurityCritical.
/// </remarks>
internal class SuppressedProperties : PackageProperties
{
    #region Constructors
    //--------------------------------------------------------------------------
    //  Constructors
    //--------------------------------------------------------------------------

    internal SuppressedProperties(EncryptedPackageEnvelope envelope)
    {
        _target = envelope.PackageProperties;
    }
    #endregion Constructors

    #region Public Properties
    //--------------------------------------------------------------------------
    //  Public Properties
    //--------------------------------------------------------------------------

    #region SummaryInformation properties

    /// <value>
    /// The title.
    /// </value>
    public override string Title
    {
        
        get
        {
             return _target.Title;
        }

        set
        {
             _target.Title = value;
        }
    }

    /// <value>
    /// The topic of the contents.
    /// </value>
    public override string Subject
    {
        
        get
        {
            return _target.Subject;
        }

        set
        {
            _target.Subject = value;
        }
    }

    /// <value>
    /// The primary creator. The identification is environment-specific and
    /// can consist of a name, email address, employee ID, etc. It is
    /// recommended that this value be only as verbose as necessary to
    /// identify the individual.
    /// </value>
    public override string Creator
    {
        
        get
        {
            return _target.Creator;
        }

        set
        {
            _target.Creator = value;
        }
    }

    /// <value>
    /// A delimited set of keywords to support searching and indexing. This
    /// is typically a list of terms that are not available elsewhere in the
    /// properties.
    /// </value>
    public override string Keywords
    {
        
        get
        {
            return _target.Keywords;
        }

        set
        {
            _target.Keywords = value;
        }
    }

    /// <value>
    /// The description or abstract of the contents.
    /// </value>
    public override string Description
    {
        
        get
        {
            return _target.Description;
        }

        set
        {
             _target.Description = value;
        }
    }

    /// <value>
    /// The user who performed the last modification. The identification is
    /// environment-specific and can consist of a name, email address,
    /// employee ID, etc. It is recommended that this value be only as
    /// verbose as necessary to identify the individual.
    /// </value>
    public override string LastModifiedBy
    {
        
        get
        {
            return _target.LastModifiedBy;
        }

        set
        {
            _target.LastModifiedBy = value;
        }
    }

    /// <value>
    /// The revision number. This value indicates the number of saves or
    /// revisions. The application is responsible for updating this value
    /// after each revision.
    /// </value>
    public override string Revision
    {
   
        get
        {
            return _target.Revision;
        }

        set
        {
            _target.Revision = value;
        }
    }

    /// <value>
    /// The date and time of the last printing.
    /// </value>
    public override Nullable<DateTime> LastPrinted
    {
        
        get
        {
            return _target.LastPrinted;
        }

        set
        {
            _target.LastPrinted = value;
        }
    }

    /// <value>
    /// The creation date and time.
    /// </value>
    public override Nullable<DateTime> Created
    {
        
        get
        {
            return _target.Created;
        }

        set
        {
            _target.Created = value;
        }
    }

    /// <value>
    /// The date and time of the last modification.
    /// </value>
    public override Nullable<DateTime> Modified
    {
        
        get
        {
            return _target.Modified;
        }

        set
        {
            _target.Modified = value;
        }
    }
    #endregion SummaryInformation properties

    #region DocumentSummaryInformation properties

    /// <value>
    /// The category. This value is typically used by UI applications to create 
    /// navigation controls.
    /// </value>
    public override string Category
    {
        
        get
        {
            return _target.Category;
        }

        set
        {
            _target.Category = value;
        }    }

    /// <value>
    /// A unique identifier.
    /// </value>
    public override string Identifier
    {
        
        get
        {
             return _target.Identifier;
        }

        set
        {
            _target.Identifier = value;
        }
    }

    /// <value>
    /// The type of content represented, generally defined by a specific
    /// use and intended audience. Example values include "Whitepaper",
    /// "Security Bulletin", and "Exam". (This property is distinct from
    /// MIME content types as defined in RFC 2045.) 
    /// </value>
    public override string ContentType
    {
        
        get
        {
            return _target.ContentType;
        }

        set
        {
            _target.ContentType = value;
        }
    }

    /// <value>
    /// The primary language of the package content. The language tag is
    /// composed of one or more parts: A primary language subtag and a
    /// (possibly empty) series of subsequent subtags, for example, "EN-US".
    /// These values MUST follow the convention specified in RFC 3066.
    /// </value>
    public override string Language
    {
        
        get
        {
            return _target.Language;
        }

        set
        {
            _target.Language = value;
        }
    }

    /// <value>
    /// The version number. This value is set by the user or by the application.
    /// </value>
    public override string Version
    {
        
        get
        {
            return _target.Version;
        }

        set
        {
            _target.Version = value;
        }
    }

    /// <value>
    /// The status of the content. Example values include "Draft",
    /// "Reviewed", and "Final".
    /// </value>
    public override string ContentStatus
    {
        
        get
        {
            return _target.ContentStatus;
        }

        set
        {
             _target.ContentStatus = value;
        }
    }

    #endregion DocumentSummaryInformation properties
    #endregion Public Properties

    #region IDisposable
    //--------------------------------------------------------------------------
    //  IDisposable Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Dispose(bool disposing) executes in two distinct scenarios.
    /// If disposing equals true, the method has been called directly
    /// or indirectly by a user's code. Managed and unmanaged resources
    /// can be disposed.
    ///
    /// If disposing equals false, the method has been called by the 
    /// runtime from inside the finalizer and you should not reference 
    /// other objects. Only unmanaged resources can be disposed.
    ///
    /// This class does have unmanaged resources, namely the OLE property
    /// storage interface pointers.
    /// </summary>
    /// <param name="disposing">
    /// true if called from Dispose(); false if called from the finalizer.
    /// </param>
    protected override void
    Dispose(
        bool disposing
        )
    {
        try
        {
            base.Dispose(true);
        }
        finally
        {
            _target.Dispose();
        }
    }

    #endregion IDisposable

    private PackageProperties _target;
}
}
