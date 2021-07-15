// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*
\***************************************************************************/


namespace MS.Internal
{
    // This class represents the details of a single trace (the ID and message strings).
    // This is used by the AvTrace class and callers.
    
    internal class AvTraceDetails
    {
        public AvTraceDetails( int id, string[] labels )
        {
            _id = id;
            _labels = labels;
        }

        // Each trace has a differnt ID
        public int Id
        {
            get
            {
                return _id;
            }
        }

        // base trace message/format-string
        public virtual string Message
        {
            get
            {
                return _labels[0];
            }
        }

        // parameter labels (with base trace message/format-string at index 0)
        public string[] Labels
        {
            get
            {
                return _labels;
            }
        }

        int _id;
        string[] _labels;
    }
}

