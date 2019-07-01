// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Xml;

namespace Microsoft.Test.Layout 
{
    
    internal class ReflectionHelper 
    {
        #region Public Static constructor wrappers
        
        public static ReflectionHelper WrapObject(object instance) {
            if(instance == null) {
                throw new ArgumentNullException("instance");
            }
            
            return new ReflectionHelper(instance);
        }
        
        public static ReflectionHelper WrapObject(object instance, string expectedType) {
            if(instance == null) 
            {
                throw new ArgumentNullException("instance");
            }
            
            if(expectedType == null) 
            {
                throw new ArgumentNullException("expectedType");
            }
            
            if(!IsOfType(instance, expectedType)) 
            {
                throw new ArgumentException("argument is not of exptected type of type " + expectedType + ": Actual type is " + instance.GetType().ToString(), "instance");
            }
            
            return new ReflectionHelper(instance, expectedType);
        }
        
        public static ReflectionHelper CreateInstance(string typeName) {
            if(typeName == null) 
            {
                throw new ArgumentNullException("typeName");
            }
            
            return new ReflectionHelper(typeName);
        }
        
        public static ReflectionHelper CreateInstance(string assemblyDllName, string typeName) {
            if(assemblyDllName == null) 
            {
                throw new ArgumentNullException("assemblyDllName");
            }
            if(typeName == null) 
            {
                throw new ArgumentNullException("typeName");
            }

            return new ReflectionHelper(assemblyDllName, typeName);
        }
        
        public static ReflectionHelper CreateInstance(string assemblyDllName, string typeName, XmlElement initParams) {
            if (assemblyDllName == null)
            {
                throw new ArgumentNullException("assemblyDllName");
            }
            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }

            return new ReflectionHelper(assemblyDllName, typeName, initParams);
        }
        
        public static ReflectionHelper WrapStatic(Type staticType) {
            if (staticType == null)
            {
                throw new ArgumentNullException("staticType");
            }
            
            ReflectionHelper rh = new ReflectionHelper();
            rh._innerType = staticType;
            rh.DefaultBindingFlags = BindingFlags.Static | BindingFlags.NonPublic;

            return rh;
        }
    
        #endregion Public Static constructor wrappers
        
        #region Constructors

        private ReflectionHelper() {
        }
        
        protected ReflectionHelper(object instance) {
            if(instance == null) {
                throw new ArgumentNullException("instance");
            }
            
            DefaultBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            InnerObject = instance;
        }
        
        protected ReflectionHelper(object instance, string expectedType) {
            if(instance == null) 
            {
                throw new ArgumentNullException("instance");
            }
            
            if(expectedType == null) 
            {
                throw new ArgumentNullException("expectedType");
            }
            
            if(!IsOfType(instance, expectedType)) 
            {
                throw new ArgumentException("argument is not of exptected type of type " + expectedType + ": Actual type is " + instance.GetType().ToString(), "instance");
            }
            
            DefaultBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            InnerObject = instance;
        }
        
        protected ReflectionHelper(string typeName): 
            this(WrapObject(Instantiate(typeName))) 
        {
        }
        
        protected ReflectionHelper(string assemblyDllName, string typeName):
            this(Instantiate(assemblyDllName, typeName)) 
        {
        }
        
        protected ReflectionHelper(string assemblyDllName, string typeName, XmlElement initParams):
            this(Instantiate(assemblyDllName, typeName, initParams))
        {
        }
        
        #endregion Constructors        
        
        public object InnerObject 
        { 
            get { return _innerObject; }
            set { 
                if(value != null) {
                    _innerType = value.GetType();
                }
                _innerObject = value; 
            }
        }
            
        public Type InnerType 
        { 
            get { return _innerType; }
        }
        
        public object GetField(string name) 
        {
            return GetField(name, DefaultBindingFlags);
        }

        public object GetField(string name, BindingFlags flags) 
        {
            return GetField(InnerType, name, flags);
        }

        public object GetField(Type type, string name) 
        {
            return GetField(type, name, DefaultBindingFlags);
        }

        public object GetField(Type type, string name, BindingFlags flags)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            //FieldInfoSW fInfo = TypeSW.Wrap(type).GetField(name, flags);
            FieldInfo fInfo = type.GetField(name, flags);
            if (fInfo == null)
            {
                throw new Exception("Field " + name + " not found on object of type " + type.ToString());
            }

            return fInfo.GetValue(InnerObject);
        }

        public void SetField(string name, object value)
        {
            SetField(name, DefaultBindingFlags, value);
        }

        public void SetField(string name, BindingFlags flags, object value)
        {
            SetField(InnerType, name, flags, value);
        }

        public void SetField(Type type, string name, object value)
        {
            SetField(type, name, DefaultBindingFlags, value);
        }

        public void SetField(Type type, string name, BindingFlags flags, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            //FieldInfoSW fInfo = TypeSW.Wrap(type).GetField(name, flags);
            FieldInfo fInfo = type.GetField(name, flags);

            if (fInfo == null)
            {
                throw new Exception("Field " + name + " not found on object of type " + type.ToString());
            }

            fInfo.SetValue(InnerObject, value);
        }
        
        public object GetProperty(string name) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            
            return GetProperty(InnerType, name, DefaultBindingFlags);
        }
        
        public object GetProperty(string name, BindingFlags flags) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            
            return GetProperty(InnerType, name, flags);
        }
        
        public object GetProperty(Type type, string name) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            
            return GetProperty(type, name, DefaultBindingFlags);
        }
            
        public object GetProperty(Type type, string name, BindingFlags flags) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            
            //PropertyInfoSW pInfo = TypeSW.Wrap(type).GetProperty(name, flags);
            PropertyInfo pInfo = type.GetProperty(name, flags);

            if(pInfo == null) {
                throw new Exception("Property " + name + " not found on object of type " + type.ToString());
            }
            
            if(!pInfo.CanRead) {
                throw new Exception("Property " + name + " on object of type " + type.ToString() + " has no getter");
            }
            
            return pInfo.GetValue(InnerObject, null);
        }
        
        public void SetProperty(string name, object value) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            SetProperty(InnerType, name, DefaultBindingFlags, value);
        }

        public void SetProperty(string name, BindingFlags flags, object value) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            SetProperty(InnerType, name, flags, value);
        }
        
        public void SetProperty(Type type, string name, object value) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }
            
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            
            SetProperty(type, name, DefaultBindingFlags, value);
        }
        
        public void SetProperty(Type type, string name, BindingFlags flags, object value) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }
            
            if(name == null) {
                throw new ArgumentNullException("name");
            }

            PropertyInfo pInfo = type.GetProperty(name, flags);
            if (pInfo == null) {
                throw new Exception("Property " + name + " not found on object of type " + type.ToString());
            }

            if (!pInfo.CanWrite) {
                throw new Exception("Property " + name + " on object of type " + type.ToString() + " has no setter");
            }

            pInfo.SetValue(InnerObject, value, null);
        }
        
        public object CallMethod(string name, params object [] args) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            
            return CallMethod(name, DefaultBindingFlags, args);
        }
        
        public object CallMethod(string name, BindingFlags flags, params object [] args) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }

            //MethodInfoSW mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
            MethodInfo mInfo = InnerType.GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);

            if (mInfo == null)
                mInfo = InnerType.GetMethod(name, flags);//mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags);
            return mInfo.Invoke(InnerObject, args);
        }
        
        public object CallMethod(MethodInfo mInfo, params object[] args)
        {
            if (mInfo == null)
            {
                throw new ArgumentNullException("mInfo");
            }
            return mInfo.Invoke(InnerObject, args);
        }

        //public object CallMethod(MethodInfoSW mInfo, params object[] args)
        //{
        //    if (mInfo == null)
        //    {
        //        throw new ArgumentNullException("mInfo");
        //    }
        //    return mInfo.Invoke(InnerObject, args);
        //}


        public MethodInfo GetMethodInfo(string name)
        {
            if (name == null)
                return null;

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo mInfo = InnerType.GetMethod(name, flags);
            if (mInfo != null) return mInfo;

            flags = BindingFlags.NonPublic | BindingFlags.Instance;
            mInfo = InnerType.GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
            if (mInfo != null) return mInfo;

            flags = BindingFlags.NonPublic | BindingFlags.Static;
            mInfo = InnerType.GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
            if (mInfo != null) return mInfo;

            flags = BindingFlags.Public | BindingFlags.Instance;
            mInfo = InnerType.GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
            if (mInfo != null) return mInfo;

            flags = BindingFlags.Public | BindingFlags.Static;
            mInfo = InnerType.GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
            if (mInfo != null) return mInfo;

            flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy;
            mInfo = InnerType.GetMethod(name, flags, null, new Type[] { typeof(TextPointer), typeof(LogicalDirection) }, null);
            if (mInfo != null) return mInfo;

            flags = BindingFlags.NonPublic | BindingFlags.Instance;
            mInfo = InnerType.GetMethod(name, flags, null, new Type[] { typeof(Block), typeof(Block) }, null);
            if (mInfo != null) return mInfo;

            return null;
        }

        //public MethodInfoSW GetMethodInfoSW(string name)
        //{
        //    if (name == null)
        //        return null;

        //    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        //    MethodInfoSW mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags);
        //    if (mInfo != null) return mInfo;

        //    flags = BindingFlags.NonPublic | BindingFlags.Instance;
        //    mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
        //    if (mInfo != null) return mInfo;

        //    flags = BindingFlags.NonPublic | BindingFlags.Static;
        //    mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
        //    if (mInfo != null) return mInfo;

        //    flags = BindingFlags.Public | BindingFlags.Instance;
        //    mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
        //    if (mInfo != null) return mInfo;

        //    flags = BindingFlags.Public | BindingFlags.Static;
        //    mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, new Type[] { typeof(FrameworkElement) }, null);
        //    if (mInfo != null) return mInfo;

        //    flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy;
        //    mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, new Type[] { typeof(TextPointer), typeof(LogicalDirection) }, null);
        //    if (mInfo != null) return mInfo;

        //    flags = BindingFlags.NonPublic | BindingFlags.Instance;
        //    mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, new Type[] { typeof(Block), typeof(Block) }, null);
        //    if (mInfo != null) return mInfo;

        //    return null;
        //}

        public MethodInfo GetMethodInfo(string name, Type[] typeArray)
        {
            if (name == null)
                return null;

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo mInfo = InnerType.GetMethod(name, flags, null, typeArray, null);
            if (mInfo != null) return mInfo;

            return null;
        }

        //public MethodInfoSW GetMethodInfoSW(string name, Type[] typeArray)
        //{
        //    if (name == null)
        //        return null;

        //    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        //    MethodInfoSW mInfo = TypeSW.Wrap(InnerType).GetMethod(name, flags, null, typeArray, null);
        //    if (mInfo != null) return mInfo;

        //    return null;
        //}

        static bool IsOfType(object o, string expectedType) {
            if(o == null) {
                throw new ArgumentNullException("o");
            }
            
            if(expectedType == null) {
                throw new ArgumentNullException("expectedType");
            }
            
            Type type = o.GetType();
            string typeName = type.ToString();
            
            if(0 == String.Compare(typeName, expectedType)) {
                return true;
            }
            
            foreach(Type interfaceType in type.GetInterfaces()) {
                if(0 == String.Compare(interfaceType.ToString(), expectedType)) {
                    return true;
                }
            }
            
            while(type != typeof(object)) {
                type = type.BaseType;
                if(0 == String.Compare(type.ToString(), expectedType)) {
                    return true;
                }
            }
            
            return false;
        }
        
        object _innerObject;
        Type _innerType;
        public BindingFlags DefaultBindingFlags;
        
        static object Instantiate(string typeName) {
            if(typeName == null) {
                throw new ArgumentNullException("typeName");
            }
            
            return Instantiate(null, typeName);
        }
        
        public static object Instantiate(string assemblyName, string typeName) 
        {
            if (assemblyName == null) {
                throw new ArgumentNullException("assemblyName");
            }

            if (typeName == null) {
                throw new ArgumentNullException("typeName");
            }
            
            object o = null;
            Assembly assembly = null;
            Type type = null;
            string assemblyFullPath = null;

            if (assemblyName == null || assemblyName == String.Empty) {                
                //assemblyname was not specified try and load the type without an assembly
                type = Type.GetType(typeName);
                if (type == null) {
                    throw new ApplicationException("The type [" + typeName + "] could not found. Perhaps the type is mis-spelled or an assembly name should be specified?");
                }

                assembly = type.Assembly;
                assemblyFullPath = String.Empty;
            }
            else {
                //look in referenced assemblies for the assembly and load the type
                foreach (AssemblyName name in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                {                    
                    //TODO improve perf by comparing name to assemblyName
                    //if(!assemblyname.FullName.Contains(name)) {
                    // continue;
                    //}
                    
                    assembly = Assembly.Load(name);
                    try {
                        type = assembly.GetType(typeName, false, false); //this always throws even when told not to.  BUG???
                    }
                    catch(Exception e) {
                        Exception inner = e.InnerException; //this supresses 'variable declared but never used' warnings from compiler
                        type = null;
                    }
                    
                    if (type != null) {
                        break;
                    }
                }

                if (assembly == null || type == null) {
                    //try and load the assembly from a file and then load the type from the assembly
                    assemblyFullPath = Path.GetFullPath(assemblyName);
                    assembly = Assembly.LoadFile(assemblyFullPath);
                    if (assembly == null) {
                        throw new ApplicationException("Could not load assembly " + assemblyFullPath);
                    }

                    type = assembly.GetType(typeName, true, false);
                    if (type == null) {
                        throw new ApplicationException("The type [" + typeName + "] could not found in the assembly [" + assemblyFullPath + "]. Perhaps the type or assembly is mis-spelled?");
                    }
                }
            }

            if (assembly == null) {
                throw new ApplicationException("Could not load assembly " + assemblyFullPath);
            }

            if (type == null) {
                throw new ApplicationException("The type [" + typeName + "] could not found.");
            }

            if (type.IsAbstract) {
                throw new ApplicationException("The type [" + typeName + "] is abstract and cannot be instantiated");
            }

            o = assembly.CreateInstance(typeName);
            if (o == null) {
                throw new ApplicationException("The type [" + typeName + "] could not be instantiated from the assembly [" + assemblyFullPath + "]");
            }

            return o;
        }
 
        public static object Instantiate(string assemblyName, string typeName, XmlElement initParams) {
            if (assemblyName == null) {
                throw new ArgumentNullException("assemblyName");
            }

            if (typeName == null) {
                throw new ArgumentNullException("typeName");
            }
            
            object o = null;
            Assembly assembly = null;
            Type type = null;
            string assemblyFullPath = null;

            if (assemblyName == String.Empty) {                
                //assemblyname was not specified try and load the type without an assembly
                type = Type.GetType(typeName);
                if (type != null) {
                    assembly = type.Assembly;
                }
                else {
                    //throw new ApplicationException("The type [" + typeName + "] could not found. Perhaps the type is mis-spelled or an assembly name should be specified?");
                    //look in referenced assemblies for the assembly and load the type
                    foreach (AssemblyName name in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                    {                    
                        //TODO improve perf by comparing name to assemblyName
                        //if(!assemblyname.FullName.Contains(name)) {
                        // continue;
                        //}
                        
                        assembly = Assembly.Load(name);
                        try {
                            type = assembly.GetType(typeName, false, false); //this always throws even when told not to.  BUG???
                        }
                        catch(Exception e) {
                            Exception inner = e.InnerException; //this supresses 'variable declared but never used' warnings from compiler
                            type = null;
                        }
                        
                        if (type != null) {
                            break;
                        }
                    }
                }
                
                if (type == null) {
                    throw new ApplicationException("The type [" + typeName + "] could not found.");
                }
                
                //TODO: get real assembly path
                assemblyFullPath = "Unkown";
            }
            else {
                //try and load the assembly from a file and then load the type from the assembly
                assemblyFullPath = Path.GetFullPath(assemblyName);
                try {
                    assembly = Assembly.LoadFile(assemblyFullPath);
                }
                catch (Exception fnfe) {
                    Exception inner = fnfe.InnerException;
                    Console.WriteLine(assemblyFullPath);
                    throw;
                }
                
                if (assembly == null) {
                    throw new FileNotFoundException("Could not load assembly " + assemblyFullPath);
                }

                type = assembly.GetType(typeName, true, false);
                if (type == null) {
                    throw new ApplicationException("The type [" + typeName + "] could not found in the assembly [" + assemblyFullPath + "]. Perhaps the type or assembly is mis-spelled?");
                }
            }

            if (type.IsAbstract) {
                throw new ApplicationException("The type [" + typeName + "] is abstract and cannot be instantiated");
            }

            try
            {
                if (initParams != null)
                {
                    object[] paramArray = new object[1];
                    paramArray[0] = initParams;
                    o = assembly.CreateInstance(typeName, true, BindingFlags.Instance | BindingFlags.Public, null, paramArray, null, null);
                }
                else
                    o = assembly.CreateInstance(typeName);
            }
            catch (Exception e)
            {
               Console.WriteLine(e.Message);
            }
            if (o == null) {
                throw new ApplicationException("The type [" + typeName + "] could not be instantiated from the assembly [" + assemblyFullPath + "]");
            }

            return o;
        }
        
        public static Type GetTypeFromName(string typeName)
        {
            Type t = null;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach(Assembly assembly in assemblies)
            {
                t = assembly.GetType(typeName);
                
                if (t != null)
                {
                    break;
                }
            }

            if(t == null) {
                throw new ApplicationException(String.Format("Could not get type '{0}' after searching the current appdomains loaded assemblies", typeName));
            }
            return t;
        }
 
    }
}
