// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿

namespace System.Windows.Shell
{
    using System.Windows;

    public class ThumbButtonInfoCollection : FreezableCollection<ThumbButtonInfo>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new ThumbButtonInfoCollection();
        }

        /// <summary>
        /// A frozen empty ThumbButtonInfoCollection.
        /// </summary>
        internal static ThumbButtonInfoCollection Empty
        {
            get
            {
                if (s_empty == null)
                {
                    var collection = new ThumbButtonInfoCollection();
                    collection.Freeze();
                    s_empty = collection;
                }

                return s_empty;
            }
        }

        private static ThumbButtonInfoCollection s_empty;
    }
}
