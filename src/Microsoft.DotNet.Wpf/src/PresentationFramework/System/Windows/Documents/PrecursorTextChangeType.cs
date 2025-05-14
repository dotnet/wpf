// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: 
//

namespace System.Windows.Documents
{
    // These are possible changes added to a change list.
    // ElementAdded/Extracted don't make sense after multiple
    // changes are combined.
    internal enum PrecursorTextChangeType
    { 
        ContentAdded = TextChangeType.ContentAdded,
        ContentRemoved = TextChangeType.ContentRemoved,
        PropertyModified = TextChangeType.PropertyModified,
        ElementAdded,
        ElementExtracted
    }
}
