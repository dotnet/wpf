// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Windows;

namespace PresentationFramework.Win11.Controls
{
    /// <summary>
    /// Represents information about the visual states of font elements that represent
    /// a rating.
    /// </summary>
    public class RatingItemFontInfo : RatingItemInfo
    {
        /// <summary>
        /// Initializes a new instance of the RatingItemFontInfo class.
        /// </summary>
        public RatingItemFontInfo()
        {
        }

        #region DisabledGlyph

        /// <summary>
        /// Identifies the DisabledGlyph dependency property.
        /// </summary>
        public static readonly DependencyProperty DisabledGlyphProperty =
            DependencyProperty.Register(
                nameof(DisabledGlyph),
                typeof(string),
                typeof(RatingItemFontInfo),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element
        /// that is disabled.
        /// </summary>
        /// <returns>The hexadecimal character code for the rating element glyph.</returns>
        public string DisabledGlyph
        {
            get => (string)GetValue(DisabledGlyphProperty);
            set => SetValue(DisabledGlyphProperty, value);
        }

        #endregion

        #region Glyph

        /// <summary>
        /// Identifies the Glyph dependency property.
        /// </summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(
                nameof(Glyph),
                typeof(string),
                typeof(RatingItemFontInfo),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element
        /// that has been set by the user.
        /// </summary>
        /// <returns>The hexadecimal character code for the rating element glyph.</returns>
        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        #endregion

        #region PlaceholderGlyph

        /// <summary>
        /// Identifies the PlaceholderGlyph dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderGlyphProperty =
            DependencyProperty.Register(
                nameof(PlaceholderGlyph),
                typeof(string),
                typeof(RatingItemFontInfo),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element
        /// that is showing a placeholder value.
        /// </summary>
        /// <returns>The hexadecimal character code for the rating element glyph.</returns>
        public string PlaceholderGlyph
        {
            get => (string)GetValue(PlaceholderGlyphProperty);
            set => SetValue(PlaceholderGlyphProperty, value);
        }

        #endregion

        #region PointerOverGlyph

        /// <summary>
        /// Identifies the PointerOverGlyph dependency property.
        /// </summary>
        public static readonly DependencyProperty PointerOverGlyphProperty =
            DependencyProperty.Register(
                nameof(PointerOverGlyph),
                typeof(string),
                typeof(RatingItemFontInfo),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element
        /// that has the pointer over it.
        /// </summary>
        /// <returns>The hexadecimal character code for the rating element glyph.</returns>
        public string PointerOverGlyph
        {
            get => (string)GetValue(PointerOverGlyphProperty);
            set => SetValue(PointerOverGlyphProperty, value);
        }

        #endregion

        #region PointerOverPlaceholderGlyph

        /// <summary>
        /// Identifies the PointerOverPlaceholderGlyph dependency property.
        /// </summary>
        public static readonly DependencyProperty PointerOverPlaceholderGlyphProperty =
            DependencyProperty.Register(
                nameof(PointerOverPlaceholderGlyph),
                typeof(string),
                typeof(RatingItemFontInfo),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element
        /// showing a placeholder value with the pointer over it.
        /// </summary>
        /// <returns>The hexadecimal character code for the rating element glyph.</returns>
        public string PointerOverPlaceholderGlyph
        {
            get => (string)GetValue(PointerOverPlaceholderGlyphProperty);
            set => SetValue(PointerOverPlaceholderGlyphProperty, value);
        }

        #endregion

        #region UnsetGlyph

        /// <summary>
        /// Identifies the UnsetGlyph dependency property.
        /// </summary>
        public static readonly DependencyProperty UnsetGlyphProperty =
            DependencyProperty.Register(
                nameof(UnsetGlyph),
                typeof(string),
                typeof(RatingItemFontInfo),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element
        /// that has not been set.
        /// </summary>
        /// <returns>The hexadecimal character code for the rating element glyph.</returns>
        public string UnsetGlyph
        {
            get => (string)GetValue(UnsetGlyphProperty);
            set => SetValue(UnsetGlyphProperty, value);
        }

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new RatingItemFontInfo();
        }
    }
}
