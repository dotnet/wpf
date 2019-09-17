// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/***************************************************************************\
*
*
* This class wraps a System.Diagnostics.TraceSource.  The purpose of
* wrapping is so that we can have a common point of enabling/disabling
* without perf effect.  This is also where we standardize the output
* we produce, to enable more effective trace filters, trace listeners,
* and post-processing tools.
*
*
\***************************************************************************/

#define TRACE

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Windows;

using Microsoft.Win32;
using MS.Win32;
using MS.Internal.WindowsBase;

namespace MS.Internal
{
    internal class AvTrace
    {
        //
        //  AvTrace constructor
        //
        //  Parameters:
        //      getTraceSourceDelegate and clearTraceSourceDelegate are for accessing the
        //      TraceSource (that corresponds to this AvTrace) from PresentationTraceSources.
        //

        public AvTrace( GetTraceSourceDelegate getTraceSourceDelegate, ClearTraceSourceDelegate clearTraceSourceDelegate )
        {
            _getTraceSourceDelegate = getTraceSourceDelegate;
            _clearTraceSourceDelegate = clearTraceSourceDelegate;

            // Get notified when we need to check the registry and debugger attached-ness.
            PresentationTraceSources.TraceRefresh += new TraceRefreshEventHandler(Refresh);

            // Fetch and cache the TraceSource from PresentationTraceSources
            Initialize();
        }


        //
        //  Refresh this trace source -- see if it needs to be enabled
        //  because registry setting has changed or debugger is attached.
        //
        //  To re-read the config file, call System.Diagnostics.Trace.Refresh() .
        //

        public void Refresh()
        {
            // Cause the registry to be re-read in case it's changed.
            _enabledInRegistry = null;

            // Re-initialize everything
            Initialize();
        }

        //
        //  Don't call Trace unless this property is true.
        //  Note that this can be false, even though IsEnabledOverride is true (this happens when
        //  running under a debugger).  See IsEnabledOverride for a description.
        //

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }

        //
        // If this flag is set, Trace doesn't automatically add the .GetHashCode and .GetType
        // to the format string.
        //

        public bool SuppressGeneratedParameters
        {
            get { return _suppressGeneratedParameters; }
            set { _suppressGeneratedParameters = value; }
        }

        //
        //  IsEnabledOverride is used as a special override.  This can be true even if IsEnabled is false.
        //  The scenario is for >=Warning traces; in this case, despite the fact that tracing wasn't enabled
        //  in the registry, we want to send traces anyway if a debugger is attached.  Clients of the trace classes
        //  have to decide to call IsEnabledOverride in these cases rather than IsEnabled.
        //  Note that this could be cleaner, but was done to minimize risk as a late checkin; the key goal being
        //  that tracing only run when enabled.
        //

        public bool IsEnabledOverride
        {
            get { return _traceSource != null; }
        }

        //
        // EnabledByDebugger is a special override that causes AvTrace to be IsEnabled, when we're running
        // under a debugger, despite the fact that the registry hasn't enabled tracing.  This was added
        // so that TraceData could cause tracing to be enabled when running under the debugger.  Other classes
        // only enable tracing based on the registry key.  See also the common on IsEnabledOverride.  And like the note there,
        // this couuld be more straightforward, but is designed this way to mitigate risk.
        //

        public bool EnabledByDebugger
        {
            get { return _enabledByDebugger; }

            set
            {
                _enabledByDebugger = value;
                if( _enabledByDebugger )
                {
                    if( !IsEnabled && IsDebuggerAttached() )
                    {
                        _isEnabled = true;
                    }
                }
                else
                {
                    if( IsEnabled && !IsWpfTracingEnabledInRegistry() && !_hasBeenRefreshed )
                    {
                        _isEnabled = false;
                    }
                }
            }
        }

        //
        // This method is called to indicate that PresentationTraceSources.Refresh
        // has been called.  When that method has been called, we'll allow
        // tracing to be enabled.
        //

        static public void OnRefresh()
        {
            _hasBeenRefreshed = true;
        }


        //
        // Extra args passed to Trace call will be forwarded to listeners of TraceExtraMessages
        //

        public event AvTraceEventHandler TraceExtraMessages;


        //
        // Internal initialization
        //
        void Initialize( )
        {
            // Decide if we should actually create a TraceSource instance (doing so isn't free,
            // so we don't want to do it if we can avoid it).

            if( ShouldCreateTraceSources() )
            {
                // Get TraceSource from the PresentationTraceSources
                // (this call will indirectly create the TraceSource if one doesn't already exist)
                _traceSource = _getTraceSourceDelegate();

                // We go enabled if tracing is enabled in the registry, if
                // PresentationTraceSources.Refresh has been called, or if we're in the debugger
                // and the debugger is supposed to enable tracing.
                _isEnabled = IsWpfTracingEnabledInRegistry() || _hasBeenRefreshed || _enabledByDebugger ;
            }
            else
            {
                _clearTraceSourceDelegate();
                _traceSource = null;
                _isEnabled = false;
            }
        }


        //
        //  See if tracing should be enabled.  It should if we're in
        //  a debugger, or if the registry key is set, or if
        //  PresentationTraceSources.Refresh has been called.
        //  (We do this so that in the default case, we don't even create
        //  the TraceSource.)
        //

        static private bool ShouldCreateTraceSources()
        {
            if( IsWpfTracingEnabledInRegistry()
                || IsDebuggerAttached()
                || _hasBeenRefreshed
              )
            {
                return true;
            }

            return false;
}



       ///
       ///  Read the registry to see if WPF tracing is allowed
       ///

       [FriendAccessAllowed]
       static internal bool IsWpfTracingEnabledInRegistry()
       {
            // First time this is called, initialize from the registry

            if( _enabledInRegistry == null )
            {
                bool enabled = false;

                object keyValue = SecurityHelper.ReadRegistryValue(
                                                            Registry.CurrentUser,
                                                            @"Software\Microsoft\Tracing\WPF",
                                                            "ManagedTracing");

                if( keyValue is int && ((int) keyValue) == 1 )
                {
                    enabled = true;
                }

                // Update the static.  Doing this last protects us from threading problems; worse case, multiple
                // threads will set the same value into it.
                _enabledInRegistry = enabled;
}

            return (bool) _enabledInRegistry;
}



        //
        // Check for an attached debugger.
        //

        internal static bool IsDebuggerAttached()
        {
            return Debugger.IsAttached || SafeNativeMethods.IsDebuggerPresent();
        }


        //
        //  Trace an event
        //
        //  note: labels start at index 1, parameters start at index 0
        //

        public void Trace( TraceEventType type, int eventId, string message, string[] labels, object[] parameters )
        {
            // Don't bother building the string if this trace is going to be ignored.

            if( _traceSource == null
                || !_traceSource.Switch.ShouldTrace( type ))
            {
                return;
            }


            // Compose the trace string.

            AvTraceBuilder traceBuilder = new AvTraceBuilder(AntiFormat(message)); // Holds the format string
            ArrayList arrayList = new ArrayList(); // Holds the combined labels & parameters arrays.

            int formatIndex = 0;

            if (parameters != null && labels != null && labels.Length > 0)
            {
                int i = 1, j = 0;
                for( ; i < labels.Length && j < parameters.Length; i++, j++ )
                {
                    // Append to the format string a "; {0} = '{1}'", where the index increments (e.g. the second iteration will
                    // produce {2} & {3}).

                    traceBuilder.Append("; {" + (formatIndex++).ToString() + "}='{" + (formatIndex++).ToString() + "}'" );

                    // If this parameter is null, convert to "<null>"; otherwise, when a string.format is ultimately called
                    // it produces bad results.

                    if( parameters[j] == null )
                    {
                        parameters[j] = "<null>";
                    }

                    // Otherwise, if this is an interesting object, add the hash code and type to
                    // the format string explicitely.

                    else if( !SuppressGeneratedParameters
                             && parameters[j].GetType() != typeof(string)
                             && !(parameters[j] is ValueType)
                             && !(parameters[j] is Type)
                             && !(parameters[j] is DependencyProperty) )
                    {
                        traceBuilder.Append("; " + labels[i].ToString() + ".HashCode='"
                                                    + GetHashCodeHelper(parameters[j]).ToString() + "'" );

                        traceBuilder.Append("; " + labels[i].ToString() + ".Type='"
                                                    + GetTypeHelper(parameters[j]).ToString() + "'" );
                    }


                    // Add the label & parameter to the combined list.
                    // (As an optimization, the generated classes could pre-allocate a thread-safe static array, to avoid
                    // this allocation and the ToArray allocation below.)

                    arrayList.Add( labels[i] );
                    arrayList.Add( parameters[j] );
}

                // It's OK if we terminate because we have more lables than parameters;
                // this is used by traces to have out-values in the Stop message.

                if( TraceExtraMessages != null && j < parameters.Length)
                {
                    TraceExtraMessages( traceBuilder, parameters, j );
                }
}

            // Send the trace

            _traceSource.TraceEvent(
                type,
                eventId,
                traceBuilder.ToString(),
                arrayList.ToArray() );

            // When in the debugger, always flush the output, to guarantee that the
            // traces and other info (e.g. exceptions) get interleaved correctly.

            if( IsDebuggerAttached() )
            {
                _traceSource.Flush();
            }
}


        //
        //  Trace an event, as both a TraceEventType.Start and TraceEventType.Stop.
        //  (information is contained in the Start event)
        //

        public void TraceStartStop( int eventID, string message, string[] labels, Object[] parameters )
        {
            Trace( TraceEventType.Start, eventID, message, labels, parameters );
            _traceSource.TraceEvent( TraceEventType.Stop, eventID);
        }


        //
        //  Convert the value to a string, even if the system conversion throws
        //  an exception.
        //

        static public string ToStringHelper(object value)
        {
            if (value == null)
                return "<null>";

            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
            #pragma warning disable 1634, 1691

            // PreSharp complains about catching NullReference (and other) exceptions.
            // In this case, these are precisely the ones we want to catch the most,
            // so that we can still print some kind of diagnostic information even
            // about objects that implement ToString poorly.
            #pragma warning disable 56500

            string result;
            try
            {
                result = value.ToString();
            }
            catch
            {
                result = "<unprintable>";
            }

            #pragma warning restore 56500
            #pragma warning restore 1634, 1691

            return AntiFormat(result);
        }


        // replace { and } by {{ and }} - call if literal string will be passed to Format
        static public string AntiFormat(string s)
        {
            int formatIndex = s.IndexOfAny(FormatChars);
            if (formatIndex < 0)
                return s;

            StringBuilder sb = new StringBuilder();
            int index = 0;
            int lengthMinus1 = s.Length - 1;

            while (formatIndex >= 0)
            {
                if (formatIndex < lengthMinus1 && s[formatIndex] == s[formatIndex+1])
                {
                    // formatting character is already duplicated - leave them alone
                    formatIndex = s.IndexOfAny(FormatChars, formatIndex+2);
                }
                else
                {
                    // duplicate the formatting character
                    sb.Append(s.Substring(index, formatIndex - index + 1));
                    sb.Append(s[formatIndex]);

                    index = formatIndex + 1;
                    formatIndex = s.IndexOfAny(FormatChars, index);
                }
            }

            if (index <= lengthMinus1)
            {
                sb.Append(s.Substring(index));
            }

            return sb.ToString();
        }


        //
        //  Return the type name for the given value
        //

        static public string TypeName(object value)
        {
            if (value == null)
                return "<null>";

            return value.GetType().Name;
        }

        //
        // This is a wrapper around Object.GetHashCode.  We use this because
        // individual GetHashCode implementations can be unreliable.
        //

        static public int GetHashCodeHelper(object value )
        {
            try
            {
                return (value != null) ? value.GetHashCode() : 0;
            }
            catch( Exception e )
            {
                if( CriticalExceptions.IsCriticalApplicationException(e))
                {
                    throw;
                }

                return 0;
            }
}


        //
        // Get an object's type, returning typeof(ValueType) for
        // the null case.
        //

        static public Type GetTypeHelper(object value)
        {
            if (value == null)
            {
                return typeof(ValueType);
            }

            return value.GetType();
        }


        //
        // Private state
        //

        // Flag showing if tracing is enabled.  See also the IsEnabledOverride property
        bool _isEnabled = false;

        // If this is set, then having the debugger attached is an excuse to be enabled,
        // even if the registry flag isn't set.
        bool _enabledByDebugger = false;

        // If this flag is set, Trace doesn't automatically add the .GetHashCode and .GetType
        // to the format string.
        bool _suppressGeneratedParameters = false;

        // If this flag is set, tracing will be enabled, as if it was set in the registry.
        static bool _hasBeenRefreshed = false;

        // Delegates to create and remove the TraceSource instance
        GetTraceSourceDelegate _getTraceSourceDelegate;
        ClearTraceSourceDelegate _clearTraceSourceDelegate;

        // Cache of TraceSource instance; real value resides in PresentationTraceSources.
        TraceSource _traceSource;

        // Cache used by IsWpfTracingEnabledInRegistry
        static Nullable<bool> _enabledInRegistry = null;

        static char[] FormatChars = new char[]{ '{', '}' };
}

    internal delegate void AvTraceEventHandler( AvTraceBuilder traceBuilder, object[] parameters, int start );

    internal class AvTraceBuilder
    {
        StringBuilder  _sb;

        public AvTraceBuilder()
        {
            _sb = new StringBuilder();
        }

        public AvTraceBuilder( string message )
        {
            _sb = new StringBuilder( message );
        }

        public void Append( string message )
        {
            _sb.Append( message );
        }

        public void AppendFormat( string message, params object[] args )
        {
            object[] argstrs = new object[args.Length];
            for (int i = 0; i < args.Length; ++i)
            {
                string s = args[i] as string;
                argstrs[i] = (s != null) ? s : AvTrace.ToStringHelper(args[i]);
            }
            _sb.AppendFormat( CultureInfo.InvariantCulture, message, argstrs );
        }

        public void AppendFormat( string message, object arg1 )
        {
            _sb.AppendFormat( CultureInfo.InvariantCulture, message, new object[] { AvTrace.ToStringHelper(arg1) } );
        }

        public void AppendFormat( string message, object arg1, object arg2 )
        {
            _sb.AppendFormat( CultureInfo.InvariantCulture, message, new object[] { AvTrace.ToStringHelper(arg1), AvTrace.ToStringHelper(arg2) } );
        }

        public void AppendFormat( string message, string arg1 )
        {
            _sb.AppendFormat( CultureInfo.InvariantCulture, message, new object[] { AvTrace.AntiFormat(arg1) } );
        }

        public void AppendFormat( string message, string arg1, string arg2 )
        {
            _sb.AppendFormat( CultureInfo.InvariantCulture, message, new object[] { AvTrace.AntiFormat(arg1), AvTrace.AntiFormat(arg2) } );
        }

        public override string ToString( )
        {
            return _sb.ToString();
        }
}

    internal delegate TraceSource GetTraceSourceDelegate();
    internal delegate void ClearTraceSourceDelegate();
}


