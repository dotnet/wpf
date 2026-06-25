// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml.Tests.Common;

public struct Optional<T>
{
    public bool HasValue { get; private set; }
    public T Value { get; private set; }

    public readonly T Or(T value) => HasValue ? Value : value;

    public readonly T Or(Func<T> valueFactory) => HasValue ? Value : valueFactory();

    public readonly T Or<TArg>(Func<TArg, T> valueFactory, TArg arg) => HasValue ? Value : valueFactory(arg);

    public readonly T Or<TArg1, TArg2>(Func<TArg1, TArg2, T> valueFactory, TArg1 arg1, TArg2 arg2) => HasValue ? Value : valueFactory(arg1, arg2);

    public readonly T Or<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, T> valueFactory, TArg1 arg1, TArg2 arg2, TArg3 arg3) => HasValue ? Value : valueFactory(arg1, arg2, arg3);

    public readonly T Or<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, T> valueFactory, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) => HasValue ? Value : valueFactory(arg1, arg2, arg3, arg4);

    public readonly T Or<TArg1, TArg2, TArg3, TArg4, TArg5>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, T> valueFactory, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) => HasValue ? Value : valueFactory(arg1, arg2, arg3, arg4, arg5);

    public static implicit operator Optional<T>(T t)
    {
        return new Optional<T>
        {
            HasValue = true,
            Value = t
        };
    }
}
