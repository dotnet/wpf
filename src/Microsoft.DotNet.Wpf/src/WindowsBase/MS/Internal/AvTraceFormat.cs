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
    // This class represents the details of a single trace (the ID, a formatted message string, and parameter labels).
    // This is used by the AvTrace class and callers.
    
    internal class AvTraceFormat : AvTraceDetails
    {
        public AvTraceFormat( AvTraceDetails details, object[] args ) : base(details.Id, details.Labels)
        {
            _message = string.Format(System.Globalization.CultureInfo.InvariantCulture, details.Labels[0], args);
        }

        // main message (formatted)
        public override string Message
        {
            get
            {
                return _message;
            }
        }

        string _message;
    }
}

