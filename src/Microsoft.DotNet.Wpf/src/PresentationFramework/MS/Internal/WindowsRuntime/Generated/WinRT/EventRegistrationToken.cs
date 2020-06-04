using System;
using System.Collections.Generic;
using System.Text;

namespace WinRT
{
    internal struct EventRegistrationToken : IEquatable<EventRegistrationToken>
    {
        public long Value;

        public EventRegistrationToken(long _Value)
        {
            Value = _Value;
        }

        public static bool operator ==(EventRegistrationToken x, EventRegistrationToken y)
        {
            return x.Value == y.Value;
        }

        public static bool operator !=(EventRegistrationToken x, EventRegistrationToken y) => !(x == y);

        public bool Equals(EventRegistrationToken other) => this == other;

        public override bool Equals(object obj)
        {
            return obj is EventRegistrationToken that && this == that;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}

namespace ABI.WinRT
{
    internal static class EventRegistrationToken
    {
        public static string GetGuidSignature() => "struct(Windows.Foundation.EventRegistrationToken;i8)";
    }
}
