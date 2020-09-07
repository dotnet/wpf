// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  The CriticalFileToken class ensures file represented is the one the
//  user has authorized us to manipulate.

using System;
using System.Security;

using MS.Internal.PresentationUI;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// The CriticalFileToken class ensures file represented is the one the
/// user has authorized us to manipulate.
/// </summary>
/// <remarks>
/// Responsibility:
/// Allow XpsViewer to safely pass around information on which file the user
/// has authorized us to manipulate on thier behalf.  Ensure that the creator
/// of the object has the privledge to manipulate the file represented.
/// 
/// Design Comments:
/// Many classes need to perform privledged operations files on behalf of the
/// user.  However only DocObjHost and FilePresentation can assert it is user
/// sourced data.
///
/// As such we need them to create this 'token' which will will use as the only
/// source of authoritative information for which files we are manipulating.
/// </remarks>
[FriendAccessAllowed]
internal sealed class CriticalFileToken
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    internal CriticalFileToken(Uri location)
    {
        _location = location;
    }
    #endregion Constructors

    #region Object Members
    //--------------------------------------------------------------------------
    // Object Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// Compares the values.
    /// </summary>
    /// <returns>True if they are equal.</returns>
    public static bool operator ==(CriticalFileToken a, CriticalFileToken b)
    {
        bool result = false;

        if (((object)a) == null)
        {
            if (((object)b) == null)
            {
                result = true;
            }
        }
        else
        {
            if (((object)b) != null)
            {
                result = a._location.ToString().Equals(
                b._location.ToString(),
                StringComparison.OrdinalIgnoreCase);
            }
        }
        return result;
    }

    /// <summary>
    /// Compares the values.
    /// </summary>
    public static bool operator !=(CriticalFileToken a, CriticalFileToken b)
    {
        
        return !(a==b);
    }

    /// <summary>
    /// Compares the values.
    /// </summary>
    public override bool Equals(object obj)
    {
        return (this == (obj as CriticalFileToken));
    }

    /// <summary>
    /// See Object.GetHashCode();
    /// </summary>
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    #endregion Object Members

    #region Internal Properties
    //--------------------------------------------------------------------------
    // Internal Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// The location for which the creator satisfied ReadWrite access.
    /// </summary>
    internal Uri Location
    {
        get
        {
            return _location;
        }
    }
    #endregion Internal Properties

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    private Uri _location;
    #endregion Private Fields
}
}
