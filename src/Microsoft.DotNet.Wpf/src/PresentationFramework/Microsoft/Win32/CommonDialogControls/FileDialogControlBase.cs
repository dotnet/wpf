// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.CommonDialogControls
{
    using System;
    using System.Threading;
    using MS.Internal.Interop;

    // Not inheritable due to internal abstract members (GetState, SetState).
    /// <summary>
    /// The base class for common item dialog controls associated with an ID.
    /// </summary>
    public abstract class FileDialogControlBase : ICloneable
    {
        /// <summary>
        /// Initializes the control with a unique ID.
        /// </summary>
        public FileDialogControlBase()
        {
            ID = NextID();
        }

        /// <summary>
        /// Gets the control's unique ID.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets or sets an object associated with the control.
        /// This provides the ability to attach an arbitrary object to the control.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Gets or sets whether the control is visible.
        /// </summary>
        public bool IsVisible
        {
            get { return GetState(CDCS.VISIBLE); }
            set { SetState(CDCS.VISIBLE, value); }
        }
        /// <summary>
        /// Gets or sets whether the control is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get { return GetState(CDCS.ENABLED); }
            set { SetState(CDCS.ENABLED, value); }
        }

        /// <summary>
        /// Hides and disables the control.
        /// </summary>
        public void HideAndDisable()
        {
            SetState(CDCS.INACTIVE);
        }

        /// <summary>
        /// Shows and enables the control.
        /// </summary>
        public virtual void ShowAndEnable()
        {
            SetState(CDCS.ENABLEDVISIBLE);
        }

        /// <summary>
        /// Creates a new instance of the class not associated with any dialog.
        /// </summary>
        public abstract object Clone();

        private protected abstract CDCS GetState();
        private protected abstract void SetState(CDCS state);

        /// <summary>
        /// Transforms a string using WPF accelerator syntax to Win32 accelerator syntax.
        /// </summary>
        /// <param name="s">The string potentially containing an accelerator using WPF syntax.</param>
        /// <returns>a string potentially containing an accelerator using Win32 syntax.</returns>
        internal static string ConvertAccelerators(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            // Win32 syntax uses the ampersand, WPF syntax uses the undescore.
            // In both cases, only the first instance of the character counts.
            // In both cases, it can be doubled for escaping, at 

            // Ampersands in WPF syntax must be escaped.
            s = s.Replace("&", "&&");

            int index = s.IndexOf('_');
            while (index >= 0)
            {
                if (index >= s.Length - 1 || s[index + 1] != '_')
                {
                    // this is the last character or a non-underscore follows, i.e. this is an accelerator
                    break;
                }

                // this is an escaped underscore, try next one
                index = s.IndexOf('_', index + 2);
            }
                   
            if (index >= 0)
            {
                // replace the underscore with an ampersand
                s = string.Concat(s.Substring(0, index), "&", s.Substring(index + 1));
            }

            // WPF escaped underscores must be unescaped.
            s = s.Replace("__", "_");

            return s;
        }

        private bool GetState(CDCS flag)
        {
            return (GetState() & flag) != 0;
        }
        private void SetState(CDCS flag, bool value)
        {
            CDCS state = GetState();

            if (value)
            {
                state |= flag;
            }
            else
            {
                state &= ~flag;
            }

            SetState(state);
        }

        // While the native API lets developers to choose their IDs, in our model we just generate a sequential ID
        // per instance of any control. Users can compare controls by reference for equality.
        internal static int NextID()
        {
            return Interlocked.Increment(ref s_GlobalIDCounter); // handles overflow
        }

        private static int s_GlobalIDCounter;
    }
}

