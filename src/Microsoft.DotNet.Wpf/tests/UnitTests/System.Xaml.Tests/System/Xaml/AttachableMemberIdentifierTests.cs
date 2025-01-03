// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests;

public class AttachableMemberIdentifierTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(typeof(int), "")]
    [InlineData(typeof(string), "memberName")]
    public void Ctor_Type_String(Type? declaringType, string? memberName)
    {
        var identifier = new AttachableMemberIdentifier(declaringType, memberName);
        Assert.Equal(declaringType, identifier.DeclaringType);
        Assert.Equal(memberName, identifier.MemberName);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var identifier = new AttachableMemberIdentifier(typeof(int), "memberName");
        yield return new object?[] { identifier, identifier, true };
        yield return new object?[] { identifier, new AttachableMemberIdentifier(typeof(int), "memberName"), true };
        yield return new object?[] { identifier, new AttachableMemberIdentifier(typeof(string), "memberName"), false };
        yield return new object?[] { identifier, new AttachableMemberIdentifier(null, "memberName"), false };
        yield return new object?[] { identifier, new AttachableMemberIdentifier(typeof(int), "otherMemberName"), false };
        yield return new object?[] { identifier, new AttachableMemberIdentifier(typeof(int), null), false };
        yield return new object?[] { new AttachableMemberIdentifier(null, null), new AttachableMemberIdentifier(null, null), true };
        yield return new object?[] { new AttachableMemberIdentifier(null, null), new AttachableMemberIdentifier(typeof(int), null), false };
        yield return new object?[] { new AttachableMemberIdentifier(null, null), new AttachableMemberIdentifier(null, "memberName"), false };

        yield return new object?[] { identifier, new object(), false };
        yield return new object?[] { identifier, null, false };
        yield return new object?[] { null, identifier, false };
        yield return new object?[] { null, null, true };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(AttachableMemberIdentifier identifier, object obj, bool expected)
    {
        AttachableMemberIdentifier? other = obj as AttachableMemberIdentifier;
        if (other != null || obj == null)
        {
            if (identifier != null)
            {
                Assert.Equal(expected, identifier.Equals(other));
                if (other != null)
                {
                    Assert.Equal(expected, identifier.GetHashCode().Equals(other.GetHashCode()));
                }
            }
            Assert.Equal(expected, identifier == other);
            Assert.Equal(!expected, identifier != other);
        }

        if (identifier != null)
        {
            Assert.Equal(expected, identifier.Equals(obj));
        }
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        yield return new object[] { new AttachableMemberIdentifier(null, "memberName"), "memberName" };
        yield return new object[] { new AttachableMemberIdentifier(typeof(int), "memberName"), "System.Int32.memberName" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(AttachableMemberIdentifier identifier, string expected)
    {
        Assert.Equal(expected, identifier.ToString());
    }
}
