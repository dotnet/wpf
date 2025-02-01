// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description:
//   A base class that allows storing parser records for later instantiation.
//

namespace System.Windows.Markup
{
    /// <summary>
    ///     A base class that allows storing parser records for later instantiation.
    /// </summary>
    public abstract class XamlInstanceCreator
    {
        /// <summary>
        ///     Creates the object that this factory represents.
        /// </summary>
        /// <returns>The instantiated object.</returns>
        public abstract object CreateObject();
    }
}
