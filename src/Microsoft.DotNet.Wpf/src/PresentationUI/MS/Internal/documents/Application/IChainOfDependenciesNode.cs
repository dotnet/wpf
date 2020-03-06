// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Defines the interface for participating in a ChainOfDependencies.

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Defines the interface for participating in a ChainOfDependencies.
/// <seealso cref="MS.Internal.Documents.Application.ChainOfDependencies<T>"/>
/// </summary>
/// <typeparam name="T">A type common to all in the chain.</typeparam>
internal interface IChainOfDependenciesNode<T>
{
    /// <summary>
    /// The next dependency in the chain.  Null indicates there are no
    /// further dependencies.
    /// </summary>
    T Dependency
    {
        get;
    }
}
}
