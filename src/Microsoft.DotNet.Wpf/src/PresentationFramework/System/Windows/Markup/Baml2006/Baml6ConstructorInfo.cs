// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Baml2006
{
    internal struct Baml6ConstructorInfo
    {
        public Baml6ConstructorInfo(List<Type> types, Func<Object[], object> ctor)
        {
            _types = types;
            _constructor = ctor;
        }

        private List<Type> _types;
        private Func<Object[], object> _constructor;

        public List<Type> Types { get { return _types; } }
        public Func<Object[], object> Constructor { get { return _constructor; } }
    }
}
