// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using MS.Internal;
using MS.Utility;
using MS.Internal.Documents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     BulletDecorator is used for decorating a generic content of type UIElement.
    /// Usually, the content is a text and the bullet is a glyph representing
    /// something similar to a checkbox or a radiobutton.
    /// Bullet property is used to decorate the content by aligning itself with the first line of the content text.
    /// </summary>
    public class BulletDecorator : Decorator
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default BulletDecorator constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher.
        /// Use alternative constructor that accepts a Dispatcher for best performance.
        /// </remarks>
        public BulletDecorator() : base()
        {
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Properties

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
                Panel.BackgroundProperty.AddOwner(typeof(BulletDecorator),
                        new FrameworkPropertyMetadata(
                                (Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Background property defines the brush used to fill the area within the BulletDecorator.
        /// </summary>
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Bullet property is the first visual element in BulletDecorator visual tree.
        /// It should be aligned to BulletDecorator.Child which is the second visual child.
        /// </summary>
        /// <value></value>
        public UIElement Bullet
        {
            get
            {
                return _bullet;
            }
            set
            {
                if (_bullet != value)
                {
                    if (_bullet != null)
                    {
                        // notify the visual layer that the old bullet has been removed.
                        RemoveVisualChild(_bullet);

                        //need to remove old element from logical tree
                        RemoveLogicalChild(_bullet);
                    }

                    _bullet = value;

                    AddLogicalChild(value);
                    // notify the visual layer about the new child.
                    AddVisualChild(value);

                    // If we decorator content exists we need to move it at the end of the visual tree
                    UIElement child = Child;
                    if (child != null)
                    {
                        RemoveVisualChild(child);
                        AddVisualChild(child);
                    }

                    InvalidateMeasure();
                }
            }
        }

        #endregion Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary> 
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (_bullet == null)
                {
                    return base.LogicalChildren;
                }

                if (Child == null)
                {
                    return new SingleChildEnumerator(_bullet);
                }

                return new DoubleChildEnumerator(_bullet, Child);
            }
        }

        private class DoubleChildEnumerator : IEnumerator
        {
            internal DoubleChildEnumerator(object child1, object child2)
            {
                Debug.Assert(child1 != null, "First child should be non-null.");
                Debug.Assert(child2 != null, "Second child should be non-null.");

                _child1 = child1;
                _child2 = child2;
            }

            object IEnumerator.Current
            {
                get
                {
                    switch (_index)
                    {
                        case 0:
                            return _child1;
                        case 1:
                            return _child2;
                        default:
                            return null;
                    }
                }
            }

            bool IEnumerator.MoveNext()
            {
                _index++;
                return _index < 2;
            }

            void IEnumerator.Reset()
            {
                _index = -1;
            }

            private int _index = -1;
            private object _child1;
            private object _child2;
        }

        /// <summary>
        /// Override from UIElement
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            // Draw background in rectangle inside border.
            Brush background = this.Background;
            if (background != null)
            {
                dc.DrawRectangle(background,
                                 null,
                                 new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }
        }

        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return (Child == null ? 0 : 1) + (_bullet == null ? 0 : 1); }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index > VisualChildrenCount-1)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            if (index == 0 && _bullet != null)
            {
                return _bullet;
            }

            return Child;
        }
        /// <summary>
        /// Updates DesiredSize of the BulletDecorator. Called by parent UIElement.
        /// This is the first pass of layout.
        /// </summary>
        /// <param name="constraint">Constraint size is an "upper limit" that BulletDecorator should not exceed.</param>
        /// <returns>BulletDecorator' desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
                Size bulletSize = new Size();
                Size contentSize = new Size();
                UIElement bullet = Bullet;
                UIElement content = Child;

                // If we have bullet we should measure it first
                if (bullet != null)
                {
                    bullet.Measure(constraint);
                    bulletSize = bullet.DesiredSize;
                }

                // If we have second child (content) we should measure it
                if (content != null)
                {
                    Size contentConstraint = constraint;
                    contentConstraint.Width = Math.Max(0.0, contentConstraint.Width - bulletSize.Width);

                    content.Measure(contentConstraint);
                    contentSize = content.DesiredSize;
                }

                Size desiredSize = new Size(bulletSize.Width + contentSize.Width, Math.Max(bulletSize.Height, contentSize.Height));
                return desiredSize;
        }

        /// <summary>
        /// BulletDecorator arranges its children - Bullet and Child.
        /// Bullet is aligned vertically with the center of the content's first line
        /// </summary>
        /// <param name="arrangeSize">Size that BulletDecorator will assume to position children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
                UIElement bullet = Bullet;
                UIElement content = Child;
                double contentOffsetX = 0;

                double bulletOffsetY = 0;

                Size bulletSize = new Size();

                // Arrange the bullet if exist
                if (bullet != null)
                {
                    bullet.Arrange(new Rect(bullet.DesiredSize));
                    bulletSize = bullet.RenderSize;

                    contentOffsetX = bulletSize.Width;
                }

                // Arrange the content if exist
                if (content != null)
                {
                    // Helper arranges child and may substitute a child's explicit properties for its DesiredSize.
                    // The actual size the child takes up is stored in its RenderSize.
                    Size contentSize = arrangeSize;
                    if (bullet != null)
                    {
                        contentSize.Width = Math.Max(content.DesiredSize.Width, arrangeSize.Width - bullet.DesiredSize.Width);
                        contentSize.Height = Math.Max(content.DesiredSize.Height, arrangeSize.Height);
                    }
                    content.Arrange(new Rect(contentOffsetX, 0, contentSize.Width, contentSize.Height));

                    double centerY = GetFirstLineHeight(content) * 0.5d;
                    bulletOffsetY += Math.Max(0d, centerY - bulletSize.Height * 0.5d);
                }

                // Re-Position the bullet if exist
                if (bullet != null && !DoubleUtil.IsZero(bulletOffsetY))
                {
                    bullet.Arrange(new Rect(0, bulletOffsetY, bullet.DesiredSize.Width, bullet.DesiredSize.Height));
                }

                return arrangeSize;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // This method calculates the height of the first line if the element is TextBlock or FlowDocumentScrollViewer
        // Otherwise returns the element height
        private double GetFirstLineHeight(UIElement element)
        {
            // We need to find TextBlock/FlowDocumentScrollViewer if it is nested inside ContentPresenter
            // Common scenario when used in styles is that BulletDecorator content is a ContentPresenter
            UIElement text = FindText(element);
            ReadOnlyCollection<LineResult> lr = null;
            if (text != null)
            {
                TextBlock textElement = ((TextBlock)text);
                if (textElement.IsLayoutDataValid)
                    lr = textElement.GetLineResults();
            }
            else
            {
                text = FindFlowDocumentScrollViewer(element);
                if (text != null)
                {
                    TextDocumentView tdv = ((IServiceProvider)text).GetService(typeof(ITextView)) as TextDocumentView;
                    if (tdv != null && tdv.IsValid)
                    {
                        ReadOnlyCollection<ColumnResult> cr = tdv.Columns;
                        if (cr != null && cr.Count > 0)
                        {
                            ColumnResult columnResult = cr[0];
                            ReadOnlyCollection<ParagraphResult> pr = columnResult.Paragraphs;
                            if (pr != null && pr.Count > 0)
                            {
                                ContainerParagraphResult cpr = pr[0] as ContainerParagraphResult;
                                if (cpr != null)
                                {
                                    TextParagraphResult textParagraphResult = cpr.Paragraphs[0] as TextParagraphResult;
                                    if (textParagraphResult != null)
                                    {
                                        lr = textParagraphResult.Lines;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (lr != null && lr.Count > 0)
            {
                Point ancestorOffset = new Point();
                text.TransformToAncestor(element).TryTransform(ancestorOffset, out ancestorOffset);
                return lr[0].LayoutBox.Height + ancestorOffset.Y * 2d;
            }

            return element.RenderSize.Height;
        }

        private TextBlock FindText(Visual root)
        {
            // Cases where the root is itself a TextBlock
            TextBlock text = root as TextBlock;
            if (text != null)
                return text;

            ContentPresenter cp = root as ContentPresenter;
            if (cp != null)
            {
                if (VisualTreeHelper.GetChildrenCount(cp) == 1)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(cp, 0);

                    // Cases where the child is a TextBlock
                    TextBlock textBlock = child as TextBlock;
                    if (textBlock == null)
                    {
                        AccessText accessText = child as AccessText;
                        if (accessText != null &&
                            VisualTreeHelper.GetChildrenCount(accessText) == 1)
                        {
                            // Cases where the child is an AccessText whose child is a TextBlock
                            textBlock = VisualTreeHelper.GetChild(accessText, 0) as TextBlock;
                        }
                    }
                    return textBlock;
                }
            }
            else
            {
                AccessText accessText = root as AccessText;
                if (accessText != null &&
                    VisualTreeHelper.GetChildrenCount(accessText) == 1)
                {
                    // Cases where the root is an AccessText whose child is a TextBlock
                    return VisualTreeHelper.GetChild(accessText, 0) as TextBlock;
                }
            }
            return null;
        }

        private FlowDocumentScrollViewer FindFlowDocumentScrollViewer(Visual root)
        {
            FlowDocumentScrollViewer text = root as FlowDocumentScrollViewer;
            if (text != null)
                return text;

            ContentPresenter cp = root as ContentPresenter;
            if (cp != null)
            {
                if(VisualTreeHelper.GetChildrenCount(cp) == 1)
                    return VisualTreeHelper.GetChild(cp, 0) as FlowDocumentScrollViewer;
            }
            return null;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Memebers
        //
        //-------------------------------------------------------------------

        #region Private Members
        UIElement _bullet = null;
        #endregion Private Members

    }
}

