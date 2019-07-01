// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using Microsoft.Test.Logging;
using System.Globalization;
using System.Runtime.Loader;
using System.IO;

namespace Microsoft.Test
{
    /// <summary>
    /// Simple Test Invocation driver.
    /// </summary>
    public sealed class SimpleTestInvoke
    {
        const string debugSti = "debugsti";
        private SimpleTestInvoke()
        {
           
        }

        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Any(arg => arg.EndsWith(debugSti, StringComparison.OrdinalIgnoreCase)))
            {
                LogManager.LogMessageDangerously("Waiting for debugger to attach to sti.exe...");
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                System.Diagnostics.Debugger.Break();
            }

            string[] testArguments = args.Where(arg => !arg.EndsWith(debugSti, StringComparison.OrdinalIgnoreCase)).ToArray();

            // STI needs to hook into assembly resolution so that we can dynamically load
            // DLLs that are not a part of STI.deps.json.  This allows us to always load
            // requested assemblies from the current directory.  This is the layout that
            // QualityVault uses when it collates tests into a single directory.
            AssemblyLoadContext.Default.Resolving += (context, asm) => {
                try
                {
                    return context.LoadFromAssemblyPath(Path.GetFullPath(asm.Name + ".dll"));
                }
                catch
                {
                    return null;
                }
            };

            try
            {
                ExecuteTestCase(testArguments);
            }
            catch (Exception e)
            {
                Console.WriteLine("A severe driver failure has occured.");
                Console.WriteLine(e);
            }

            // If we get to this point we were able to run the test case, which
            // means we return 0 to indicate successful execution of Sti. Note
            // that this exit code does not represent the result of the actual
            // test case executed, but rather the execution itself.
            return 0;
        }
        
        /// <summary>
        /// Find test case defined in property bag by reflection and execute.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        // This method needs to be public because it is given as an AppDomain callback, and if
        // the created appdomain is partial trust, non-public accessibility throws a security exception.
        public static void ExecuteTestCase(string[] args)
        {

            StiDriverParameters driverParameters = new StiDriverParameters(DriverState.DriverParameters, args);
#if TESTBUILD_CLR20
            StiUtilities.ApplySecurityPolicy(driverParameters.SecurityLevel);
#endif

            bool hasNext = false;
            do
            {
                driverParameters = new StiDriverParameters(DriverState.DriverParameters, args);

                LogManager.BeginTest(DriverState.TestName);
                try
                {
                    LogReproArguments();

                    InvokeTestMethod(driverParameters);

                    // If there is an open variation, we need to close it down so we can end the test.
                    if (Variation.Current != null)
                    {
                        if (Variation.Current.Result != null)
                        {
                            Variation.Current.LogMessage("FAILURE: Test did not close its variation. Because it already logged a result, we will close it and create a synthetic variation to report failure.");
                            Variation.Current.Close();
                            Log.Current.CreateVariation("Variation not closed synthetic variation");
                        }
                        Variation.Current.LogMessage("FAILURE: Test did not close its variation.");
                        Variation.Current.LogResult(Result.Fail);
                        Variation.Current.Close();
                    }
                }
                catch (Exception exception)
                {
                    // If there is an open variation, we need to close it down so we can end the test.
                    if (Variation.Current != null)
                    {
                        Variation.Current.LogObject(exception);
                        Variation.Current.LogResult(Result.Fail);
                        Variation.Current.Close();
                    }
                    else
                    {
                        LogManager.LogMessageDangerously(exception.ToString());
                    }
                }
                finally
                {
                    LogManager.LogMessageDangerously(string.Format(CultureInfo.InvariantCulture, "Repro Arguments: /Name={0} /Area={1} /SubArea={2}", DriverState.TestName, DriverState.TestArea, DriverState.TestSubArea)); 
                    LogManager.EndTest();
                }

                hasNext = DriverState.HasNext();
                if (hasNext)
                {
                    DriverState.Next();
                }
            } while (hasNext);
        }

        private static void LogReproArguments()
        {
            if (String.IsNullOrEmpty(DriverState.TestSubArea))
            {
                LogManager.LogMessageDangerously(string.Format(CultureInfo.InvariantCulture, "Repro Arguments: /Name={0} /Area={1}", DriverState.TestName, DriverState.TestArea));
            }
            else
            {
                LogManager.LogMessageDangerously(string.Format(CultureInfo.InvariantCulture, "Repro Arguments: /Name={0} /Area={1} /SubArea={2}", DriverState.TestName, DriverState.TestArea, DriverState.TestSubArea));
            }
        }

        private static void InvokeTestMethod(StiDriverParameters driverParameters)
        {
            MethodBase methodToInvoke = GetMethodBaseFromLooselyTypedArguments(driverParameters.Class, driverParameters.Method, driverParameters.MethodParams);
            object[] methodParameters = new object[driverParameters.MethodParams.Length];
            TypeConvertParameters(methodToInvoke, driverParameters.MethodParams, ref methodParameters);

            if (methodToInvoke.IsStatic)
            {
                methodToInvoke.Invoke(null, methodParameters);
            }
            else
            {
                if (driverParameters.Class.IsAbstract)
                {
                    throw new TargetException("Non-static method " + methodToInvoke.Name + " on abstract class " + driverParameters.Class.Name + " cannot be invoked.");
                }

                MethodBase constructorToInvoke = GetMethodBaseFromLooselyTypedArguments(driverParameters.Class, driverParameters.Class.Name, driverParameters.CtorParams);
                if (constructorToInvoke == null)
                {
                    throw new MissingMethodException("No constructor with matching signature found.");
                }
                else if (constructorToInvoke as ConstructorInfo == null)
                {
                    throw new TargetException("MethodBase with name matching class name was not a ConstructorInfo.");
                }

                object[] ctorParameters = new object[driverParameters.CtorParams.Length];
                TypeConvertParameters(constructorToInvoke, driverParameters.CtorParams, ref ctorParameters);

                object testObj = (constructorToInvoke as ConstructorInfo).Invoke(ctorParameters);

                methodToInvoke.Invoke(testObj, methodParameters);
            }
        }

        /// <summary>
        /// Given a type and method name, try to find an overload of that
        /// method name that has a parameter signature that the loosely
        /// typed array of string values can be TypeConverted to match.
        /// For example, if the array values were 'true' and '-1', a method
        /// overload that takes a bool and an int would match.
        /// </summary>
        /// <param name="type">Declaring type.</param>
        /// <param name="methodName">Name of method to match.</param>
        /// <param name="args">String representation of parameter values.</param>
        /// <returns>MethodBase corresponding to a method with a matching signature.</returns>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        private static MethodBase GetMethodBaseFromLooselyTypedArguments(Type type, string methodName, string[] args)
        {
            int numArgs = 0;
            if (args != null) numArgs = args.Length;
            object[] objArgs = new object[numArgs];

            IEnumerable<MethodBase> methodBases;

            if (methodName == type.Name)
            {
                methodBases = type.GetConstructors();
            }
            else
            {
                // Get all methods on the type and filter for those whose name is 'methodName'.
                methodBases = type.GetMethods().Cast<MethodBase>().Where(methodBase => methodBase.Name == methodName);
            }

            foreach (MethodBase methodBase in methodBases)
            {
                if (TypeConvertParameters(methodBase, args, ref objArgs))
                {
                    return methodBase;
                }
            }

            return null;
        }

        /// <summary>
        /// Given an array of string value representations of parameter values,
        /// convert them to strong types matching those specificed by the MethodBase,
        /// and then populate the object array with those converted values.
        /// </summary>
        /// <param name="mBase">MethodBase describing the method the parameters are for.</param>
        /// <param name="args">String value representation of parameters.</param>
        /// <param name="objArgs">Object array to populate with converted values.</param>
        /// <returns>Whether the object array was populated successfully.</returns>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        private static bool TypeConvertParameters(MethodBase mBase, string[] args, ref object[] objArgs)
        {
            int numArgs = 0;
            if (args != null) numArgs = args.Length;

            ParameterInfo[] pInfos = mBase.GetParameters();

            // Easy comparisons
            if (pInfos.Length != numArgs)
                return false;
            if (pInfos.Length == 0 && numArgs == 0)
                return true;
    
            // Match convert parameters.
            for (int i = 0; i < pInfos.Length; i++)
            {
                ParameterInfo pInfo = pInfos[i];
                TypeConverter converter = TypeDescriptor.GetConverter(pInfo.ParameterType);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    try
                    {
                        objArgs[i] = converter.ConvertFromInvariantString(args[i]);
                    }
                    catch (NotSupportedException)
                    {
                        // ConvertFromInvariantString always fails by throwing an exception.
                        return false;
                    }
                }
                // Type is special cased because the TypeConverter for Type doesn't think it
                // can convert from a String, but we know that if the String is the full
                // assembly qualified name, then Type.GetType can.
                else if (pInfo.ParameterType as Type != null)
                {
                    try
                    {
                        objArgs[i] = Type.GetType(args[i]);
                    }
                    catch (TypeLoadException)
                    {
                        return false;
                    }
                }
                else
                    return false;
            }

            return true;
        }
    }
}