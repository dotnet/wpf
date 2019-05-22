// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Provides helper methods for invoking methods, accessing fields and properties via reflection
//

#if WINDOWS_BASE
using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
using MS.Internal.WindowsRuntime;
#elif REACHFRAMEWORK
using MS.Internal.ReachFramework;
#else
using MS.Internal;
#endif

using System;
using System.Runtime.InteropServices;
using System.Security;

#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif PRESENTATION_CORE
namespace MS.Internal.PresentationCore
#elif REACHFRAMEWORK
namespace MS.Internal.ReachFramework
#else
namespace MS.Internal
#endif
{
    namespace WindowsRuntime
    {
        using System;
        using System.Collections.Generic;
        using System.Reflection;

        internal static class ReflectionHelper
        {
            /// <summary>
            /// Calls method <i>methodName</i> statically from the type <i>type</i>.
            /// This function is needed since a static reflection call requires a null object be passed in.
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="type"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>type</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="AmbiguousMatchException">More than one method of the form <i>methodname()</i> was found</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            /// <exception cref="InvalidCastException">The result of the method call cannot be successfully cast to <i>TResult</i></exception>
            public static TResult ReflectionStaticCall<TResult>(this Type type, string methodName)
            {
                MethodInfo method;
                object result;

                method = type.GetMethod(methodName, Type.EmptyTypes);

                if (method == null)
                {
                    throw new MissingMethodException(methodName);
                }

                result = method.Invoke(null, null);

                return (TResult)result;
            }

            /// <summary>
            /// Calls method <i>methodName</i> statically from type <i>type</i>.
            /// This function is needed since a static reflection call requires a null object to be passed in.
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <typeparam name="TArg"></typeparam>
            /// <param name="type"></param>
            /// <param name="methodName"></param>
            /// <param name="arg"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>type</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            /// <exception cref="InvalidCastException">The result of the method call cannot be successfully cast to <i>TResult</i></exception>
            public static TResult ReflectionStaticCall<TResult, TArg>(this Type type, string methodName, TArg arg)
            {
                MethodInfo method;
                object result;

                method = type.GetMethod(methodName, new Type[] { typeof(TArg) });

                if (method == null)
                {
                    throw new MissingMethodException(methodName);
                }

                result = method.Invoke(null, new object[] { arg });

                return (TResult)result;
            }

            /// <summary>
            /// Calls method <i>methodName</i> on object <i>obj</i>
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="obj"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>obj</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="AmbiguousMatchException">More than one method of the form <i>methodname()</i> was found</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            /// <exception cref="InvalidCastException">The result of the method call cannot be successfully cast to <i>Result</i></exception>
            public static TResult ReflectionCall<TResult>(this object obj, string methodName)
            {
                object result = obj.ReflectionCall(methodName);
                return (TResult)result;
            }

            /// <summary>
            /// Calls method <i>methodName</i> on object <i>obj</i>
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>obj</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="AmbiguousMatchException">More than one method of the form <i>methodname()</i> was found</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            public static object ReflectionCall(this object obj, string methodName)
            {
                MethodInfo method;
                object result;


                method = obj.GetType().GetMethod(methodName, Type.EmptyTypes);

                if (method == null)
                {
                    throw new MissingMethodException(methodName);
                }

                result = method.Invoke(obj, null);

                return result;
            }


            /// <summary>
            /// Calls method <i>methodName</i> on object <i>obj</i>
            /// </summary>
            /// <typeparam name="TArg1"></typeparam>
            /// <param name="obj"></param>
            /// <param name="methodName"></param>
            /// <param name="arg1"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>obj</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="AmbiguousMatchException">More than one method of the form <i>methodname()</i> was found</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            public static object ReflectionCall<TArg1>(this object obj, string methodName, TArg1 arg1)
            {
                MethodInfo method;
                object result;

                method = obj.GetType().GetMethod(methodName, new Type[] { typeof(TArg1) });
                if (method == null)
                {
                    throw new MissingMethodException(methodName);
                }

                result = method.Invoke(obj, new object[] { arg1 });

                return result;
            }

            /// <summary>
            /// Calls method <i>methodName</i> on object <i>obj</i>
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <typeparam name="TArg1"></typeparam>
            /// <param name="obj"></param>
            /// <param name="methodName"></param>
            /// <param name="arg1"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>obj</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="AmbiguousMatchException">More than one method of the form <i>methodname()</i> was found</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            /// <exception cref="InvalidCastException">The result of the method call cannot be successfully cast to <i>Result</i></exception>
            public static TResult ReflectionCall<TResult, TArg1>(this object obj, string methodName, TArg1 arg1)
            {
                object result = obj.ReflectionCall<TArg1>(methodName, arg1);
                return (TResult)result;
            }

            /// <summary>
            /// Calls method <i>methodName</i> on object <i>obj</i>
            /// </summary>
            /// <typeparam name="TArg1"></typeparam>
            /// <typeparam name="TArg2"></typeparam>
            /// <param name="obj"></param>
            /// <param name="methodName"></param>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>obj</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="AmbiguousMatchException">More than one method of the form <i>methodname()</i> was found</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            public static object ReflectionCall<TArg1, TArg2>(this object obj, string methodName, TArg1 arg1, TArg2 arg2)
            {
                MethodInfo method;
                object result;

                method = obj.GetType().GetMethod(methodName, new Type[] { typeof(TArg1), typeof(TArg2) });
                if (method == null)
                {
                    throw new MissingMethodException(methodName);
                }

                result = method.Invoke(obj, new object[] { arg1, arg2 });

                return result;
            }

            /// <summary>
            /// Calls method <i>methodName</i> on object <i>obj</i>
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <typeparam name="TArg1"></typeparam>
            /// <typeparam name="TArg2"></typeparam>
            /// <param name="obj"></param>
            /// <param name="methodName"></param>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>obj</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>methodName</i> is <b>null</b></exception>
            /// <exception cref="TargetInvocationException">The invoked method throws an exception</exception>
            /// <exception cref="AmbiguousMatchException">More than one method of the form <i>methodname()</i> was found</exception>
            /// <exception cref="MissingMethodException">The method <i>methodName</i> was not found</exception>
            /// <exception cref="MethodAccessException">The caller does not have permission to execute the method</exception>
            /// <exception cref="InvalidCastException">The result of the method call cannot be successfully cast to <i>Result</i></exception>
            public static TResult ReflectionCall<TResult, TArg1, TArg2>(this object obj, string methodName, TArg1 arg1, TArg2 arg2)
            {
                object result = obj.ReflectionCall<TArg1, TArg2>(methodName, arg1, arg2);
                return (TResult)result;
            }

            /// <summary>
            /// Gets field name <i>fieldName</i> from object <i>obj</i>. Use this method for accessing fields from structs.
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="obj"></param>
            /// <param name="fieldName"></param>
            /// <returns></returns>
            /// <exception cref="NullReferenceException"><i>obj</i> is <b>null</b></exception>
            /// <exception cref="ArgumentNullException"><i>fieldName</i> is <b>null</b></exception>
            /// <exception cref="FieldAccessException">The caller does not have permission to acccess this field</exception>
            /// <exception cref="MissingFieldException">The Field <i>fieldName</i> was not found</exception>
            /// <exception cref="InvalidCastException">The result of the method call cannot be successfully cast to <i>Result</i></exception>
            public static TResult ReflectionGetField<TResult>(this object obj, string fieldName)
            {
                FieldInfo fieldInfo;
                object result;
                
                fieldInfo = obj.GetType().GetField(fieldName);
                if (fieldInfo == null)
                {
                    throw new MissingFieldException(fieldName);
                }

                result = fieldInfo.GetValue(obj);

                return (TResult)result;
            }


            /// <summary>
            /// Calls default constructor for type <i>type</i> and returns an instance
            /// </summary>
            /// <param name="type"></param>
            /// <returns><b>null if default constructor doesn't exist</b></returns>
            /// <exception cref="NullReferenceException"><i>type</i> is null</exception>
            /// <exception cref="MemberAccessException">The class is abstract, or the constructor is a class initializer</exception>
            /// <exception cref="MethodAccessException">The constructor is private or protected, and the caller lacks ReflectionPermissionFlag.MemberAccess/></exception>
            /// <exception cref="TargetInvocationException">The invoked constructor throws an exception</exception>
            /// <exception cref="System.Security.SecurityException">The caller does not have the necessary code access permission</exception>
            /// <exception cref="MissingMethodException">A default constructor does not exist</exception>
            public static object ReflectionNew(this Type type)
            {
                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                {
                    string constructorName = type.FullName + "." + type.Name  + "()";
                    throw new MissingMethodException(constructorName);
                }

                return constructor.Invoke(null);
            }

            /// <summary>
            /// Calls a constructor with one arg of type <i>Arg1</i>
            /// </summary>
            /// <typeparam name="TArg1"></typeparam>
            /// <param name="type"></param>
            /// <param name="arg1"></param>
            /// <returns><b>null</b> if a matching constructor can't be found</returns>
            /// <exception cref="NullReferenceException"><i>type</i> is null</exception>
            /// <exception cref="MemberAccessException">The class is abstract, or the constructor is a class initializer</exception>
            /// <exception cref="MethodAccessException">The constructor is private or protected, and the caller lacks ReflectionPermissionFlag.MemberAccess/></exception>
            /// <exception cref="TargetInvocationException">The invoked constructor throws an exception</exception>
            /// <exception cref="System.Security.SecurityException">The caller does not have the necessary code access permission</exception>
            /// <exception cref="MissingMethodException">A default constructor does not exist</exception>
            public static object ReflectionNew<TArg1>(this Type type, TArg1 arg1)
            {
                ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(TArg1) });
                if (constructor == null)
                {
                    string constructorName = string.Format("{0}.{1}({2})", type.FullName, type.Name, typeof(TArg1).Name);
                    throw new MissingMethodException(constructorName);
                }

                return constructor.Invoke(new object[] { arg1 });
            }

            /// <summary>
            /// Calls a constructor with two args of types <i>Arg1</i> and <i>Arg2</i>
            /// </summary>
            /// <typeparam name="TArg1"></typeparam>
            /// <typeparam name="TArg2"></typeparam>
            /// <param name="type"></param>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns><b>null</b> if a matching constructor can't be found</returns>
            /// <exception cref="NullReferenceException"><i>type</i> is null</exception>
            /// <exception cref="MemberAccessException">The class is abstract, or the constructor is a class initializer</exception>
            /// <exception cref="MethodAccessException">The constructor is private or protected, and the caller lacks ReflectionPermissionFlag.MemberAccess/></exception>
            /// <exception cref="TargetInvocationException">The invoked constructor throws an exception</exception>
            /// <exception cref="System.Security.SecurityException">The caller does not have the necessary code access permission</exception>
            /// <exception cref="MissingMethodException">A default constructor does not exist</exception>
            public static object ReflectionNew<TArg1, TArg2>(this Type type, TArg1 arg1, TArg2 arg2)
            {
                ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(TArg1), typeof(TArg2) });
                if (constructor == null)
                {
                    string constructorName = string.Format("{0}.{1}({2},{3})", type.FullName, type.Name, typeof(TArg1).Name, typeof(TArg2).Name);
                    throw new MissingMethodException(constructorName);
                }

                return constructor.Invoke(new object[] { arg1, arg2 });
            }


            /// <summary>
            /// Retrieves the property <i>propertyName</i> from object <i>obj</i>. 
            /// This is equivalent to calling the method get_<i>propertyName</i> on the object <i>obj</i>
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="obj"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            /// <exception cref="AmbiguousMatchException">More than one property is found with the specified name. See <see cref="Type.GetProperty(string)"/></exception>
            /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null</exception>
            /// <exception cref="MissingMemberException"><paramref name="propertyName"/> does not exist</exception>
            /// <exception cref="TargetParameterCountException">An indexed property was accessed without an index</exception>
            /// <exception cref="InvalidCastException">The result of the property access can not be cast to <i>Result</i> type</exception>
            public static TResult ReflectionGetProperty<TResult>(this object obj, string propertyName)
            {
                Type type = obj.GetType();
                PropertyInfo p = type.GetProperty(propertyName);

                if (p == null)
                {
                    throw new MissingMemberException(propertyName);
                }

                return (TResult)p.GetValue(obj);
            }

            /// <summary>
            /// Retrieves the property <i>propertyName</i> from object <i>obj</i>. 
            /// This is equivalent to calling the method get_<i>propertyName</i> on the object <i>obj</i>
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            /// <exception cref="AmbiguousMatchException">More than one property is found with the specified name. See <see cref="Type.GetProperty(string)"/></exception>
            /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null</exception>
            /// <exception cref="MissingMemberException"><paramref name="propertyName"/> does not exist</exception>
            /// <exception cref="TargetParameterCountException">An indexed property was accessed without an index</exception>
            public static object ReflectionGetProperty(this object obj, string propertyName)
            {
                return obj.ReflectionGetProperty<object>(propertyName);
            }

            /// <summary>
            /// Retrieves the static property <i>propertyName</i> from type <i>type</i>
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="type"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            /// <exception cref="AmbiguousMatchException">More than one property is found with the specified name. See <see cref="Type.GetProperty(string)"/></exception>
            /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null</exception>
            /// <exception cref="MissingMemberException"><paramref name="propertyName"/> does not exist</exception>
            /// <exception cref="TargetParameterCountException">An indexed property was accessed without an index</exception>
            public static TResult ReflectionStaticGetProperty<TResult>(this Type type, string propertyName)
            {
                PropertyInfo p = type.GetProperty(propertyName, BindingFlags.Static);
                if (p == null)
                {
                    throw new MissingMemberException(propertyName);
                }

                return (TResult)p.GetValue(null);
            }
        }
    }
}