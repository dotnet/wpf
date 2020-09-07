// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define TRACE

//
// Description: Defines TraceData class, for providing debugging information
//              for Data Binding and Styling
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using MS.Internal.Data;
using MS.Win32;

namespace MS.Internal
{
    // levels for the various extended traces
    internal enum TraceDataLevel
    {
        // Binding and friends
        CreateExpression    = PresentationTraceLevel.High, // 10,
        ShowPath            = PresentationTraceLevel.High, // 11,
        ResolveDefaults     = PresentationTraceLevel.High, // 13,
        Attach              = PresentationTraceLevel.Low, // 1,
        AttachToContext     = PresentationTraceLevel.Low, // 2,
        SourceLookup        = PresentationTraceLevel.Low, // 4,
        Activate            = PresentationTraceLevel.Low, // 3,
        Transfer            = PresentationTraceLevel.Medium, // 5,
        Update              = PresentationTraceLevel.Medium, // 6,
        Validation          = PresentationTraceLevel.High, // 12,
        Events              = PresentationTraceLevel.Medium, // 7,
        GetValue            = PresentationTraceLevel.High, // 12,
        ReplaceItem         = PresentationTraceLevel.Medium, // 8,
        GetInfo             = PresentationTraceLevel.Medium, // 9,

        // Data providers
        ProviderQuery       = PresentationTraceLevel.Low, // 1,
        XmlProvider         = PresentationTraceLevel.Medium, // 2,
        XmlBuildCollection  = PresentationTraceLevel.High, // 3,
    }

    /// <summary>
    /// Provides a central mechanism for providing debugging information
    /// to aid programmers in using data binding.
    /// Helpers are defined here.
    /// The rest of the class is generated; see also: AvTraceMessage.txt and genTraceStrings.pl
    /// </summary>
    internal static partial class TraceData
    {
        // ------------------------------------------------------------------
        // Constructors
        // ------------------------------------------------------------------

        static TraceData()
        {
            _avTrace.TraceExtraMessages += new AvTraceEventHandler(OnTrace);

            // This tells tracing that IsEnabled should be true if we're in the debugger,
            // even if the registry flag isn't turned on.  By default, IsEnabled is only
            // true if the registry is set.
            _avTrace.EnabledByDebugger = true;

            // This tells the tracing code not to automatically generate the .GetType
            // and .HashCode in the trace strings.
            _avTrace.SuppressGeneratedParameters = true;
        }

        // ------------------------------------------------------------------
        // Methods
        // ------------------------------------------------------------------

        // determine whether an extended trace should be produced for the given
        // object
        static public bool IsExtendedTraceEnabled(object element, TraceDataLevel level)
        {
            if (TraceData.IsEnabled)
            {
                PresentationTraceLevel traceLevel = PresentationTraceSources.GetTraceLevel(element);
                return (traceLevel >= (PresentationTraceLevel)level);
            }
            else
                return false;
        }

        // report/describe any additional parameters passed to TraceData.Trace()
        static public void OnTrace( AvTraceBuilder traceBuilder, object[] parameters, int start )
        {
            for( int i = start; i < parameters.Length; i++ )
            {
                object o = parameters[i];
                string s = o as string;
                traceBuilder.Append(" ");
                if (s != null)
                {
                    traceBuilder.Append(s);
                }
                else if (o != null)
                {
                    traceBuilder.Append(o.GetType().Name);
                    traceBuilder.Append(":");
                    Describe(traceBuilder, o);
                }
                else
                {
                    traceBuilder.Append("null");
                }
            }
        }

        // ------------------------------------------------------------------
        // Helper functions for message string construction
        // ------------------------------------------------------------------

        /// <summary>
        /// Construct a string that describes data and debugging information about the object.
        /// A title is appended in front if provided.
        /// If object o is not a recognized object, it will be ToString()'ed.
        /// </summary>
        /// <param name="traceBuilder">description will be appended to this builder</param>
        /// <param name="o">object to be described;
        /// currently recognized types: BindingExpression, Binding, DependencyObject, Exception</param>
        /// <returns>a string that describes the object</returns>
        static public void Describe(AvTraceBuilder traceBuilder, object o)
        {
            if (o == null)
            {
                traceBuilder.Append("null");
            }

            else if (o is BindingExpression)
            {
                BindingExpression bindingExpr = o as BindingExpression;

                Describe(traceBuilder, bindingExpr.ParentBinding);
                traceBuilder.Append("; DataItem=");
                DescribeSourceObject(traceBuilder, bindingExpr.DataItem);
                traceBuilder.Append("; ");
                DescribeTarget(traceBuilder, bindingExpr.TargetElement, bindingExpr.TargetProperty);
            }

            else if (o is Binding)
            {
                Binding binding = o as Binding;
                if (binding.Path != null)
                    traceBuilder.AppendFormat("Path={0}", binding.Path.Path );
                else if (binding.XPath != null)
                    traceBuilder.AppendFormat("XPath={0}", binding.XPath );
                else
                    traceBuilder.Append("(no path)");
            }

            else if (o is BindingExpressionBase)
            {
                BindingExpressionBase beb = o as BindingExpressionBase;
                DescribeTarget(traceBuilder, beb.TargetElement, beb.TargetProperty);
            }

            else if (o is DependencyObject)
            {
               DescribeSourceObject(traceBuilder, o);
            }

            else
            {
                traceBuilder.AppendFormat("'{0}'", AvTrace.ToStringHelper(o));
            }
        }

        /// <summary>
        /// Produces a string that describes a source object:
        /// e.g. element in a Binding Path, DataItem in BindingExpression, ContextElement
        /// </summary>
        /// <param name="traceBuilder">description will be appended to this builder</param>
        /// <param name="o">a source object (e.g. element in a Binding Path, DataItem in BindingExpression, ContextElement)</param>
        /// <returns>a string that describes the object</returns>
        static public void DescribeSourceObject(AvTraceBuilder traceBuilder, object o)
        {
            if (o == null)
            {
                traceBuilder.Append("null");
            }
            else
            {
                FrameworkElement fe = o as FrameworkElement;
                if (fe != null)
                {
                    traceBuilder.AppendFormat("'{0}' (Name='{1}')", fe.GetType().Name, fe.Name);
                }
                else
                {
                    traceBuilder.AppendFormat("'{0}' (HashCode={1})", o.GetType().Name, o.GetHashCode());
                }
            }
        }

        /// <summary>
        /// </summary>
        static public string DescribeSourceObject(object o)
        {
            AvTraceBuilder atb = new AvTraceBuilder(null);
            DescribeSourceObject(atb, o);
            return atb.ToString();
        }

        /// <summary>
        /// Produces a string that describes TargetElement and TargetProperty
        /// </summary>
        /// <param name="traceBuilder">description will be appended to this builder</param>
        /// <param name="targetElement">TargetElement</param>
        /// <param name="targetProperty">TargetProperty</param>
        /// <returns>a string that describes TargetElement and TargetProperty</returns>
        static public void DescribeTarget(AvTraceBuilder traceBuilder, DependencyObject targetElement, DependencyProperty targetProperty)
        {
            if (targetElement != null)
            {
                traceBuilder.Append("target element is ");
                DescribeSourceObject(traceBuilder, targetElement);
                if (targetProperty != null)
                {
                    traceBuilder.Append("; ");
                }
            }

            if (targetProperty != null)
            {
                traceBuilder.AppendFormat("target property is '{0}' (type '{1}')", targetProperty.Name, targetProperty.PropertyType.Name);
            }
        }

        /// <summary>
        /// </summary>
        static public string DescribeTarget(DependencyObject targetElement, DependencyProperty targetProperty)
        {
            AvTraceBuilder atb = new AvTraceBuilder(null);
            DescribeTarget(atb, targetElement, targetProperty);
            return atb.ToString();
        }

        static public string Identify(object o)
        {
            if (o == null)
                return "<null>";

            Type type = o.GetType();

            if (type.IsPrimitive || type.IsEnum)
                return Format("'{0}'", o);

            string s = o as String;
            if (s != null)
                return Format("'{0}'", AvTrace.AntiFormat(s));

            NamedObject n = o as NamedObject;
            if (n != null)
                return AvTrace.AntiFormat(n.ToString());

            ICollection ic = o as ICollection;
            if (ic != null)
                return Format("{0} (hash={1} Count={2})", type.Name, AvTrace.GetHashCodeHelper(o), ic.Count);

            return Format("{0} (hash={1})", type.Name, AvTrace.GetHashCodeHelper(o));
        }

        static public string IdentifyWeakEvent(Type type)
        {
            const string suffix = "EventManager";
            string name = type.Name;
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }

            return name;
        }

        static public string IdentifyAccessor(object accessor)
        {
            DependencyProperty dp = accessor as DependencyProperty;
            if (dp != null)
                return Format("{0}({1})", dp.GetType().Name, dp.Name);

            PropertyInfo pi = accessor as PropertyInfo;;
            if (pi != null)
                return Format("{0}({1})", pi.GetType().Name, pi.Name);

            PropertyDescriptor pd = accessor as PropertyDescriptor;;
            if (pd != null)
                return Format("{0}({1})", pd.GetType().Name, pd.Name);

            return Identify(accessor);
        }

        static public string IdentifyException(Exception ex)
        {
            if (ex == null)
                return "<no error>";

            return Format("{0} ({1})", ex.GetType().Name, AvTrace.AntiFormat(ex.Message));
        }

        static string Format(string format, params object[] args)
        {
            return String.Format(TypeConverterHelper.InvariantEnglishUS, format, args);
        }
    }
}
