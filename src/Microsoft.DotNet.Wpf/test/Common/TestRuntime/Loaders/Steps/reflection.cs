// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0618
using System;
using System.Collections;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Test.Utilities.Reflection
{
    [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
    internal class Utils
    {
        /// <summary>
        /// Returns an assembly from a file path
        /// </summary>
        /// <param name="assemblyPath">the path of the assembly</param>
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static Assembly LoadFrom(string assemblyPath)
        {
            return(Assembly.LoadFrom(assemblyPath));
        }

        /// <summary>
        /// Returns an assembly from a name
        /// </summary>
        /// <param name="assemblyName">the name of the assembly</param>
        internal static Assembly Load(string assemblyName)
        {
            return(Assembly.Load(assemblyName));
        }

        /// <summary>
        /// Returns an assembly code base
        /// </summary>
        /// <param name="a">the assembly</param>
        /// <returns>its code base</returns>
        internal static string GetCodeBase(Assembly a)
        {
            return (a.CodeBase);
        }

        /// <summary>
        /// Returns an instance of the given type from the given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="typeName"></param>
        /// <returns>the instance</returns>
        internal static object CreateInstance(Assembly assembly, string typeName)
        {
            return assembly.CreateInstance(typeName);
        }

        /// <summary>
        /// Get a type from the given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        internal static Type GetType(Assembly assembly, string typeName)
        {
            return assembly.GetType(typeName);
        }

        /// <summary>
        /// Invokes a method
        /// </summary>
        /// <param name="method">method to invoke</param>
        /// <param name="instance">instance that created this method</param>
        /// <param name="parameters">method parameters</param>
        /// <returns>method call's result</returns>
        internal static object Invoke(MethodInfo method, object instance, object[] parameters)
        {
            return method.Invoke(instance, parameters);
        }
    }
}

namespace Microsoft.Test.Utilities.Reflection
{
    /// <summary>
    /// AssemblyProxy class allow to easily construct types from the assemblies and call
    /// its methods pushing parameters to do so
    /// <code>
    ///    // get the assembly proxy object
    ///    AssemblyProxy ap = new AssemblyProxy();
    ///    ap.Load("DummyLibrary, Version=1.0.1490.30744, Culture=neutral, PublicKeyToken=a069dee354d95f76, ProcessorArchitecture=MSIL");
    ///
    ///    // call a static method
    ///    ap.Parameters.Add("x");
    ///    ap.Invoke("DummyLibrary.DummyClass", "Static", null);
    ///    ap.Parameters.Clear();
    ///
    ///    // call an instance method
    ///    ap.Parameters.Add(1);
    ///    ap.Parameters.Add("y");
    ///    DummyLibrary.DummyClass obj = (DummyLibrary.DummyClass)ap.Construct("DummyLibrary.DummyClass");
    ///    obj.NonStatic("z");
    ///    ap.Parameters.Clear();
    /// </code>
    /// </summary>
    [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
    internal class AssemblyProxy
    {
        /// <summary>
        /// assembly holds the assembly over which objects will be constructed and/or methods will be called
        /// </summary>
        private Assembly assembly = null;
        private ArrayList parameters = null;
        private ArrayList parametersTypes = null;

        /// <summary>
        /// ArrayList exposing the parameters that will be passed to a method
        /// After the call, it holds the parameter list with, if that's the case, out parameters modified
        /// </summary>
        /// <value>ArrayList</value>
        internal ArrayList Parameters
        {
            get
            {
                return (parameters);
            }
        }

        /// <summary>
        /// ArrayList exposing the parameters types that will be used to find a method
        /// </summary>
        /// <value>ArrayList</value>
        internal ArrayList ParametersTypes
        {
            get
            {
                return (parametersTypes);
            }
        }

        /// <summary>
        /// Creates an AssemblyProxy object
        /// </summary>
        internal AssemblyProxy()
        {
            parameters = new ArrayList();
            parametersTypes = new ArrayList();
        }

        /// <summary>
        /// Initializes the AssemblyProxy object
        /// </summary>
        /// <param name="assemblyPath">the path of the assembly whose types will be used</param>
        internal void LoadFrom(string assemblyPath)
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }

        /// <summary>
        /// Initializes the AssemblyProxy object
        /// </summary>
        /// <param name="asm">the assembly whose types will be used</param>
        internal void Load(Assembly asm)
        {
            assembly = asm;
        }

        /// <summary>
        /// Initializes the AssemblyProxy object
        /// </summary>
        /// <param name="assemblyName">the name of the assembly whose types will be used</param>
        internal void Load(string assemblyName)
        {
            assembly = Assembly.Load(assemblyName);
        }

        /// <summary>
        /// Initializes the AssemblyProxy object from a partial name
        /// </summary>
        /// <param name="assemblyName">the name of the assembly</param>
        internal void LoadWithPartialName(string assemblyName)
        {
            assembly = Assembly.LoadWithPartialName(assemblyName);
        }

        /// <summary>
        /// Invokes the constructor of the given object that matches the passed parameters.
        /// </summary>
        /// <param name="typeName">name of the type to instantiate</param>
        internal object Construct(string typeName)
        {
            // verify that the assembly was loaded
            if (assembly == null)
            {
                throw (new NullReferenceException("Load the target assembly first"));
            }

            // build the types array from de parameters array
            Type[] conTypes = new Type[parameters.Count];
            IEnumerator enumerator = parameters.GetEnumerator();

            for (int pos = 0; enumerator.MoveNext(); pos++)
            {
                conTypes[pos] = (Type)enumerator.Current.GetType();
            }

            // get the type to construct from the assembly
            Type typeToConstruct = assembly.GetType(typeName);

            // get the constructor of the type
            ConstructorInfo constInfo = typeToConstruct.GetConstructor(conTypes);

            if (constInfo == null)
            {
                return (null);
            }

            // build the object array to invoke the constructor
            object[] constParams = new object[parameters.Count];

            enumerator = parameters.GetEnumerator();
            for (int pos = 0; enumerator.MoveNext(); pos++)
            {
                constParams[pos] = (object)enumerator.Current;
            }

            // invoke the constructor
            object instanceOfType = constInfo.Invoke(constParams);

            // return the created object
            return (instanceOfType);
        }

        /// <summary>
        /// Returns a type array containing the types of the current parameters
        /// </summary>
        /// <returns>A type array corresponding to the types of the parameters</returns>
        private Type[] GetParametersTypes()
        {
            // create the array to return
            Type[] methodTypes = new Type[parameters.Count];

            // if parameters types were specified, use them
            if (parameters.Count == parametersTypes.Count)
            {
                // build the types array to invoke the method
                IEnumerator enumerator = parametersTypes.GetEnumerator();
                for (int pos = 0; enumerator.MoveNext(); pos++)
                {
                    if (enumerator.Current != null)
                    {
                        methodTypes[pos] = (Type)enumerator.Current;
                    }
                    else
                    {
                        methodTypes[pos] = typeof(object);
                    }
                }
            }
            else// parameters types were not specified, take them from the parameters
            {
                // build the types array to invoke the method
                IEnumerator enumerator = parameters.GetEnumerator();
                for (int pos = 0; enumerator.MoveNext(); pos++)
                {
                    if (enumerator.Current != null)
                    {
                        methodTypes[pos] = enumerator.Current.GetType();
                    }
                    else
                    {
                        methodTypes[pos] = typeof(object);
                    }
                }
            }

            return (methodTypes);
        }

        /// <summary>
        /// Returns an object array containing the current parameters.
        /// </summary>
        /// <returns>An object array</returns>
        private object[] GetParametersArray()
        {
            // build the array to invoke the method
            object[] methodParams = new object[parameters.Count];
            IEnumerator enumerator = parameters.GetEnumerator();

            // collect the parameters
            for (int pos = 0; enumerator.MoveNext(); pos++)
            {
                methodParams[pos] = (object)enumerator.Current;
            }

            // return the array
            return (methodParams);
        }

        /// <summary>
        /// Fills an ArrayList with the elements of an object array.
        /// </summary>
        /// <param name="objArray">The object array to take the elements from</param>
        /// <param name="arrayList">The ArrayList to put the elements into</param>
        private void ObjectArrayToArrayList(object[] objArray, ArrayList arrayList)
        {
            // copy the members, one by one
            for (int i = 0; i < objArray.Length; i++)
            {
                arrayList.Add(objArray[i]);
            }
        }

        /// <summary>
        /// Performs the invoke and refresh the parameters ArrayList with the (maybe) modified parameters
        /// </summary>
        /// <param name="objInstance">Instance of the object to be used to make the call</param>
        /// <param name="methodToInvoke">MethodInfo of the method to invoke</param>
        /// <param name="methodParams">object array of parameters</param>
        /// <returns>the returned object from the call</returns>
        private object MakeInvoke(object objInstance, MethodInfo methodToInvoke, object[] methodParams)
        {
            // invoke the method
            object ret = methodToInvoke.Invoke(objInstance, methodParams);

            // clean the parameters list
            parameters.Clear();

            // put the parameters back in the array list
            ObjectArrayToArrayList(methodParams, parameters);

            // return the result
            return (ret);
        }

        /// <summary>
        /// Calls the given method for the given object
        /// </summary>
        /// <param name="constructedObject">object that owns the method to be called</param>
        /// <param name="methodName">name of the method to be called</param>
        /// <returns>returned object from the call</returns>
        internal object Invoke(object constructedObject, string methodName)
        {
            // get parameters types
            Type[] methodTypes = GetParametersTypes();

            // get parameters array
            object[] methodParams = GetParametersArray();

            // get the method info
            BindingFlags bindingFlags = BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            Type constructedObjectType = constructedObject.GetType();
            MethodInfo methodToInvoke = constructedObjectType.GetMethod(methodName, bindingFlags, null, methodTypes, null);

            // Make the call
            object ret = MakeInvoke(constructedObject, methodToInvoke, methodParams);

            // return the result
            return (ret);
        }

        /// <summary>
        /// Calls a static method in a class
        /// </summary>
        /// <param name="typeName">Type that owns the method to be called</param>
        /// <param name="methodName">The method name to be called</param>
        /// <param name="methodOwnerInstance">The instance that created the method</param>
        /// <returns></returns>
        internal object Invoke(string typeName, string methodName, object methodOwnerInstance)
        {
            // get parameters types
            Type[] methodTypes = GetParametersTypes();

            // get parameters array
            object[] methodParams = GetParametersArray();

            // get the method
            BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreReturn | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            Type theType = assembly.GetType(typeName);
            MethodInfo methodToInvoke = theType.GetMethod(methodName, bindingFlags, null, methodTypes, null);

            // make the call
            object ret = MakeInvoke(methodOwnerInstance, methodToInvoke, methodParams);

            // return the result
            return (ret);
        }

        /// <summary>
        /// Returns an object representing an enum value
        /// </summary>
        /// <param name="name">fully qualified value name</param>
        /// <returns>the enum value object</returns>
        internal object GetEnumValue(string name)
        {
            // extract type name
            char[] dot = {'.'};
            int lastDot = name.LastIndexOfAny(dot);
            string typeName = name.Substring(0, lastDot);

            // get type
            Type t = assembly.GetType(typeName);

            // get the values for that type
            Array values = Enum.GetValues(t);

            // return appropiate value
            string valueName = name.Substring(lastDot + 1, name.Length - lastDot - 1);
            foreach (object val in values)
            {
                if (val.ToString() == valueName)
                {
                    return (val);
                }
            }

            // no object matches
            return (null);
        }
    }
}
