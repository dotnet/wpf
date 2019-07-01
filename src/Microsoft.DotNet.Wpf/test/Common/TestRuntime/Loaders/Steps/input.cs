// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Security.Permissions;

namespace Microsoft.Test.Utilities
{
    internal class Clipboard
    {
        /// <summary>
        /// Set
        /// </summary>
        /// <param name="s"></param>
        internal static void Set(string s)
        {
            System.Windows.Clipboard.SetDataObject(s);
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [UIPermission(SecurityAction.Assert, Unrestricted = true)]
        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static string Get()
        {
            DataObject d = (DataObject)System.Windows.Clipboard.GetDataObject();
            string s = (string)d.GetData(DataFormats.StringFormat);
            return (s);
        }

        /// <summary>
        /// Formats
        /// </summary>
        internal static string[] Formats
        {
            [UIPermission(SecurityAction.Assert, Unrestricted = true)]
            [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
            get
            {
                return (System.Windows.Clipboard.GetDataObject().GetFormats());
            }
        }
    }

    /// <summary>
    /// LocalizationHelper exposes util routines related to localization
    /// </summary>
    internal class LocalizationHelper
    {
        internal struct InputLocale
        {
            internal const string English_UnitedStates = "00000409";
            internal const string Arabic_SaudiArabia = "00000401";
            internal const string Hebrew_Hebrew = "0000040d";
            internal const string Japanese_JapaneseInputSystem = "e0010411";
            internal const string Spanish_Argentina = "00002c0a";
        }

        /// <summary>
        /// Sets the input locale identifier (formerly called the keyboard layout handle) for the calling thread.
        /// </summary>
        /// <param name="inputLocale">
        /// A string composed of the hexadecimal value of the Language Identifier (low word) and a device identifier (high word)
        /// (the composition is known as input locale identifier).
        /// </param>
        /// <example>The following code shows how to use this method.<code>
        /// Set input locale to: Arabic (Saudi Arabia) - Arabic (101): KeyboardInput.SetActiveInputLocale("00000401");
        /// Set input locale to: English (United States) - US: KeyboardInput.SetActiveInputLocale("00000409");
        /// Set input locale to: Hebrew - Hebrew: KeyboardInput.SetActiveInputLocale("0000040d");
        /// Set input locale to: Japanese - Japanese Input System (MS-IME2002): KeyboardInput.SetActiveInputLocale("e0010411");
        /// Set input locale to: Spanish (Argentina) - Latin American: KeyboardInput.SetActiveInputLocale("00002c0a");
        /// </code></example>
        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static void SetActiveInputLocale(string inputLocale)
        {
            IntPtr hkl;     // New layout handle.
            IntPtr hklOld;  // Old layout handle.

            if (inputLocale == null)
            {
                throw new ArgumentNullException("inputLocale");
            }
            if (inputLocale.Length != 8)
            {
                throw new ArgumentException("Input locale values should be 8 hex digits.", "inputLocale");
            }

            // Attempt to load the keyboard layout, but do not use the return value. For some reason, this is always returning the English keyboard
            hkl = Win32Helper.LoadKeyboardLayout(inputLocale, 0);
            if (hkl == IntPtr.Zero)
            {
                throw new Exception("Unable to set input locale " + inputLocale);
            }

            hkl = (IntPtr)(uint.Parse(inputLocale, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            hklOld = Win32Helper.ActivateKeyboardLayout(hkl, 0);
            if (hklOld == IntPtr.Zero)
            {
                throw new Exception("Locale " + inputLocale + " loaded, " + "but activation failed.");
            }
            if (hkl == hklOld)
            {
                System.Diagnostics.Debug.WriteLine("WARNING: changing keyboard layout " + "to " + inputLocale + " keeps the same layout " + "(input locale " + ((uint)hkl).ToString("x8") + ")");
            }
        }
    }
}