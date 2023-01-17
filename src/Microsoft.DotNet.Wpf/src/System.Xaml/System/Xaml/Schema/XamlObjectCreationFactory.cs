// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace System.Xaml.Schema
{
    public static class XamlObjectCreationFactory
    {
        public static void RegisterCreator<T>(Func<T> creator)
        {
            if (creator == null) throw new ArgumentNullException(nameof(creator));
            RegisterCreator(typeof(T), () => creator());
        }

        public static void RegisterCreator(Type type, Func<object> creator)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            s_xamlObjectCreatorDictionary[type] = creator ?? throw new ArgumentNullException(nameof(creator));

            HasBeenRegister = true;
        }

        internal static bool HasBeenRegister { get; private set; }

        internal static bool TryGetCreator(Type type, out Func<object> creator)
            => s_xamlObjectCreatorDictionary.TryGetValue(type, out creator);

        private static readonly ConcurrentDictionary<Type, Func<object>> s_xamlObjectCreatorDictionary =
            new ConcurrentDictionary<Type, Func<object>>();
    }
}
