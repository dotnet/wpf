// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Base class for null records.
    /// </summary>
    internal abstract partial class NullRecord
    {
        private Count _count;

        public virtual Count NullCount
        {
            get => _count;
            private protected set
            {
                if (value == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _count = value;
            }
        }

        internal static void Write(BinaryWriter writer, int nullCount)
        {
            switch (nullCount)
            {
                case 1:
                    ObjectNull.Instance.Write(writer);
                    break;
                case <= 255:
                    new ObjectNullMultiple256(nullCount).Write(writer);
                    break;
                default:
                    new ObjectNullMultiple(nullCount).Write(writer);
                    break;
            }
        }
    }
}
