// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Windows;

namespace PresentationFramework.Win11.Controls
{
    /// <summary>
    /// Represents information about the visual states of the elements that represent
    /// a rating.
    /// </summary>
    public class RatingItemInfo : Freezable
    {
        /// <summary>
        /// Initializes a new instance of the RatingItemInfo class.
        /// </summary>
        public RatingItemInfo()
        {
        }

        protected override Freezable CreateInstanceCore()
        {
            return new RatingItemInfo();
        }
    }
}
