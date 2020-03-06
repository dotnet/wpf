// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Diagnostics;
using System.Globalization;
using System.Windows;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    internal interface ISyncKeyTipAndContent
    {
        bool KeepKeyTipAndContentInSync { get; set; }
        bool IsKeyTipSyncSource { get; set; }
        bool SyncingKeyTipAndContent { get; set; }
    }

    internal static class KeyTipAndContentSyncHelper
    {
        public static void Sync(ISyncKeyTipAndContent syncElement, DependencyProperty contentProperty)
        {
            if (syncElement.SyncingKeyTipAndContent)
            {
                return;
            }

            DependencyObject element = syncElement as DependencyObject;
            Debug.Assert(element != null);
            try
            {
                syncElement.SyncingKeyTipAndContent = true;
                if (!syncElement.KeepKeyTipAndContentInSync)
                {
                    string keyTip = KeyTipService.GetKeyTip(element);
                    bool isKeyTipSet = !string.IsNullOrEmpty(keyTip);
                    string stringContent = element.GetValue(contentProperty) as string;
                    if (stringContent != null)
                    {
                        int accessKeyIndex = RibbonHelper.FindAccessKeyMarker(stringContent);
                        if (isKeyTipSet)
                        {
                            if (accessKeyIndex == -1)
                            {
                                int accessorIndex = stringContent.IndexOf(keyTip[0]);
                                if (accessorIndex != -1)
                                {
                                    syncElement.KeepKeyTipAndContentInSync = true;
                                    syncElement.IsKeyTipSyncSource = true;
                                }
                            }
                        }
                        else if (accessKeyIndex != -1)
                        {
                            syncElement.KeepKeyTipAndContentInSync = true;
                            syncElement.IsKeyTipSyncSource = false;
                        }
                    }
                }
                element.CoerceValue(contentProperty);
                element.CoerceValue(KeyTipService.KeyTipProperty);
            }
            finally
            {
                syncElement.SyncingKeyTipAndContent = false;
            }
        }

        public static void OnKeyTipChanged(ISyncKeyTipAndContent syncElement, DependencyProperty contentProperty)
        {
            DependencyObject element = syncElement as DependencyObject;
            Debug.Assert(element != null);
            if (!syncElement.SyncingKeyTipAndContent)
            {
                if (syncElement.KeepKeyTipAndContentInSync)
                {
                    syncElement.KeepKeyTipAndContentInSync = false;
                    if (syncElement.IsKeyTipSyncSource)
                    {
                        element.CoerceValue(contentProperty);
                    }
                }
                Sync(syncElement, contentProperty);
            }
        }

        public static object CoerceKeyTip(ISyncKeyTipAndContent syncElement,
            object baseValue,
            DependencyProperty contentProperty)
        {
            DependencyObject element = syncElement as DependencyObject;
            Debug.Assert(element != null);
            if (syncElement.KeepKeyTipAndContentInSync &&
                !syncElement.IsKeyTipSyncSource)
            {
                syncElement.KeepKeyTipAndContentInSync = false;
                if (string.IsNullOrEmpty((string)baseValue))
                {
                    string stringContent = element.GetValue(contentProperty) as string;
                    if (stringContent != null)
                    {
                        int accessIndex = RibbonHelper.FindAccessKeyMarker(stringContent);
                        if (accessIndex >= 0 && accessIndex < stringContent.Length - 1)
                        {
                            syncElement.KeepKeyTipAndContentInSync = true;
                            return StringInfo.GetNextTextElement(stringContent, accessIndex + 1);
                        }
                    }
                }
            }
            return baseValue;
        }

        public static void OnContentPropertyChanged(ISyncKeyTipAndContent syncElement, DependencyProperty contentProperty)
        {
            DependencyObject element = syncElement as DependencyObject;
            Debug.Assert(element != null);
            if (!syncElement.SyncingKeyTipAndContent)
            {
                if (syncElement.KeepKeyTipAndContentInSync)
                {
                    syncElement.KeepKeyTipAndContentInSync = false;
                    if (!syncElement.IsKeyTipSyncSource)
                    {
                        element.CoerceValue(KeyTipService.KeyTipProperty);
                    }
                }
                Sync(syncElement, contentProperty);
            }
        }

        public static object CoerceContentProperty(ISyncKeyTipAndContent syncElement,
            object baseValue)
        {
            DependencyObject element = syncElement as DependencyObject;
            Debug.Assert(element != null);
            if (syncElement.KeepKeyTipAndContentInSync &&
                syncElement.IsKeyTipSyncSource)
            {
                syncElement.KeepKeyTipAndContentInSync = false;
                string stringContent = baseValue as string;
                if (stringContent != null)
                {
                    if (RibbonHelper.FindAccessKeyMarker(stringContent) < 0)
                    {
                        string keyTip = KeyTipService.GetKeyTip(element);
                        if (!string.IsNullOrEmpty(keyTip) &&
                            keyTip.Length > 0)
                        {
                            int accessorIndex = stringContent.IndexOf(keyTip[0]);
                            if (accessorIndex >= 0)
                            {
                                syncElement.KeepKeyTipAndContentInSync = true;
                                return stringContent.Substring(0, accessorIndex) + '_' + stringContent.Substring(accessorIndex);
                            }
                        }
                    }
                }
            }
            return baseValue;
        }
    }
}