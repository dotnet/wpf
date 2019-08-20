// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Automation
{
    ///
    public static class AutomationProperties
    {
        #region AutomationId
        /// <summary>
        /// AutomationId Property
        /// </summary>
        public static readonly DependencyProperty AutomationIdProperty =
                    DependencyProperty.RegisterAttached(
                                "AutomationId",
                                typeof(string),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(string.Empty),
                                new ValidateValueCallback(IsNotNull));

        /// <summary>
        /// Helper for setting AutomationId property on a DependencyObject. 
        /// </summary>
        public static void SetAutomationId(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(AutomationIdProperty, value);
        }

        /// <summary>
        /// Helper for reading AutomationId property from a DependencyObject.
        /// </summary>
        public static string GetAutomationId(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((string)element.GetValue(AutomationIdProperty));
        }
        #endregion AutomationId

        #region Name
        /// <summary>
        /// Name Property
        /// </summary>
        public static readonly DependencyProperty NameProperty =
                    DependencyProperty.RegisterAttached(
                                "Name",
                                typeof(string),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(string.Empty),
                                new ValidateValueCallback(IsNotNull));

        /// <summary>
        /// Helper for setting Name property on a DependencyObject. 
        /// </summary>
        public static void SetName(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(NameProperty, value);
        }

        /// <summary>
        /// Helper for reading Name property from a DependencyObject.
        /// </summary>
        public static string GetName(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((string)element.GetValue(NameProperty));
        }
        #endregion Name

        #region HelpText
        /// <summary>
        /// HelpText Property
        /// </summary>
        public static readonly DependencyProperty HelpTextProperty =
                    DependencyProperty.RegisterAttached(
                                "HelpText",
                                typeof(string),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(string.Empty),
                                new ValidateValueCallback(IsNotNull));

        /// <summary>
        /// Helper for setting HelpText property on a DependencyObject. 
        /// </summary>
        public static void SetHelpText(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(HelpTextProperty, value);
        }

        /// <summary>
        /// Helper for reading HelpText property from a DependencyObject.
        /// </summary>
        public static string GetHelpText(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((string)element.GetValue(HelpTextProperty));
        }
        #endregion HelpText

        #region AcceleratorKey
        /// <summary>
        /// AcceleratorKey Property
        /// </summary>
        public static readonly DependencyProperty AcceleratorKeyProperty =
                    DependencyProperty.RegisterAttached(
                                "AcceleratorKey",
                                typeof(string),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(string.Empty),
                                new ValidateValueCallback(IsNotNull));

        /// <summary>
        /// Helper for setting AcceleratorKey property on a DependencyObject. 
        /// </summary>
        public static void SetAcceleratorKey(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(AcceleratorKeyProperty, value);
        }

        /// <summary>
        /// Helper for reading AcceleratorKey property from a DependencyObject.
        /// </summary>
        public static string GetAcceleratorKey(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((string)element.GetValue(AcceleratorKeyProperty));
        }
        #endregion AcceleratorKey

        #region AccessKey
        /// <summary>
        /// AccessKey Property
        /// </summary>
        public static readonly DependencyProperty AccessKeyProperty =
                    DependencyProperty.RegisterAttached(
                                "AccessKey",
                                typeof(string),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(string.Empty),
                                new ValidateValueCallback(IsNotNull));

        /// <summary>
        /// Helper for setting AccessKey property on a DependencyObject. 
        /// </summary>
        public static void SetAccessKey(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(AccessKeyProperty, value);
        }

        /// <summary>
        /// Helper for reading AccessKey property from a DependencyObject.
        /// </summary>
        public static string GetAccessKey(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((string)element.GetValue(AccessKeyProperty));
        }
        #endregion AccessKey

        #region ItemStatus
        /// <summary>
        /// ItemStatus Property
        /// </summary>
        public static readonly DependencyProperty ItemStatusProperty =
                    DependencyProperty.RegisterAttached(
                                "ItemStatus",
                                typeof(string),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(string.Empty),
                                new ValidateValueCallback(IsNotNull));

        /// <summary>
        /// Helper for setting ItemStatus property on a DependencyObject. 
        /// </summary>
        public static void SetItemStatus(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ItemStatusProperty, value);
        }

        /// <summary>
        /// Helper for reading ItemStatus property from a DependencyObject.
        /// </summary>
        public static string GetItemStatus(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((string)element.GetValue(ItemStatusProperty));
        }
        #endregion ItemStatus

        #region ItemType
        /// <summary>
        /// ItemType Property
        /// </summary>
        public static readonly DependencyProperty ItemTypeProperty =
                    DependencyProperty.RegisterAttached(
                                "ItemType",
                                typeof(string),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(string.Empty),
                                new ValidateValueCallback(IsNotNull));

        /// <summary>
        /// Helper for setting ItemType property on a DependencyObject. 
        /// </summary>
        public static void SetItemType(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ItemTypeProperty, value);
        }

        /// <summary>
        /// Helper for reading ItemType property from a DependencyObject.
        /// </summary>
        public static string GetItemType(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((string)element.GetValue(ItemTypeProperty));
        }
        #endregion ItemType

        #region IsColumnHeader
        /// <summary>
        /// IsColumnHeader Property
        /// </summary>
        public static readonly DependencyProperty IsColumnHeaderProperty =
                    DependencyProperty.RegisterAttached(
                                "IsColumnHeader",
                                typeof(bool),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

        /// <summary>
        /// Helper for setting IsColumnHeader property on a DependencyObject. 
        /// </summary>
        public static void SetIsColumnHeader(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsColumnHeaderProperty, value);
        }

        /// <summary>
        /// Helper for reading IsColumnHeader property from a DependencyObject.
        /// </summary>
        public static bool GetIsColumnHeader(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((bool)element.GetValue(IsColumnHeaderProperty));
        }
        #endregion IsColumnHeader

        #region IsRowHeader
        /// <summary>
        /// IsRowHeader Property
        /// </summary>
        public static readonly DependencyProperty IsRowHeaderProperty =
                    DependencyProperty.RegisterAttached(
                                "IsRowHeader",
                                typeof(bool),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

        /// <summary>
        /// Helper for setting IsRowHeader property on a DependencyObject. 
        /// </summary>
        public static void SetIsRowHeader(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsRowHeaderProperty, value);
        }

        /// <summary>
        /// Helper for reading IsRowHeader property from a DependencyObject.
        /// </summary>
        public static bool GetIsRowHeader(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((bool)element.GetValue(IsRowHeaderProperty));
        }
        #endregion IsRowHeader

        #region IsRequiredForForm
        /// <summary>
        /// IsRequiredForForm Property
        /// </summary>
        public static readonly DependencyProperty IsRequiredForFormProperty =
                    DependencyProperty.RegisterAttached(
                                "IsRequiredForForm",
                                typeof(bool),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

        /// <summary>
        /// Helper for setting IsRequiredForForm property on a DependencyObject. 
        /// </summary>
        public static void SetIsRequiredForForm(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsRequiredForFormProperty, value);
        }

        /// <summary>
        /// Helper for reading IsRequiredForForm property from a DependencyObject.
        /// </summary>
        public static bool GetIsRequiredForForm(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((bool)element.GetValue(IsRequiredForFormProperty));
        }
        #endregion IsRequiredForForm

        #region LabeledBy
        /// <summary>
        /// LabeledBy Property
        /// </summary>
        public static readonly DependencyProperty LabeledByProperty =
                    DependencyProperty.RegisterAttached(
                                "LabeledBy",
                                typeof(UIElement),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata((UIElement)null));

        /// <summary>
        /// Helper for setting LabeledBy property on a DependencyObject. 
        /// </summary>
        public static void SetLabeledBy(DependencyObject element, UIElement value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(LabeledByProperty, value);
        }

        /// <summary>
        /// Helper for reading LabeledBy property from a DependencyObject.
        /// </summary>
        public static UIElement GetLabeledBy(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((UIElement)element.GetValue(LabeledByProperty));
        }
        #endregion LabeledBy

        #region IsOffscreenBehavior
        /// <summary>
        /// IsOffscreenBehavior Property
        /// </summary>
        public static readonly DependencyProperty IsOffscreenBehaviorProperty =
                    DependencyProperty.RegisterAttached(
                                "IsOffscreenBehavior",
                                typeof(IsOffscreenBehavior),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(IsOffscreenBehavior.Default));

        /// <summary>
        /// Helper for setting IsOffscreenBehavior property on a DependencyObject. 
        /// </summary>
        public static void SetIsOffscreenBehavior(DependencyObject element, IsOffscreenBehavior value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsOffscreenBehaviorProperty, value);
        }

        /// <summary>
        /// Helper for reading IsOffscreenBehavior property from a DependencyObject.
        /// </summary>
        public static IsOffscreenBehavior GetIsOffscreenBehavior(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((IsOffscreenBehavior)element.GetValue(IsOffscreenBehaviorProperty));
        }
        #endregion IsOffscreenBehavior
        
        #region LiveSetting
        /// <summary>
        /// LiveSetting Property
        /// </summary>
        public static readonly DependencyProperty LiveSettingProperty =
                    DependencyProperty.RegisterAttached(
                                "LiveSetting",
                                typeof(AutomationLiveSetting),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(AutomationLiveSetting.Off));

        /// <summary>
        /// Helper for setting LiveSetting property on a DependencyObject. 
        /// </summary>
        public static void SetLiveSetting(DependencyObject element, AutomationLiveSetting value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(LiveSettingProperty, value);
        }

        /// <summary>
        /// Helper for reading LiveSetting property from a DependencyObject.
        /// </summary>
        public static AutomationLiveSetting GetLiveSetting(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((AutomationLiveSetting)element.GetValue(LiveSettingProperty));
        }
        #endregion LiveSetting

        #region PositionInSet
        /// <summary>
        /// PositionInSet property describes the ordinal location of the element within a set of elements which are considered to be siblings.
        /// </summary>
        /// <remarks>
        /// PositionInSet works in coordination with the SizeOfSet property to describe the ordinal location in the set.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ee684017(v=vs.85).aspx"/>
        /// </remarks>
        public static readonly DependencyProperty PositionInSetProperty =
                    DependencyProperty.RegisterAttached(
                                "PositionInSet",
                                typeof(int),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(AutomationProperties.AutomationPositionInSetDefault));

        /// <summary>
        /// Helper for setting PositionInSet property on a DependencyObject. 
        /// </summary>
        public static void SetPositionInSet(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(PositionInSetProperty, value);
        }

        /// <summary>
        /// Helper for reading PositionInSet property from a DependencyObject.
        /// </summary>
        public static int GetPositionInSet(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((int)element.GetValue(PositionInSetProperty));
        }
        #endregion

        #region SizeOfSet
        /// <summary>
        /// SizeOfSet property describes the count of automation elements in a group or set that are considered to be siblings.
        /// </summary>
        /// <remarks>
        /// SizeOfSet works in coordination with the PositionInSet property to describe the count of items in the set.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ee684017(v=vs.85).aspx"/>
        /// </remarks>
        public static readonly DependencyProperty SizeOfSetProperty =
                    DependencyProperty.RegisterAttached(
                                "SizeOfSet",
                                typeof(int),
                                typeof(AutomationProperties),
                                new UIPropertyMetadata(AutomationProperties.AutomationSizeOfSetDefault));

        /// <summary>
        /// Helper for setting SizeOfSet property on a DependencyObject. 
        /// </summary>
        public static void SetSizeOfSet(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(SizeOfSetProperty, value);
        }

        /// <summary>
        /// Helper for reading SizeOfSet property from a DependencyObject.
        /// </summary>
        public static int GetSizeOfSet(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((int)element.GetValue(SizeOfSetProperty));
        }
        #endregion

        #region private implementation
        // Validation callback for string properties
        private static bool IsNotNull(object value)
        {
            return (value != null);
        }
        #endregion

        #region internal constants
        internal const int AutomationPositionInSetDefault = -1;
        internal const int AutomationSizeOfSetDefault = -1;
        #endregion
    }
}

