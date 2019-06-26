// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace MS.Internal 
{
    using System;
    using System.Diagnostics;
    using System.Security;
    using System.Threading;
    using System.IO;
    using MS.Internal.WindowsBase;

    /// <summary>
    /// A static class for retail validated assertions.
    /// Instead of breaking into the debugger an exception is thrown.
    /// </summary>
    internal static class Verify
    {
        /// <summary>
        /// Ensure that the current thread's apartment state is what's expected.
        /// </summary>
        /// <param name="requiredState">
        /// The required apartment state for the current thread.
        /// </param>
        /// <param name="message">
        /// The message string for the exception to be thrown if the state is invalid.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the calling thread's apartment state is not the same as the requiredState.
        /// </exception>
        public static void IsApartmentState(ApartmentState requiredState)
        {
            if (Thread.CurrentThread.GetApartmentState() != requiredState)
            {
                throw new InvalidOperationException(SR.Get(SRID.Verify_ApartmentState, requiredState));
            }
        }

        /// <summary>
        /// Ensure that an argument is neither null nor empty.
        /// </summary>
        /// <param name="value">The string to validate.</param>
        /// <param name="name">The name of the parameter that will be presented if an exception is thrown.</param>
        public static void IsNeitherNullNorEmpty(string value, string name)
        {
            // catch caller errors, mixing up the parameters.  Name should never be empty.
            Debug.Assert(!string.IsNullOrEmpty(name));

            // Notice that ArgumentNullException and ArgumentException take the parameters in opposite order :P
            if (value == null)
            {
                throw new ArgumentNullException(name, SR.Get(SRID.Verify_NeitherNullNorEmpty));
            }
            if (value == "")
            {
                throw new ArgumentException(SR.Get(SRID.Verify_NeitherNullNorEmpty), name);
            }
        }

        /// <summary>Verifies that an argument is not null.</summary>
        /// <typeparam name="T">Type of the object to validate.  Must be a class.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <param name="name">The name of the parameter that will be presented if an exception is thrown.</param>
        public static void IsNotNull<T>(T obj, string name) where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Verifies the specified expression is true.  Throws an ArgumentException if it's not.
        /// </summary>
        /// <param name="expression">The expression to be verified as true.</param>
        /// <param name="name">Name of the parameter to include in the ArgumentException.</param>
        /// <param name="message">The message to include in the ArgumentException.</param>
        public static void IsTrue(bool expression, string name, string message)
        {
            if (!expression)
            {
                throw new ArgumentException(message, name);
            }
        }

        /// <summary>
        /// Verifies two values are not equal to each other.  Throws an ArgumentException if they are.
        /// </summary>
        /// <param name="actual">The actual value.</param>
        /// <param name="notExpected">The value that 'actual' should not be.</param>
        /// <param name="parameterName">The name to display for 'actual' in the exception if this test fails.</param>
        /// <param name="message">The message to include in the ArgumentException.</param>
        public static void AreNotEqual<T>(T actual, T notExpected, string parameterName, string message)
        {
            if (notExpected == null)
            {
                // Two nulls are considered equal, regardless of type semantics.
                if (actual == null || actual.Equals(notExpected))
                {
                    throw new ArgumentException(SR.Get(SRID.Verify_AreNotEqual, notExpected), parameterName);
                }
            }
            else if (notExpected.Equals(actual))
            {
                throw new ArgumentException(SR.Get(SRID.Verify_AreNotEqual, notExpected), parameterName);
            }
        }

        /// <summary>
        /// Verifies the specified file exists.  Throws an ArgumentException if it doesn't.
        /// </summary>
        /// <param name="filePath">The file path to check for existence.</param>
        /// <param name="parameterName">Name of the parameter to include in the ArgumentException.</param>
        /// <remarks>This method demands FileIOPermission(FileIOPermissionAccess.PathDiscovery)</remarks>
        public static void FileExists(string filePath, string parameterName)
        {
            Verify.IsNeitherNullNorEmpty(filePath, parameterName);

            if (!File.Exists(filePath))
            {
                throw new ArgumentException(SR.Get(SRID.Verify_FileExists, filePath), parameterName);
            }
        }
    }
}

