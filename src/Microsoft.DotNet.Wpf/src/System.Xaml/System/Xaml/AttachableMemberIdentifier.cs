// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml
{
    public class AttachableMemberIdentifier : IEquatable<AttachableMemberIdentifier>
    {
        public AttachableMemberIdentifier(Type declaringType, string memberName)
        {
            DeclaringType = declaringType;
            MemberName = memberName;
        }

        public string MemberName { get; }

        public Type DeclaringType { get; }

        public static bool operator !=(AttachableMemberIdentifier left, AttachableMemberIdentifier right)
        {
            return !(left == right);
        }

        public static bool operator ==(AttachableMemberIdentifier left, AttachableMemberIdentifier right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AttachableMemberIdentifier);
        }

        public bool Equals(AttachableMemberIdentifier other)
        {
            if (other == null)
            {
                return false;
            }

            return this.DeclaringType == other.DeclaringType && this.MemberName == other.MemberName;
        }

        public override int GetHashCode()
        {
            int a = this.DeclaringType == null ? 0 : this.DeclaringType.GetHashCode();
            int b = this.MemberName == null ? 0 : this.MemberName.GetHashCode();
            return ((a << 5) + a) ^ b;
        }

        public override string ToString()
        {
            if (this.DeclaringType == null)
            {
                return this.MemberName;
            }

            return this.DeclaringType.ToString() + "." + MemberName;
        }
    }
}
