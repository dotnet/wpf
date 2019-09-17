// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      A helper class for tracking the change of the system high contrast setting.
//
// Features:
//


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MS.Internal.Ink
{
    /// <summary>
    /// HighContrastCallback Classs - An abstract helper class
    /// </summary>
    internal abstract class HighContrastCallback
    {
        /// <summary>
        /// TurnHighContrastOn
        /// </summary>
        /// <param name="highContrastColor"></param>
        internal abstract void TurnHighContrastOn(Color highContrastColor);

        /// <summary>
        /// TurnHighContrastOff
        /// </summary>
        internal abstract void TurnHighContrastOff();

        /// <summary>
        /// Returns the dispatcher if the object is associated to a UIContext.
        /// </summary>
        internal abstract Dispatcher Dispatcher
        {
            get;
        }
    }

    /// <summary>
    /// StylusEditingBehavior - a base class for all stylus related editing behaviors
    /// </summary>
    internal static class HighContrastHelper
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        static HighContrastHelper()
        {
            __highContrastCallbackList = new List<WeakReference>();
            __increaseCount = 0;
        }
        
        #endregion Constructors


        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Register the weak references for HighContrastCallback
        /// </summary>
        /// <param name="highContrastCallback"></param>
        internal static void RegisterHighContrastCallback(HighContrastCallback highContrastCallback)
        {
            lock ( __lock )
            {
                int count = __highContrastCallbackList.Count;
                int i = 0;
                int j = 0;

                // Every 100 items, We go through the list to remove the references
                // which have been collected by GC.
                if ( __increaseCount > CleanTolerance )
                {
                    while ( i < count )
                    {
                        WeakReference weakRef = __highContrastCallbackList[j];
                        if ( weakRef.IsAlive )
                        {
                            j++;
                        }
                        else
                        {
                            // Remove the unavaliable reference from the list
                            __highContrastCallbackList.RemoveAt(j);
                        }
                        i++;
                    }

                    // Reset the count
                    __increaseCount = 0;
                }

                __highContrastCallbackList.Add(new WeakReference(highContrastCallback));
                __increaseCount++;
            }
        }

        /// <summary>
        /// The method is called from SystemResources.SystemThemeFilterMessage
        /// </summary>
        internal static void OnSettingChanged()
        {
            UpdateHighContrast();
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// UpdateHighContrast which calls out all the registered callbacks.
        /// </summary>
        private static void UpdateHighContrast()
        {
            lock ( __lock )
            {
                int count = __highContrastCallbackList.Count;
                int i = 0;
                int j = 0;

                // Now go through the list,
                // And we will notify the alive callbacks 
                // or remove the references which have been collected by GC.
                while ( i < count )
                {
                    WeakReference weakRef = __highContrastCallbackList[j];
                    if ( weakRef.IsAlive )
                    {
                        HighContrastCallback highContrastCallback = weakRef.Target as HighContrastCallback;

                        if ( highContrastCallback.Dispatcher != null )
                        {
                            highContrastCallback.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                new UpdateHighContrastCallback(OnUpdateHighContrast),
                                highContrastCallback);
                        }
                        else
                        {
                            OnUpdateHighContrast(highContrastCallback);
                        }

                        j++;
                    }
                    else
                    {
                        // Remove the dead ones
                        __highContrastCallbackList.RemoveAt(j);
                    }
                    i++;
                }

                // Reset the count
                __increaseCount = 0;
}
        }

        private delegate void UpdateHighContrastCallback(HighContrastCallback highContrastCallback);

        /// <summary>
        /// Invoke the callback
        /// </summary>
        /// <param name="highContrastCallback"></param>
        private static void OnUpdateHighContrast(HighContrastCallback highContrastCallback)
        {
            // Get the current setting.
            bool isHighContrast = SystemParameters.HighContrast;
            Color windowTextColor = SystemColors.WindowTextColor;

            if ( isHighContrast )
            {
                highContrastCallback.TurnHighContrastOn(windowTextColor);
            }
            else
            {
                highContrastCallback.TurnHighContrastOff();
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private static object                   __lock = new object();
        private static List<WeakReference>      __highContrastCallbackList;
        private static int                      __increaseCount;
        private const int                       CleanTolerance = 100;

        #endregion Private Fields
    }
}
