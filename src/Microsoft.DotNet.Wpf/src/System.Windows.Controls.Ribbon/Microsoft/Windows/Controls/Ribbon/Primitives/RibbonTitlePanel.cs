// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    #region Using directives

    using MS.Internal;
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    
    #endregion

    /// <summary>
    ///   Implements the title panel of the Ribbon, this layouts out the Ribbon title
    ///   and RibbonContextualTabGroup separators around the QAT and system buttons.
    /// </summary>
    public class RibbonTitlePanel : Panel
    {
        #region Private Properties

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTitlePanel));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///   Measures the children of the RibbonTitlePanel.
        /// </summary>
        /// <param name="constraint">The space available for layout.</param>
        /// <returns>Returns the RibbonTitlePanel's desired size as determined by measurement of its children.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size();
            int count = Children.Count;
            if (count == 0)
            {
                return desiredSize;
            }

            double startContextualTabX = availableSize.Width;
            double endContextualTabX = 0.0;

            RibbonContextualTabGroupItemsControl groupHeaderItemsControl = Ribbon.ContextualTabGroupItemsControl;
            if (groupHeaderItemsControl != null && groupHeaderItemsControl.Visibility == Visibility.Visible)
            {
                // A dummy measure to ensure containers are generated.
                // We need to know FirstContextualTabHeader and LastContextualTabHeader
                if (groupHeaderItemsControl.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    groupHeaderItemsControl.Measure(availableSize);
                }

                // Calculate start and end positions
                RibbonContextualTabGroup firstContextualTab = groupHeaderItemsControl.FirstContextualTabHeader;
                if (firstContextualTab != null)
                {
                    startContextualTabX = Math.Min(CalculateContextualTabGroupStartX(firstContextualTab), availableSize.Width); 
                }

                RibbonContextualTabGroup lastContextualTab = groupHeaderItemsControl.LastContextualTabHeader;
                if (lastContextualTab != null)
                {
                    endContextualTabX = Math.Min(CalculateContextualTabGroupEndX(lastContextualTab), availableSize.Width);
                }

                groupHeaderItemsControl.Measure(new Size(Math.Max(endContextualTabX - startContextualTabX, 0), availableSize.Height));
                desiredSize.Width += groupHeaderItemsControl.DesiredSize.Width;
                desiredSize.Height = Math.Max(desiredSize.Height, groupHeaderItemsControl.DesiredSize.Height);
                
            }
            
            FrameworkElement qat = Ribbon.QatTopHost as FrameworkElement;
            FrameworkElement titleHost = Ribbon.TitleHost as FrameworkElement;

            if (qat != null && titleHost != null)
            {
                double availableToQat = 0.0, availableToTitle;
                double leftSpace, rightSpace;

                // If ribbon is not collapsed and QAT is on top
                if (qat.Visibility == Visibility.Visible)
                {
                    rightSpace = availableSize.Width - endContextualTabX;
                    // 1) if there is no room on the right for Title
                    // 2) or when there are no contextual tabs (i.e. endContextualTabX==0), then rightSpace=entire width
                    //  want to ensure MinWidth for title if possible
                    if ((DoubleUtil.LessThan(rightSpace, titleHost.MinWidth) ||             
                        DoubleUtil.GreaterThan(startContextualTabX, endContextualTabX))         
                        && DoubleUtil.GreaterThan(startContextualTabX, titleHost.MinWidth))
                    {
                        // Leave titleHost.MinWidth at the left of ContextualTabGroupItemsControl for TitleHost
                        availableToQat = startContextualTabX - titleHost.MinWidth;
                    }
                    else
                    {
                        // Give QAT space from 0 to start of ContextualTabGroupItemsControl
                        availableToQat = startContextualTabX;
                    }
                    // The size computed could be negative because of invalid
                    // transforms due to invalid arrange. Assume that the space
                    // available is 0 in such cases and assume that validation of
                    // transforms at a later point of time will fix the situation.
                    availableToQat = Math.Max(availableToQat, 0);

                    // Measure QAT
                    qat.Measure(new Size(availableToQat, availableSize.Height));
                    availableToQat = qat.DesiredSize.Width;

                    desiredSize.Width += availableToQat;
                    desiredSize.Height = Math.Max(desiredSize.Height, qat.DesiredSize.Height);
                }

                // Measure TitleHost. 
                // We try to give it titleHost.MinWidth, but it may get less than that when QAT has already reached qat.MinWidth
                endContextualTabX = Math.Max(endContextualTabX, availableToQat);
                rightSpace = availableSize.Width - endContextualTabX;
                leftSpace = startContextualTabX - availableToQat;

                // first measure to title's full size and see if it can fit entirely to the left
                titleHost.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (DoubleUtil.LessThanOrClose(titleHost.DesiredSize.Width, leftSpace))
                {
                    availableToTitle = leftSpace;
                }
                else
                {
                    // title takes best of right or left
                    availableToTitle = Math.Max(leftSpace, rightSpace);
                }
                // The size computed could be negative because of invalid
                // transforms due to invalid arrange. Assume that the space
                // available is 0 in such cases and assume that validation of
                // transforms at a later point of time will fix the situation.
                availableToTitle = Math.Max(availableToTitle, 0);

                titleHost.Measure(new Size(availableToTitle, availableSize.Height));
                desiredSize.Width += titleHost.DesiredSize.Width;
                desiredSize.Height = Math.Max(desiredSize.Height, titleHost.DesiredSize.Height);
            }

            desiredSize.Width = Math.Min(desiredSize.Width, availableSize.Width); // Prevent clipping
            return desiredSize;
        }

        /// <summary>
        ///   Arranges the children of the RibbonTitlePanel.
        /// </summary>
        /// <param name="arrangeSize">The arranged size of the TitlePanel.</param>
        /// <returns>The size at which the TitlePanel was arranged.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double x = 0.0;
            double width;
            double height;
            
            double startContextualTabX = 0;
            double endContextualTabX = 0;

            // Arrange ContextualTabHeaders
            RibbonContextualTabGroupItemsControl tabGroups = Ribbon.ContextualTabGroupItemsControl;
            if (tabGroups != null && tabGroups.Visibility == Visibility.Visible)
            {
                RibbonContextualTabGroup firstContextualTab = tabGroups.FirstContextualTabHeader;
                if (firstContextualTab != null)
                {
                    startContextualTabX = Math.Min(CalculateContextualTabGroupStartX(firstContextualTab), finalSize.Width);
                }

                endContextualTabX = Math.Min(startContextualTabX + tabGroups.DesiredSize.Width, finalSize.Width);
                tabGroups.Arrange(new Rect(startContextualTabX, finalSize.Height - tabGroups.DesiredSize.Height, tabGroups.DesiredSize.Width, tabGroups.DesiredSize.Height));
            }

            // Arrange QuickAccessToolbar
            double qatDesiredWidth = 0.0;
            UIElement qat = Ribbon.QatTopHost;
            if (qat != null)
            {
                qatDesiredWidth = qat.DesiredSize.Width;
                qat.Arrange(new Rect(0, 0.0, qatDesiredWidth, qat.DesiredSize.Height));
            }

            endContextualTabX = Math.Max(endContextualTabX, qatDesiredWidth);

            // Arrange the title
            UIElement titleHost = Ribbon.TitleHost;
            if (titleHost != null)
            {
                x = 0.0;
                width = titleHost.DesiredSize.Width;
                height = titleHost.DesiredSize.Height;
                double leftSpace = startContextualTabX - qatDesiredWidth;

                // If title can fit entirely to the left, then arrange on the left
                // i.e. between QAT and CTGHeaders
                if ( DoubleUtil.LessThanOrClose(width, leftSpace) )
                { 
                    x = qatDesiredWidth;
                }
                else
                {
                    // Arrange to the right of CTGHeaders.
                    x = endContextualTabX; ;
                }
            
                titleHost.Arrange(new Rect(x, 0.0, width, height));
            }
            
            return finalSize;
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        ///   Calculates the x-position at which the given ContextualTabGroup should be placed on the title panel.
        /// </summary>
        /// <param name="group">The RibbonContextualTabGroup to position.</param>
        /// <returns>The x-offset at which the ContextualTabGroup should be positioned.</returns>
        private double CalculateContextualTabGroupStartX(RibbonContextualTabGroup groupHeader)
        {
            double result = 0.0;

            // A visible CTG can have some of its tabs Collapsed. 
            // We should start from the first visible Tab in this CTG.
            RibbonTab firstTab = groupHeader.FirstVisibleTab;
            if (firstTab != null && firstTab.RibbonTabHeader != null && Ribbon != null)
            {
                RibbonTabHeader tabHeader = firstTab.RibbonTabHeader;
                GeneralTransform transformRibbonTabToRibbon = tabHeader.TransformToAncestor(Ribbon);
                GeneralTransform transformRibbonToRibbonTitlePanel = Ribbon.TransformToDescendant(this);
                Point point = new Point();
                point = transformRibbonTabToRibbon.Transform(point);
                point = transformRibbonToRibbonTitlePanel.Transform(point);
                result = point.X - tabHeader.Margin.Left; // Reduce RibbonTab Margin
            }

            return result;
        }

        private double CalculateContextualTabGroupEndX(RibbonContextualTabGroup groupHeader)
        {
            double endX = CalculateContextualTabGroupStartX(groupHeader);

            foreach (RibbonTab tab in groupHeader.Tabs)
            {
                if (tab.Visibility == Visibility.Visible && tab.RibbonTabHeader != null)
                {
                    endX += tab.RibbonTabHeader.DesiredSize.Width;
                }
            }
            return endX;
        }
        #endregion
    }
}
