// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: 
//

namespace System.Windows.Documents
{
    /// <summary>
    /// Specifies the type of change applied to TextContainer content.
    /// </summary>
    internal enum TextChangeType
    { 
        /// <summary>
        /// New content was inserted.
        /// </summary>
        ContentAdded,

        /// <summary>
        /// Content was deleted.
        /// </summary>
        ContentRemoved, 

        /// <summary>
        /// A local DependencyProperty value changed.
        /// </summary>
        PropertyModified,
    }
}
