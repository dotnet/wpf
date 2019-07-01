// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.ComponentModel;
using Microsoft.Test.Globalization;

namespace Microsoft.Test
{
    public delegate void ExceptionTriggerCallback();
    public delegate void ExceptionValidationCallback<T>(T exception) where T : Exception;

    public static class ExceptionHelper
    {
        public static void ExpectException<T>(ExceptionTriggerCallback exceptionTrigger, ExceptionValidationCallback<T> exceptionValidation) where T : Exception
        {
            try
            {
                exceptionTrigger();
            }
            catch (T exception)
            {
                exceptionValidation(exception);
                return;
            }

            throw new TestValidationException("No exception triggered.");
        }

        public static void ExpectException<T>(ExceptionTriggerCallback exceptionTrigger, T expectedException) where T : Exception
        {
            ExpectException(exceptionTrigger, expectedException, null, WpfBinaries.PresentationFramework);
        }

        public static void ExpectException<T>(ExceptionTriggerCallback exceptionTrigger, T expectedException, string resourceId, WpfBinaries targetBinary) where T : Exception
        {
            ExpectException(exceptionTrigger, delegate(T caughtException)
            {
                if (!String.IsNullOrEmpty(resourceId) && !Exceptions.CompareMessage(caughtException.Message, resourceId, targetBinary))
                {
                    throw new TestValidationException("Expected Message: " + Exceptions.GetMessage(resourceId, targetBinary) + ", Actual Message: " + caughtException.Message);
                }

                PropertyDescriptorCollection baseClassProperties = TypeDescriptor.GetProperties(typeof(Exception));
                PropertyDescriptorCollection subClassProperties = TypeDescriptor.GetProperties(typeof(T));

                foreach (PropertyDescriptor property in subClassProperties)
                {
                    if (!baseClassProperties.Contains(property))
                    {
                        if (!Object.Equals(property.GetValue(caughtException), property.GetValue(expectedException)))
                        {
                            throw new TestValidationException("Property Name: " + property.Name + ", Expected Value: " + property.GetValue(expectedException) + ", Actual Value: " + property.GetValue(caughtException));
                        }
                    }
                }

                return;
            });
        }
    }
}
