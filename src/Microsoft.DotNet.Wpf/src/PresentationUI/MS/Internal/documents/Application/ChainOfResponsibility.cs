// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// An implementation of the 'Chain of Responsibility' from Design Patterns

using System.Collections.Generic;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// An implementation of the 'Chain of Responsibility' from Design Patterns
/// </summary>
/// <remarks>
/// Design Comments:
/// 
/// The pattern is implemented as:
/// 
///  - concrete coupling of Ts (successors) at construction
///  - request are represented by ChainOfResponsiblity[T, S].Action delegate
///    where S is the parameter data
///  - IChainOfResponsibiltyNode[S] is used to determin if the member is willing
///    to participate in the request.
/// </remarks>
/// <typeparam name="T">A common type for all members of the chain.</typeparam>
/// <typeparam name="S">A common type for data for all members of the chain.
/// </typeparam>
internal class ChainOfResponsiblity<T,S> where T : IChainOfResponsibiltyNode<S>
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// Provides for concrete coupling of T's at construction.
    /// </summary>
    /// <param name="members"></param>
    internal ChainOfResponsiblity(
        params T[] members)
    {
        _members = new List<T>(members);
    }
    #endregion Constructors

    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will dispatch the action first to last in the chain until a member
    /// reports handling the action.
    /// </summary>
    /// <returns>True if successfully handled by a member.</returns>
    /// <param name="action">The action to perform.</param>
    /// <param name="subject">The subject to perform it on.</param>
    internal bool Dispatch(ChainOfResponsiblity<T, S>.Action action, S subject)
    {
        bool handled = false;

        foreach (T member in _members)
        {
            if (member.IsResponsible(subject))
            {
                Trace.SafeWrite(
                    Trace.File,
                    "Dispatching {0} to {1} using {2}.",
                    action.Method.Name,
                    member.GetType().Name,
                    subject.GetType().Name);

                handled = action(member, subject);
                if (handled)
                {
                    Trace.SafeWrite(
                        Trace.File,
                       "Finished {0} by {1} with {2}.",
                        action.Method.Name,
                        member.GetType().Name,
                        subject.GetType().Name);
                    break;
                }
            }
        }
        return handled;
    }
    #endregion Internal Methods

    #region Internal Delegates
    //--------------------------------------------------------------------------
    // Internal Delegates
    //--------------------------------------------------------------------------

    /// <summary>
    /// Actions which members T can be perform on S.
    /// </summary>
    /// <param name="member">The member to perform the action.</param>
    /// <param name="subject">The subject to perform the action on.</param>
    /// <returns>True if handled by the member.</returns>
    internal delegate bool Action(T member, S subject);
    #endregion Internal Delegates

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    /// <summary>
    /// The concrete list of members.
    /// </summary>
    private List<T> _members;
    #endregion Private Fields
}
}
