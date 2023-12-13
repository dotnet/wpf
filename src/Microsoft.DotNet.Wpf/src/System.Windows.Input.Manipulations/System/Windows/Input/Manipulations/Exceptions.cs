// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Provides all exceptions thrown from this assembly.
    /// </summary>
    internal static class Exceptions
    {
        /// <summary>
        /// Gets an exception for when an argument that should be finite, isn't.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Exception ValueMustBeFinite(string paramName, object value)
        {
            return ArgumentOutOfRange(paramName, value, SR.ValueMustBeFinite);
        }

        /// <summary>
        /// Gets an exception indicating that an argument needs to be either NaN or finite.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Exception ValueMustBeFiniteOrNaN(string paramName, object value)
        {
            return ArgumentOutOfRange(paramName, value, SR.ValueMustBeFiniteOrNaN);
        }

        /// <summary>
        /// Gets an exception indicating that an argument needs to be finite and non-negative.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Exception ValueMustBeFiniteNonNegative(string paramName, object value)
        {
            return ArgumentOutOfRange(paramName, value, SR.ValueMustBeFiniteNonNegative);
        }

        /// <summary>
        /// Gets an exception for an illegal pivot radius.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Exception IllegalPivotRadius(string paramName, object value)
        {
            return ArgumentOutOfRange(paramName, value, SR.IllegalPivotRadius);
        }

        /// <summary>
        /// Gets an exception for an illegal inertia processor radius.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Exception IllegialInertiaRadius(string paramName, object value)
        {
            return ArgumentOutOfRange(paramName, value, SR.InertiaProcessorInvalidRadius);
        }

        /// <summary>
        /// Gets an exception for an invalid timestamp.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Exception InvalidTimestamp(string paramName, long value)
        {
            return ArgumentOutOfRange(paramName, value, SR.InvalidTimestamp);
        }

        /// <summary>
        /// Gets a generic argument-out-of-range exception.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Exception ArgumentOutOfRange(string paramName, object value)
        {
            return ArgumentOutOfRange(paramName, value, SR.ArgumentOutOfRange);
        }

        /// <summary>
        /// Gets an exception indicating that only proportional expansion is supported.
        /// </summary>
        /// <param name="paramName1"></param>
        /// <param name="paramName2"></param>
        /// <returns></returns>
        public static Exception OnlyProportionalExpansionSupported(
            string paramName1,
            string paramName2)
        {
            Debug.Assert(paramName1 != null);
            Debug.Assert(paramName2 != null);
            return new NotSupportedException(Format(
                SR.OnlyProportionalExpansionSupported,
                paramName1,
                paramName2));
        }

        /// <summary>
        /// Gets an exception indicating that an inertia parameter needs to be specified.
        /// </summary>
        /// <param name="paramName1"></param>
        /// <param name="paramName2"></param>
        /// <returns></returns>
        public static Exception InertiaParametersUnspecified2(
            string paramName1,
            string paramName2)
        {
            Debug.Assert(paramName1 != null);
            Debug.Assert(paramName2 != null);
            return new InvalidOperationException(Format(
                SR.InertiaParametersUnspecified2,
                paramName1,
                paramName2));
        }

        /// <summary>
        /// Gets an exception indicating that an inertia parameter needs to be specified.
        /// </summary>
        /// <param name="paramName1"></param>
        /// <param name="paramName2a"></param>
        /// <param name="paramName2b"></param>
        /// <returns></returns>
        public static Exception InertiaParametersUnspecified1and2(
            string paramName1,
            string paramName2a,
            string paramName2b)
        {
            Debug.Assert(paramName1 != null);
            Debug.Assert(paramName2a != null);
            Debug.Assert(paramName2b != null);
            return new InvalidOperationException(Format(
                SR.InertiaParametersUnspecified1and2,
                paramName1,
                paramName2a,
                paramName2b));
        }

        /// <summary>
        /// Gets an exception indicating that an inertia parameter cannot be changed
        /// while the inertia processor is running.
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static Exception CannotChangeParameterDuringInertia(string paramName)
        {
            return new InvalidOperationException(Format(
                SR.CannotChangeParameterDuringInertia,
                paramName));
        }

        /// <summary>
        /// Gets an exception indicating that inertia can't occur unless at least
        /// one velocity is specified.
        /// </summary>
        /// <param name="linearVelocityXParamName"></param>
        /// <param name="linearVelocityYParamName"></param>
        /// <param name="angularVelocityParamName"></param>
        /// <param name="expansionVelocityXParamName"></param>
        /// <param name="expansionVelocityYParamName"></param>
        /// <returns></returns>
        public static Exception NoInertiaVelocitiesSpecified(
            string linearVelocityXParamName,
            string linearVelocityYParamName,
            string angularVelocityParamName,
            string expansionVelocityXParamName,
            string expansionVelocityYParamName)
        {
            return new InvalidOperationException(Format(
                SR.NoInertiaVelocitiesSpecified,
                linearVelocityXParamName,
                linearVelocityYParamName,
                angularVelocityParamName,
                expansionVelocityXParamName,
                expansionVelocityYParamName));
        }

        /// <summary>
        /// Gets an argument-out-of-range exception with the specified message.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <param name="messageFormat"></param>
        private static ArgumentOutOfRangeException ArgumentOutOfRange(
            string paramName,
            object value,
            string messageFormat)
        {
            Debug.Assert(paramName != null);
            Debug.Assert(messageFormat != null);

            string valueName = IsPropertyName(paramName) ? "value" : paramName;
            string message = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                messageFormat,
                paramName);
            return new ArgumentOutOfRangeException(valueName, value, message);
        }

        /// <summary>
        /// Determines whether the specified string is a property name, as opposed
        /// to a value name.
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        private static bool IsPropertyName(string paramName)
        {
            Debug.Assert(paramName != null);
            Debug.Assert(paramName.Length > 0);

            // We use capital letters to indicate property names. The
            // following code is culture-unsafe, and thus would be wholly
            // inappropriate for any string either taken from a user or from
            // localized SR.  We're only using it for hard-coded parameter
            // names from our own code, however, so this is fine.
            char firstLetter = paramName[0];
            return (firstLetter >= 'A') && (firstLetter <= 'Z');
        }

        /// <summary>
        /// Formats a string.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string Format(string format, params object[] args)
        {
            Debug.Assert(format != null);
            return string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                format,
                args);
        }
    }
}
