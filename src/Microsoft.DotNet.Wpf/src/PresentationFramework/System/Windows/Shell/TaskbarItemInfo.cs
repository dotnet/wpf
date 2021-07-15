// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
namespace System.Windows.Shell
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using MS.Internal;

    /// <summary>
    /// The values of TaskbarItemInfo's ProgressState dependency property.
    /// </summary>
    public enum TaskbarItemProgressState
    {
        None,
        Indeterminate,
        Normal,
        Error,
        Paused,
    }

    /// <summary>
    /// This class manages Window's interaction with the Taskbar features added in Windows 7.
    /// </summary>
    public sealed class TaskbarItemInfo : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new TaskbarItemInfo();
        }

        #region Dependency Properties and support methods.

        /// <summary>
        /// ProgressState Dependency Property
        /// </summary>
        public static readonly DependencyProperty ProgressStateProperty = DependencyProperty.Register(
            "ProgressState",
            typeof(TaskbarItemProgressState),
            typeof(TaskbarItemInfo),
            new PropertyMetadata(
                TaskbarItemProgressState.None,
                (d, e) => ((TaskbarItemInfo)d).NotifyDependencyPropertyChanged(e),
                (d, baseValue) => ((TaskbarItemInfo)d).CoerceProgressState((TaskbarItemProgressState)baseValue)));

        /// <summary>
        /// Gets or sets the ProgressState property.  This dependency property 
        /// indicates the type of progress bar to display for the Window on the taskbar.
        /// </summary>
        public TaskbarItemProgressState ProgressState
        {
            get { return (TaskbarItemProgressState)GetValue(ProgressStateProperty); }
            set { SetValue(ProgressStateProperty, value); }
        }

        private TaskbarItemProgressState CoerceProgressState(TaskbarItemProgressState value)
        {
            switch (value)
            {
                case TaskbarItemProgressState.Error:
                case TaskbarItemProgressState.Indeterminate:
                case TaskbarItemProgressState.None:
                case TaskbarItemProgressState.Normal:
                case TaskbarItemProgressState.Paused:
                    break;
                default:
                    // Convert bad data into no-progress bar.
                    value = TaskbarItemProgressState.None;
                    break;
            }
            
            return value;
        }

        /// <summary>
        /// ProgressValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register(
            "ProgressValue",
            typeof(double),
            typeof(TaskbarItemInfo),
            new PropertyMetadata(
                0d,
                (d, e) => ((TaskbarItemInfo)d).NotifyDependencyPropertyChanged(e),
                (d, baseValue) => CoerceProgressValue((double)baseValue)));

        /// <summary>
        /// Gets or sets the ProgressValue property.  This dependency property 
        /// indicates the value of the progress bar for the Window in the taskbar.
        /// </summary>
        public double ProgressValue
        {
            get { return (double)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }

        private static double CoerceProgressValue(double progressValue)
        {
            if (double.IsNaN(progressValue))
            {
                progressValue = 0;
            }
            else
            {
                progressValue = Math.Max(progressValue, 0);
                progressValue = Math.Min(1, progressValue);
            }

            return progressValue;
        }

        /// <summary>
        /// Overlay Dependency Property
        /// </summary>
        public static readonly DependencyProperty OverlayProperty = DependencyProperty.Register(
            "Overlay",
            typeof(ImageSource),
            typeof(TaskbarItemInfo),
            new PropertyMetadata(
                null,
                (d, e) => ((TaskbarItemInfo)d).NotifyDependencyPropertyChanged(e)));

        /// <summary>
        /// Gets or sets the Overlay property.  This dependency property 
        /// indicates the overlay that is used to indicate status for the Window in the taskbar.
        /// </summary>
        public ImageSource Overlay
        {
            get { return (ImageSource)GetValue(OverlayProperty); }
            set { SetValue(OverlayProperty, value); }
        }

        /// <summary>
        /// Description Dependency Property
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description",
            typeof(string),
            typeof(TaskbarItemInfo),
            new PropertyMetadata(
                string.Empty,
                (d, e) => ((TaskbarItemInfo)d).NotifyDependencyPropertyChanged(e)));

        /// <summary>
        /// Gets or sets the Description property.  This dependency property 
        /// indicates the tooltip to display on the thumbnail for this window.
        /// </summary>
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        /// <summary>
        /// ThumbnailClipMargin Dependency Property
        /// </summary>
        public static readonly DependencyProperty ThumbnailClipMarginProperty = DependencyProperty.Register(
            "ThumbnailClipMargin",
            typeof(Thickness),
            typeof(TaskbarItemInfo),
            new PropertyMetadata(
                default(Thickness),
                (d, e) => ((TaskbarItemInfo)d).NotifyDependencyPropertyChanged(e),
                (d, baseValue) => ((TaskbarItemInfo)d).CoerceThumbnailClipMargin((Thickness)baseValue)));

        /// <summary>
        /// Gets or sets the ThumbnailClipMargin property.  This dependency property 
        /// indicates the border of the Window to clip when displayed in the taskbar thumbnail preview.
        /// </summary>
        public Thickness ThumbnailClipMargin
        {
            get { return (Thickness)GetValue(ThumbnailClipMarginProperty); }
            set { SetValue(ThumbnailClipMarginProperty, value); }
        }

        private Thickness CoerceThumbnailClipMargin(Thickness margin)
        {
            // Any negative/NaN/Infinity margins we'll treat as nil.
            if (!margin.IsValid(false, false, false, false))
            {
                return new Thickness();
            }

            return margin;
        }

        /// <summary>
        /// ThumbButtonInfos Dependency Property
        /// </summary>
        public static readonly DependencyProperty ThumbButtonInfosProperty = DependencyProperty.Register(
            "ThumbButtonInfos",
            typeof(ThumbButtonInfoCollection),
            typeof(TaskbarItemInfo),
            new PropertyMetadata(
                new FreezableDefaultValueFactory(ThumbButtonInfoCollection.Empty),
                (d, e) => ((TaskbarItemInfo)d).NotifyDependencyPropertyChanged(e)));

        /// <summary>
        /// Gets the ThumbButtonInfos property.  This dependency property 
        /// indicates the collection of command buttons to be displayed in the taskbar item thumbnail for the window.
        /// </summary>
        public ThumbButtonInfoCollection ThumbButtonInfos
        {
            get { return (ThumbButtonInfoCollection)GetValue(ThumbButtonInfosProperty); }
            set { SetValue(ThumbButtonInfosProperty, value); }
        }

        #endregion

        private void NotifyDependencyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DependencyPropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Used by Window to receive notifications about sub-property changes.
        internal event DependencyPropertyChangedEventHandler PropertyChanged;
    }
}
