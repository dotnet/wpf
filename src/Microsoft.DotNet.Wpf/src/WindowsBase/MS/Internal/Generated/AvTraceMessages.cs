﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This file was generated by $(WpfCodeGenDir)AvTrace\AvTraceMessages.tt.
// Any manual updates to this file will overwritten.

namespace MS.Internal
{
    static internal partial class TraceDependencyProperty
    {
        static private AvTrace _avTrace = new AvTrace(
                delegate() { return PresentationTraceSources.DependencyPropertySource; },
                delegate() { PresentationTraceSources._DependencyPropertySource = null; }
                );

		static AvTraceDetails _ApplyTemplateContent;
		static public AvTraceDetails ApplyTemplateContent
        {
            get
            {
                if ( _ApplyTemplateContent == null )
                {
                    _ApplyTemplateContent = new AvTraceDetails(1, new string[] { "Apply template" } );
                }

                return _ApplyTemplateContent;
            }
        }

		static AvTraceDetails _Register;
		static public AvTraceDetails Register
        {
            get
            {
                if ( _Register == null )
                {
                    _Register = new AvTraceDetails(2, new string[] { "Registered DependencyProperty" } );
                }

                return _Register;
            }
        }

		static AvTraceDetails _UpdateEffectiveValueStart;
		static public AvTraceDetails UpdateEffectiveValueStart
        {
            get
            {
                if ( _UpdateEffectiveValueStart == null )
                {
                    _UpdateEffectiveValueStart = new AvTraceDetails(3, new string[] { "Update effective DP value (Start)" } );
                }

                return _UpdateEffectiveValueStart;
            }
        }

		static AvTraceDetails _UpdateEffectiveValueStop;
		static public AvTraceDetails UpdateEffectiveValueStop
        {
            get
            {
                if ( _UpdateEffectiveValueStop == null )
                {
                    _UpdateEffectiveValueStop = new AvTraceDetails(4, new string[] { "Update effective DP value (Stop)" } );
                }

                return _UpdateEffectiveValueStop;
            }
        }

        /// <summary> Send a single trace output </summary>
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, params object[] parameters )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        /// <summary> These help delay allocation of object array </summary>
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, Array.Empty<object>() );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        /// <summary> Send a singleton "activity" trace (really, this sends the same trace as both a Start and a Stop) </summary>
        static public void TraceActivityItem( AvTraceDetails traceDetails, params Object[] parameters )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        /// <summary> These help delay allocation of object array </summary>
        static public void TraceActivityItem( AvTraceDetails traceDetails )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, Array.Empty<object>() );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        static public bool IsEnabled
        {
            get { return _avTrace != null && _avTrace.IsEnabled; }
        }

        /// <summary> Is there a Tracesource?  (See comment on AvTrace.IsEnabledOverride.) </summary>
        static public bool IsEnabledOverride
        {
            get { return _avTrace.IsEnabledOverride; }
        }

        /// <summary> Re-read the configuration for this trace source </summary>
        static public void Refresh()
        {
            _avTrace.Refresh();
        }
	}
    static internal partial class TraceFreezable
    {
        static private AvTrace _avTrace = new AvTrace(
                delegate() { return PresentationTraceSources.FreezableSource; },
                delegate() { PresentationTraceSources._FreezableSource = null; }
                );

		static AvTraceDetails _UnableToFreezeExpression;
		static public AvTraceDetails UnableToFreezeExpression
        {
            get
            {
                if ( _UnableToFreezeExpression == null )
                {
                    _UnableToFreezeExpression = new AvTraceDetails(1, new string[] { "CanFreeze is returning false because a DependencyProperty on the Freezable has a value that is an expression" } );
                }

                return _UnableToFreezeExpression;
            }
        }

		static AvTraceDetails _UnableToFreezeDispatcherObjectWithThreadAffinity;
		static public AvTraceDetails UnableToFreezeDispatcherObjectWithThreadAffinity
        {
            get
            {
                if ( _UnableToFreezeDispatcherObjectWithThreadAffinity == null )
                {
                    _UnableToFreezeDispatcherObjectWithThreadAffinity = new AvTraceDetails(2, new string[] { "CanFreeze is returning false because a DependencyProperty on the Freezable has a value that is a DispatcherObject with thread affinity" } );
                }

                return _UnableToFreezeDispatcherObjectWithThreadAffinity;
            }
        }

		static AvTraceDetails _UnableToFreezeFreezableSubProperty;
		static public AvTraceDetails UnableToFreezeFreezableSubProperty
        {
            get
            {
                if ( _UnableToFreezeFreezableSubProperty == null )
                {
                    _UnableToFreezeFreezableSubProperty = new AvTraceDetails(3, new string[] { "CanFreeze is returning false because a DependencyProperty on the Freezable has a value that is a Freezable that has also returned false from CanFreeze" } );
                }

                return _UnableToFreezeFreezableSubProperty;
            }
        }

		static AvTraceDetails _UnableToFreezeAnimatedProperties;
		static public AvTraceDetails UnableToFreezeAnimatedProperties
        {
            get
            {
                if ( _UnableToFreezeAnimatedProperties == null )
                {
                    _UnableToFreezeAnimatedProperties = new AvTraceDetails(4, new string[] { "CanFreeze is returning false because at least one DependencyProperty on the Freezable is animated." } );
                }

                return _UnableToFreezeAnimatedProperties;
            }
        }

        /// <summary> Send a single trace output </summary>
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, params object[] parameters )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        /// <summary> These help delay allocation of object array </summary>
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, Array.Empty<object>() );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        /// <summary> Send a singleton "activity" trace (really, this sends the same trace as both a Start and a Stop) </summary>
        static public void TraceActivityItem( AvTraceDetails traceDetails, params Object[] parameters )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        /// <summary> These help delay allocation of object array </summary>
        static public void TraceActivityItem( AvTraceDetails traceDetails )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, Array.Empty<object>() );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        static public bool IsEnabled
        {
            get { return _avTrace != null && _avTrace.IsEnabled; }
        }

        /// <summary> Is there a Tracesource?  (See comment on AvTrace.IsEnabledOverride.) </summary>
        static public bool IsEnabledOverride
        {
            get { return _avTrace.IsEnabledOverride; }
        }

        /// <summary> Re-read the configuration for this trace source </summary>
        static public void Refresh()
        {
            _avTrace.Refresh();
        }
	}
    static internal partial class TraceNameScope
    {
        static private AvTrace _avTrace = new AvTrace(
                delegate() { return PresentationTraceSources.NameScopeSource; },
                delegate() { PresentationTraceSources._NameScopeSource = null; }
                );

		static AvTraceDetails _RegisterName;
		static public AvTraceDetails RegisterName
        {
            get
            {
                if ( _RegisterName == null )
                {
                    _RegisterName = new AvTraceDetails(1, new string[] { "Name has been registered on INameScope" } );
                }

                return _RegisterName;
            }
        }

		static AvTraceDetails _UnregisterName;
		static public AvTraceDetails UnregisterName
        {
            get
            {
                if ( _UnregisterName == null )
                {
                    _UnregisterName = new AvTraceDetails(2, new string[] { "Name has been un-registered on INameScope" } );
                }

                return _UnregisterName;
            }
        }

        /// <summary> Send a single trace output </summary>
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, params object[] parameters )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        /// <summary> These help delay allocation of object array </summary>
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, Array.Empty<object>() );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        /// <summary> Send a singleton "activity" trace (really, this sends the same trace as both a Start and a Stop) </summary>
        static public void TraceActivityItem( AvTraceDetails traceDetails, params Object[] parameters )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        /// <summary> These help delay allocation of object array </summary>
        static public void TraceActivityItem( AvTraceDetails traceDetails )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, Array.Empty<object>() );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        static public bool IsEnabled
        {
            get { return _avTrace != null && _avTrace.IsEnabled; }
        }

        /// <summary> Is there a Tracesource?  (See comment on AvTrace.IsEnabledOverride.) </summary>
        static public bool IsEnabledOverride
        {
            get { return _avTrace.IsEnabledOverride; }
        }

        /// <summary> Re-read the configuration for this trace source </summary>
        static public void Refresh()
        {
            _avTrace.Refresh();
        }
	}
}
