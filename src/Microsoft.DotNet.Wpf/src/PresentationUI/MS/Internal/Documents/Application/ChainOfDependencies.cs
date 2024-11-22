// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// A Generic that provides user with the ability to chain dependent objects
// of a shared base type and perform actions on them in order of dependency.

namespace MS.Internal.Documents.Application
{
/// <summary>
/// A Generic that provides user with the ability to chain dependent objects
/// of a shared base type and perform actions on them in order of dependency.
/// </summary>
/// <remarks>
/// This is different from the chain of responsiblity in the following ways:
/// 
///  - Order of execution in the chain can be inversed by calling LastToFirst.
///  - The same operation is performed on each member once.
/// 
/// This class has many methods which are intentionaly recursive.  There is
/// currently no validation to prevent cyclic dependencies.  As the chain is
/// currently fixed at compile time there is no need; StackOverFlowException
/// durring testing is fine.
/// </remarks>
/// <typeparam name="T">A type common to all in the chain.</typeparam>
internal static class ChainOfDependencies<T> where T : IChainOfDependenciesNode<T>
{
    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Gets the last member in the chain. (The one with no dependencies.)
    /// </summary>
    /// <param name="member">The current member.</param>
    /// <returns>The last member in the chain. (The one with no dependencies.)
    /// </returns>
    internal static T GetLast(T member)
    {
        T last = member;

        if (member.Dependency != null)
        {
            last = GetLast(member.Dependency);
        }

        return last;
    }

    /// <summary>
    /// Will perform the action from the member with no dependencies to the most
    /// dependent member.
    /// </summary>
    /// <param name="member">The member on which to perform the action.</param>
    /// <param name="action">The action to perform on the member.</param>
    /// <returns>Returns true if all the actions returned true.</returns>
    internal static bool OrderByLeastDependent(
        T member,
        ChainOfDependencies<T>.Action action)
    {
        bool satisfied = true;

        T nextInChain = member.Dependency;

        if (nextInChain != null)
        {
            satisfied = OrderByLeastDependent(nextInChain, action);
        }

        if (satisfied)
        {
            satisfied = action(member);
        }
        else
        {
            Trace.SafeWrite(
                 Trace.File,
               "Dependency for {0}#{1} was not satisfied skipping action.",
                member.GetType(),
                member.GetHashCode());
        }

        return satisfied;
    }

    /// <summary>
    /// Will perform the action from most dependent to not dependent.
    /// </summary>
    /// <param name="member">The member on which to perform the action.</param>
    /// <param name="action">The action to perform on the member.</param>
    /// <returns>Returns true if the all the actions returned true.</returns>
    internal static bool OrderByMostDependent(
        T member,
        ChainOfDependencies<T>.Action action)
    {
        bool satisfied = action(member);

        T nextInChain = member.Dependency;

        if (satisfied)
        {
            if (nextInChain != null)
            {
                satisfied = OrderByMostDependent(nextInChain, action);
            }
        }
        else
        {
            Trace.SafeWrite(
                Trace.File,
                "Dependency for {0}#{1} was not satisfied skipping action.",
                member.GetType(),
                member.GetHashCode());
        }

        return satisfied;
    }
    #endregion Internal Methods

    #region Internal Delegates
    //--------------------------------------------------------------------------
    // Internal Delegates
    //--------------------------------------------------------------------------

    /// <summary>
    /// An action to perform on a ChainOfDependencies member.
    /// </summary>
    /// <param name="member">The member on which to perform the action.</param>
    /// <returns>True if the dependency was satisfied.</returns>
    internal delegate bool Action(T member);
    #endregion Internal Delegates
}
}
