// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Spinner Proxy

using System;
using System.Collections;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // WinForms NumericUpDown proxy
    class WinformsSpinner : ProxyHwnd, IRawElementProviderHwndOverride, IRangeValueProvider, IValueProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors 

        // Contructor for WinformsSpinner class. Calls the base class constructor.
        internal WinformsSpinner(IntPtr hwnd, IntPtr hwndEdit, IntPtr hwndUpDown, ProxyFragment parent, int item)
            : base(hwnd, parent, item)
        {
            _elEdit = new WindowsEditBox(hwndEdit, this, (int)0);
            _elUpDown = new WindowsUpDown(hwndUpDown, this, (int)0);

            string text;
            try
            {
                text = Misc.ProxyGetText(hwndEdit);
            }
            catch (TimeoutException)
            {
                text = null;
            }
            catch (Win32Exception)
            {
                text = null;
            }

            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    double.Parse(text, NumberStyles.Any, CultureInfo.InvariantCulture);

                    // the text parsed just fine so must be a number.
                    _type = SpinnerType.Numeric;
                }
                catch (FormatException)
                {
                    // the text is not a based 10 number.  Check if the text is a hex number.
                    try
                    {
                        int.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                        // the text parsed just fine so must be a number.
                        _type = SpinnerType.Numeric;
                    }
                    catch (FormatException)
                    {
                        // the text does not consist solely of an optional negative sign followed by a sequence of
                        // digits ranging from 0 to 9, so this spinner must be a domain spinner
                        _type = SpinnerType.Domain;
                    }
                    catch (OverflowException)
                    {
                        // the text represents a number less than MinValue or greater than MaxValue, but it is still
                        // a number, therefore must be a numeric spinner
                        _type = SpinnerType.Numeric;
                    }
                }
                catch (OverflowException)
                {
                    // the text represents a number less than MinValue or greater than MaxValue, but it is still
                    // a number, therefore must be a numeric spinner
                    _type = SpinnerType.Numeric;
                }
            }
            else
            {
                // numeric spinners always have a value.  The defualt state of a domain spinner
                // may be blank.  The text value is blank so this must be a domain spinner.
                _type = SpinnerType.Domain;
            }

            // Set the strings to return properly the properties.
            _cControlType = ControlType.Spinner;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents(WindowsUpDown.RaiseEvents);
        }

        #endregion

        #region Proxy Create 

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // If idChild is not zero this can't be a Winforms spinner.  The way this
            // code is called, the passed in hwnd could be some other control and in
            // that case the calling code should continue looking for a valid proxy.
            if (idChild != 0)
            {
                return null;
            }

            try
            {
                //
                // A winform spinner control can only have 2 children - An Edit and an UpDown.
                // 

                // First child
                IntPtr hwndFirstChild = Misc.GetWindow(hwnd, NativeMethods.GW_CHILD);
                if (hwndFirstChild == IntPtr.Zero)
                {
                    return null;
                }

                // Last child
                IntPtr hwndLastChild = Misc.GetWindow(hwndFirstChild, NativeMethods.GW_HWNDLAST);
                if (hwndLastChild == IntPtr.Zero)
                {
                    return null;
                }

                // No children in the middle
                if (Misc.GetWindow(hwndFirstChild, NativeMethods.GW_HWNDNEXT) != hwndLastChild)
                {
                    return null;
                }
                

                // We need to positively identify the two children as Edit and UpDown controls
                IntPtr hwndEdit;
                IntPtr hwndSpin;

                // Find the Edit control.  Typically the UpDown is first so we'll start with the other window.                                
                if (Misc.ProxyGetClassName(hwndLastChild).IndexOf("Edit", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    hwndEdit = hwndLastChild;
                    hwndSpin = hwndFirstChild;
                }
                else
                {
                    // Haven't seen this but suppose it's possible.  Subsequent test will confirm.
                    if (Misc.ProxyGetClassName(hwndFirstChild).IndexOf("Edit", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        hwndEdit = hwndFirstChild;
                        hwndSpin = hwndLastChild;
                    }
                    else
                    {
                        // Must contain an Edit/UpDown control pair
                        return null;
                    }
                }
                                
                //
                // A winform UpDown control can only have 2 children, both spin buttons.
                // 

                // Use IAccessible implementation to confirm the spinner & children
                Accessible acc = null;
                if (Accessible.AccessibleObjectFromWindow(hwndSpin, NativeMethods.OBJID_CLIENT, ref acc) != NativeMethods.S_OK || acc == null)
                {
                    return null;
                }

                if ((acc.Role != AccessibleRole.SpinButton) || (acc.ChildCount != 2))
                {
                    return null;
                }
                
                // Confirmed spinner
                return new WinformsSpinner(hwnd, hwndEdit, hwndSpin, null, 0);
            }
            catch (ElementNotAvailableException)
            {
                // Ignore ElementNotAvailableExceptions and return null
                return null;
            }
        }

        #endregion Proxy Create 

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        // ------------------------------------------------------
        //
        // ProxySimple interface implementation
        //
        // ------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            if (iid == RangeValuePattern.Pattern && _type == SpinnerType.Numeric)
            {
                return this;
            }
            else if (_type == SpinnerType.Domain)
            {
                if (iid == ValuePattern.Pattern)
                {
                    return this;
                }
                else if (iid == SelectionPattern.Pattern)
                {
                    // Special case for a WinForms domain spinner. It is supposed to support
                    // SelectionPattern, but because it is based on an edit control, this is
                    // not possible unless each of the items in the spinner have been reviewed
                    // at least once. Call out this unusual case by throwing the following
                    // NotImplementedException.
                    throw new NotImplementedException();
                }
            }
            return null;
        }

        #endregion

        // ------------------------------------------------------
        //
        // ProxyFragment interface implementation
        //
        // ------------------------------------------------------

        #region ProxyFragment Interface

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint(int x, int y)
        {
            Rect rcUpDown = _elUpDown.BoundingRectangle;

            if (rcUpDown.Contains(new Point(x, y)))
            {
                return _elUpDown.ElementProviderFromPoint (x, y);
            }

            return this;
        }

        #endregion

        // ------------------------------------------------------
        //
        // ProxyHwnd interface implementation
        //
        // ------------------------------------------------------

        #region ProxyHwnd Interface

        internal override void AdviseEventAdded(AutomationEvent eventId, AutomationProperty[] aidProps)
        {
            base.AdviseEventAdded(eventId, aidProps);

            // Need to also advise the edit portions of the spinner so that it can raise events.
            if (_elEdit != null)
            {
                _elEdit.AdviseEventAdded(eventId, aidProps);
            }
        }

        internal override void AdviseEventRemoved(AutomationEvent eventId, AutomationProperty[] aidProps)
        {
            base.AdviseEventRemoved(eventId, aidProps);

            // Need to also remove the advise from the edit portions of the spinner.
            if (_elEdit != null)
            {
                _elEdit.AdviseEventRemoved(eventId, aidProps);
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // IRawElementProviderHwndOverride interface implementation
        //
        // ------------------------------------------------------

        #region IRawElementProviderHwndOverride

        IRawElementProviderSimple IRawElementProviderHwndOverride.GetOverrideProviderForHwnd(IntPtr hwnd)
        {
            // The Edit hwnd in the spinner is not a Logical Element
            // returns the full spinner on the request to get a provider for the edit
            if (IsEdit(hwnd))
            {
                return new WinformsSpinnerEdit(_hwnd, _elEdit._hwnd, _elUpDown._hwnd, _parent, _item);
            }

            return null;
        }

        #endregion IRawElementProviderHwndOverride

        // ------------------------------------------------------
        //
        // IRangeValueProvider interface implementation
        //
        // ------------------------------------------------------

        #region RangeValue Pattern

        // Sets a new position for the edit part of the spinner.
        void IRangeValueProvider.SetValue (double obj)
        {
            ((IRangeValueProvider)_elUpDown).SetValue(obj);
        }

        // Request to get the value that this UI element is representing in a native format
        double IRangeValueProvider.Value
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).Value;
            }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return (bool)((IRangeValueProvider)_elUpDown).IsReadOnly &&
                       (bool)((IValueProvider)_elEdit).IsReadOnly;
            }
        }

        double IRangeValueProvider.Maximum
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).Maximum;
            }
        }

        double IRangeValueProvider.Minimum
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).Minimum;
            }
        }

        double IRangeValueProvider.SmallChange
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).SmallChange;
            }
        }

        double IRangeValueProvider.LargeChange
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).LargeChange;
            }
        }
        #endregion RangeValuePattern

        #region Value Pattern

        // ------------------------------------------------------
        //
        // IValueProvider interface implementation
        //
        // ------------------------------------------------------

        // Sets a value into the edit box
        void IValueProvider.SetValue(string val)
        {
            ((IValueProvider)_elEdit).SetValue(val);
        }

        // Returns the value of the edit.
        string IValueProvider.Value
        {
            get
            {
                return ((IValueProvider)_elEdit).Value;
            }
        }

        // Returns if the edit control is read-only.
        bool IValueProvider.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private methods                                             
        //
        // ------------------------------------------------------

        #region Private Methods

        // Check if the hwnd is the Edit Window in the spinner box
        private bool IsEdit(IntPtr hwnd)
        {
            return _elEdit._hwnd == hwnd;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields                                             
        //
        // ------------------------------------------------------

        #region Private Fields

        private WindowsEditBox _elEdit;
        private WindowsUpDown _elUpDown;

        private SpinnerType _type;

        #endregion

        // ------------------------------------------------------
        //
        // Private Types
        //
        // ------------------------------------------------------

        #region Private Types

        // Button control types based on groupings of style constants
        private enum SpinnerType
        {
            Numeric,
            Domain
        };

        #endregion

        //------------------------------------------------------
        //
        //  WinformsSpinnerEdit Private Class
        //
        //------------------------------------------------------

        #region WinformsSpinnerEdit

        // Proxy for ComboBox Edit portion
        // The Edit proxy does not exist, it the the whole spinner. This class is needed
        // though to override ProviderOptions
        class WinformsSpinnerEdit : WinformsSpinner
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructor

            internal WinformsSpinnerEdit(IntPtr hwnd, IntPtr hwndEdit, IntPtr hwndUpDown, ProxyFragment parent, int item)
            : base(hwnd, hwndEdit, hwndUpDown, parent, item)
            {
            }

            #endregion Constructor

            //------------------------------------------------------
            //
            //  Provider Implementation
            //
            //------------------------------------------------------

            internal override ProviderOptions ProviderOptions
            {
                get
                {
                    return base.ProviderOptions | ProviderOptions.OverrideProvider;
                }
            }
        }

        #endregion
    }
}
