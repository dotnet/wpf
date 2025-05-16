// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.IO;

namespace MS.Internal
{
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
                throw new InvalidOperationException(SR.Format(SR.Verify_ApartmentState, requiredState));
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
            ArgumentException.ThrowIfNullOrEmpty(filePath, parameterName);

            if (!File.Exists(filePath))
            {
                throw new ArgumentException(SR.Format(SR.Verify_FileExists, filePath), parameterName);
            }
        }
    }
}

