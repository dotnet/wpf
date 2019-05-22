// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Debug helpers for TextFlow. 
//

using System;
using System.Diagnostics;

namespace MS.Internal.PtsHost
{
#if TEXTPANELLAYOUTDEBUG
    // ----------------------------------------------------------------------
    // Debug helpers for TextFlow.
    // ----------------------------------------------------------------------
    internal sealed class TextPanelDebug
    {
        // ------------------------------------------------------------------
        // Enter the scope and log the message.
        // ------------------------------------------------------------------
        internal static void BeginScope(string msg, Category category)
        {
            if (_instance == null) { _instance = new TextPanelDebug(); }
            if (_instance._IsEnabled(category))
            {
                _instance._BeginScope(msg);
            }
        }

        // ------------------------------------------------------------------
        // Exit the current scope.
        // ------------------------------------------------------------------
        internal static void EndScope(Category category)
        {
            Debug.Assert(_instance != null);
            if (_instance._IsEnabled(category))
            {
                _instance._EndScope();
            }
        }

        // ------------------------------------------------------------------
        // Enter the scope and start timer.
        // ------------------------------------------------------------------
        internal static void StartTimer(string name, Category category)
        {
            if (_instance == null) { _instance = new TextPanelDebug(); }
            if (_instance._IsEnabled(category))
            {
                _instance._StartTimer(name);
            }
        }

        // ------------------------------------------------------------------
        // Exit the current scope and stop timer.
        // ------------------------------------------------------------------
        internal static void StopTimer(string name, Category category)
        {
            Debug.Assert(_instance != null);
            if (_instance._IsEnabled(category))
            {
                _instance._StopTimer(name);
            }
        }

        // ------------------------------------------------------------------
        // Increment counter for specific event.
        // ------------------------------------------------------------------
        internal static void IncrementCounter(string name, Category category)
        {
            if (_instance == null) { _instance = new TextPanelDebug(); }
            if (_instance._IsEnabled(category))
            {
                _instance._IncrementCounter(name);
            }
        }


        // ------------------------------------------------------------------
        // Log message.
        // ------------------------------------------------------------------
        internal static void Log(string msg, Category category)
        {
            if (_instance == null) { _instance = new TextPanelDebug(); }
            if (_instance._IsEnabled(category))
            {
                _instance._Log(msg);
            }
        }

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // ------------------------------------------------------------------
        // Private ctor.
        // ------------------------------------------------------------------
        private TextPanelDebug()
        {
            //_categories = Category.MeasureArrange | Category.ContentChange | Category.Context;
            //_categories = Category.MeasureArrange | Category.ContentChange;
            //_categories = Category.TextView;
            _categories = Category.MeasureArrange;
        }

        // ------------------------------------------------------------------
        // Print summary.
        // ------------------------------------------------------------------
        ~TextPanelDebug()
        {
            Console.WriteLine("> TextPanelDebug Summary ------------------------------------------");
            if (_counters != null)
            {
                Console.WriteLine("> Counters:");
                foreach (string name in _counters.Keys)
                {
                    Console.WriteLine(">     " + name + ": " + (int)_counters[name]);
                }
            }
            if (_timers != null)
            {
                Console.WriteLine("> Timers:");
                foreach (string name in _timers.Keys)
                {
                    Console.WriteLine(">     " + name + ": " + ((Stopwatch)_timers[name]).ElapsedTicks);
                }
            }
            Console.WriteLine("> -----------------------------------------------------------------");
        }

        // ------------------------------------------------------------------
        // Enter the scope and log the message.
        // ------------------------------------------------------------------
        private void _BeginScope(string msg)
        {
            _Log(msg);
            ++_indent;
        }

        // ------------------------------------------------------------------
        // Exit the current scope.
        // ------------------------------------------------------------------
        private void _EndScope()
        {
            --_indent;
            _Log(".end");
        }

        // ------------------------------------------------------------------
        // Enter the scope and start timer.
        // ------------------------------------------------------------------
        private void _StartTimer(string name)
        {
            _IncrementCounter(name);
            if (_timers == null)
            {
                _timers = new System.Collections.Hashtable();
            }
            Stopwatch stopwatch = _timers.ContainsKey(name) ? (Stopwatch)_timers[name] : null;
            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                _timers[name] = stopwatch;
            }
            stopwatch.Start();
        }

        // ------------------------------------------------------------------
        // Exit the current scope and stop timer.
        // ------------------------------------------------------------------
        private void _StopTimer(string name)
        {
            Debug.Assert(_timers != null);
            Stopwatch stopwatch = _timers.ContainsKey(name) ? (Stopwatch)_timers[name] : null;
            Debug.Assert(stopwatch != null);
            stopwatch.Stop();
        }

        // ------------------------------------------------------------------
        // Increment counter for specific event.
        // ------------------------------------------------------------------
        private void _IncrementCounter(string eventTag)
        {
            if (_counters == null)
            {
                _counters = new System.Collections.Hashtable();
            }
            int currentValue = _counters.ContainsKey(eventTag) ? (int)_counters[eventTag] : 0;
            _counters[eventTag] = currentValue + 1;
        }

        // ------------------------------------------------------------------
        // Log message.
        // ------------------------------------------------------------------
        private void _Log(string msg)
        {
            Console.WriteLine("> " + CurrentIndent + msg);
        }

        // ------------------------------------------------------------------
        // String representing current indent.
        // ------------------------------------------------------------------
        private bool _IsEnabled(Category category)
        {
            return ((category & _categories) == category);
        }

        // ------------------------------------------------------------------
        // String representing current indent.
        // ------------------------------------------------------------------
        private string CurrentIndent { get { return new string(' ', _indent * 2); } }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // ------------------------------------------------------------------
        // Current indent.
        // ------------------------------------------------------------------
        private int _indent;

        // ------------------------------------------------------------------
        // Current indent.
        // ------------------------------------------------------------------
        private Category _categories;

        // ------------------------------------------------------------------
        // Name to counter map.
        // ------------------------------------------------------------------
        private System.Collections.Hashtable _counters;

        // ------------------------------------------------------------------
        // Name to time map.
        // ------------------------------------------------------------------
        private System.Collections.Hashtable _timers;

        // ------------------------------------------------------------------
        // Instance of the TextPanelDebug.
        // ------------------------------------------------------------------
        private static TextPanelDebug _instance;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  Internal Types
        //
        //-------------------------------------------------------------------

        #region Internal Types

        // ------------------------------------------------------------------
        // Debug category flags.
        // ------------------------------------------------------------------
        [Flags]
        internal enum Category
        {
            None = 0x0000,
            MeasureArrange = 0x0001,
            ContentChange = 0x0002,
            Context = 0x0004,
            TextView = 0x0008,
        }

        #endregion Internal Types
    }
#endif
}
