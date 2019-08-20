// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

namespace System.Windows.Media.Animation
{
    internal struct ResolvedKeyFrameEntry : IComparable
    {
        internal Int32 _originalKeyFrameIndex;
        internal TimeSpan _resolvedKeyTime;

        public Int32 CompareTo(object other)
        {
            ResolvedKeyFrameEntry otherEntry = (ResolvedKeyFrameEntry)other;

            if (otherEntry._resolvedKeyTime > _resolvedKeyTime)
            {
                return -1;
            }
            else if (otherEntry._resolvedKeyTime < _resolvedKeyTime)
            {
                return 1;
            }
            else
            {
                if (otherEntry._originalKeyFrameIndex > _originalKeyFrameIndex)
                {
                    return -1;
                }
                else if (otherEntry._originalKeyFrameIndex < _originalKeyFrameIndex)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
