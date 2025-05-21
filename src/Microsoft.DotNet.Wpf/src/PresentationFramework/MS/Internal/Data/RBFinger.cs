// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Finger into a red-black tree.
//

namespace MS.Internal.Data
{
    internal struct RBFinger<T>
    {
        public RBNode<T> Node { get; set; }
        public int Offset { get; set; }
        public int Index { get; set; }
        public bool Found { get; set; }
        public T Item { get { return Node.GetItemAt(Offset); } }
        public void SetItem(T x) { Node.SetItemAt(Offset, x); }
        public bool IsValid { get { return Node != null && Node.HasData; } }

        public static RBFinger<T> operator +(RBFinger<T> finger, int delta)
        {
            if (delta >= 0)
                for (; delta > 0 && finger.IsValid; --delta) ++finger;
            else
                for (; delta < 0 && finger.IsValid; ++delta) --finger;
            return finger;
        }

        public static RBFinger<T> operator -(RBFinger<T> finger, int delta)
        {
            return finger + (-delta);
        }

        public static int operator -(RBFinger<T> f1, RBFinger<T> f2)
        {
            return f1.Index - f2.Index;
        }

        public static RBFinger<T> operator ++(RBFinger<T> finger)
        {
            finger.Offset += 1;
            finger.Index += 1;
            if (finger.Offset == finger.Node.Size)
            {
                finger.Node = finger.Node.GetSuccessor();
                finger.Offset = 0;
            }
            return finger;
        }

        public static RBFinger<T> operator --(RBFinger<T> finger)
        {
            finger.Offset -= 1;
            finger.Index -= 1;
            if (finger.Offset < 0)
            {
                finger.Node = finger.Node.GetPredecessor();
                if (finger.Node != null)
                    finger.Offset = finger.Node.Size - 1;
            }
            return finger;
        }

        public static bool operator <(RBFinger<T> f1, RBFinger<T> f2)
        {
            return (f1.Index < f2.Index);
        }

        public static bool operator >(RBFinger<T> f1, RBFinger<T> f2)
        {
            return (f1.Index > f2.Index);
        }
    }
}
