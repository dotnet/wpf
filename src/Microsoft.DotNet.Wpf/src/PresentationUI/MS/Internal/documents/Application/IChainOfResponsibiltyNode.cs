// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Defines the interface for participating in a ChainOfResponsibility.

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Defines the interface for participating in a ChainOfResponsibility.
/// </summary>
/// <typeparam name="T">A type common to all members of the chain.</typeparam>
internal interface IChainOfResponsibiltyNode<T>
{
    /// <summary>
    /// When true the member would like to try and handle the subject.
    /// </summary>
    /// <param name="subject">The subject to perform an action on.</param>
    /// <returns>Ture if the member would like an oppertuntity to handle this
    /// this subject.</returns>
    bool IsResponsible(T subject);
}
}
