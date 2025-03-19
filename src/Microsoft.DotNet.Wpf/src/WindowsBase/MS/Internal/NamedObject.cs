// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace MS.Internal
{
    /// <summary>
    /// An instance of this class can be used wherever you might otherwise use
    /// "new Object()".  The name will show up in the debugger, instead of
    /// merely "{object}"
    /// </summary>
    internal class NamedObject
    {
        public NamedObject(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(name);

            _name = name;
        }

        public override string ToString()
        {
            if (_name[0] != '{')
            {
                // lazily add {} around the name, to avoid allocating a string
                // until it's actually needed
                _name = String.Format(CultureInfo.InvariantCulture, "{{{0}}}", _name);
            }

            return _name;
        }

        private string _name;
    }
}
