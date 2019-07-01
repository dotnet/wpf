// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
#if TESTBUILD_CLR20
// These includes are for the different Assembly.LoadFrom() overload used to make CLR 2.0 CAS policy work
using System.Security;
using System.Security.Policy;
#endif
using Microsoft.Test.FaultInjection.Constants;

namespace Microsoft.Test.FaultInjection.SignatureParsing
{
    /// <summary>
    /// Parse string expression into value
    /// </summary>
    internal static class Expression
    {
        #region Private Data

        private static Regex isStruct = new Regex(@"\(\x20*(?<StructName>.[\w\.<>\[\]]+)\x20*(@\x20*(?<Assembly>[\w\.:\\/=,'\x20]*))?\)\x20*(?<Parameter>.+)", RegexOptions.CultureInvariant);
        private static Regex isString = new Regex(@"'(?<String>.*)'", RegexOptions.CultureInvariant);
        private static Regex isObject = new Regex(@"(?<ClassName>[\w\.<>\[\]]+)\x20*(\(\x20*(?<Parameters>.*)\x20*\))?(\x20*@\x20*(?<Assembly>[\w\.:\\/=,'\x20]+)\x20*)?", RegexOptions.CultureInvariant);
        private static Regex isArray = new Regex(@"(?<ClassName>[\w\.<>\[\]]+)\x20*\[\x20*\]\x20*{\x20*(?<Parameters>.*)\x20*}(\x20*@\x20*(?<Assembly>[\w\.:\\/=,'\x20]+))?\x20*", RegexOptions.CultureInvariant);
       
        #endregion

        #region Public Members

        /// <summary>
        /// Parse expression that represents an reference object
        /// </summary>
        /// <param name="className">class name</param>
        /// <param name="parameterString">constructor parameter string</param>
        /// <param name="assembly">assembly, optional</param>
        /// <returns>the object</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811")]
        public static object ObjectExpression(string className, string parameterString, string assembly)
        {
            Type type;
            return ObjectExpression(className, parameterString, assembly, null, out type);
        }

        /// <summary>
        /// Parse expression from string, also return expression value type
        /// </summary>
        /// <param name="fullName">expression string</param>
        /// <param name="type">return value type</param>
        /// <returns>return value</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811")]
        public static object GeneralExpression(string fullName, out Type type)
        {
            return GeneralExpression(fullName, null, out type);
        }

        /// <summary>
        /// Parse expression from string
        /// </summary>
        /// <param name="FullName">expression string</param>
        /// <returns>return value</returns>
        public static object GeneralExpression(string FullName)
        {
            Type type;
            return GeneralExpression(FullName, null, out type);
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Load assembly given its name with path
        /// </summary>
        /// <param name="assemblyName">assembly name</param>
        /// <returns>the assembly</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001")] //Assembly.LoadFrom
        private static Assembly LoadAssembly(string assemblyName)
        {
            //consider the form of [assembly name]
            assemblyName = assemblyName.Trim();
            Match matchString = isString.Match(assemblyName);

            //is string
            if (matchString.Success && matchString.Groups[0].Length == assemblyName.Length)
            {
                Type outType;
                assemblyName = (string)StringExpression(matchString.Groups["String"].ToString(), out outType);
            }

            //load assembly using Assembly.Load(FullName)
            Assembly targetAssembly;
            try
            {
                targetAssembly = Assembly.Load(assemblyName);
            }
            catch (ArgumentNullException)
            {
                targetAssembly = null;
            }
            catch (ArgumentException)
            {
                targetAssembly = null;
            }
            catch (FileNotFoundException)
            {
                targetAssembly = null;
            }
            catch (FileLoadException)
            {
                targetAssembly = null;
            }
            catch (BadImageFormatException)
            {
                targetAssembly = null;
            }
            if (targetAssembly != null)
            {
                return targetAssembly;
            }

            //load assembly using Assembly.Load(AssemblyName)
            AssemblyName assemblyRef = new AssemblyName();
            assemblyRef.Name = Path.GetFileNameWithoutExtension(assemblyName);
            assemblyRef.CodeBase = Path.GetDirectoryName(assemblyName);
            if (string.IsNullOrEmpty(assemblyRef.CodeBase))
            {
                assemblyRef.CodeBase = Environment.CurrentDirectory;
            }

            try
            {
                targetAssembly = Assembly.Load(assemblyRef);
            }
            catch (FileNotFoundException)
            {
                targetAssembly = null;
            }
            catch (FileLoadException)
            {
                targetAssembly = null;
            }
            catch (BadImageFormatException)
            {
                targetAssembly = null;
            }
            if (targetAssembly != null)
            {
                return targetAssembly;
            }

            //use Assembly.LoadFrom(string) if Load(AssemblyName)failed

#if TESTBUILD_CLR20
            Evidence evidence = new Evidence();
            evidence.AddHost(new Zone(SecurityZone.MyComputer));
            targetAssembly = Assembly.LoadFrom(assemblyName, evidence);
#endif
#if TESTBUILD_CLR40
            targetAssembly = Assembly.LoadFrom(assemblyName);
#endif
            if (targetAssembly == null)
            {
                throw new FaultInjectionException(string.Format(CultureInfo.CurrentCulture, XmlErrorMessages.TargetAssemblyNotFound, assemblyName));
            }
            return targetAssembly;
        }

        /// <summary>
        /// Get type from current appDomain or specific assembly, supporting nested class names
        /// </summary>
        /// <param name="typeName">type name</param>
        /// <param name="assemblyName">assembly file path</param>
        /// <param name="alternativeAssemblyName">alternative assembly name</param>
        /// <returns>return type</returns>
        private static Type GetType(string typeName, string assemblyName, string alternativeAssemblyName)
        {
            //get assemblies
            List<Assembly> assemblyList = new List<Assembly>();
            if (!string.IsNullOrEmpty(assemblyName))
            {
                assemblyList.Add(LoadAssembly(assemblyName));
            }
            else
            {
                if (!string.IsNullOrEmpty(alternativeAssemblyName))
                {
                    assemblyList.Add(LoadAssembly(alternativeAssemblyName));
                }

                //get type from current appDomain
                assemblyList.AddRange(AppDomain.CurrentDomain.GetAssemblies());
            }
            Assembly[] assemblies = assemblyList.ToArray();

            //Trim white spaces
            typeName = typeName.Trim();

            //Change known buitin type with full name
            string fullName = BuiltInTypeHelper.AliasToFullName(typeName);
            if (fullName == null)
            {
                fullName = typeName;
            }

            Type type = null;
            foreach (Assembly assembly in assemblies)
            {
                string reflectionTypeName = fullName;
                do
                {
                    type = assembly.GetType(reflectionTypeName);
                    if (type != null)
                    {
                        break;
                    }
                    int lastPeriod = reflectionTypeName.LastIndexOf('.');
                    if (lastPeriod < 0)
                    {
                        break;
                    }
                    reflectionTypeName = reflectionTypeName.Substring(0, lastPeriod) + '+' + reflectionTypeName.Substring(lastPeriod + 1, reflectionTypeName.Length - lastPeriod - 1);
                } while (true);
                if (type != null)
                {
                    break;
                }
            }
            if (type == null)
            {
                if (typeName.StartsWith(EngineInfo.NameSpace, StringComparison.Ordinal))
                {
                    typeName = typeName.Substring(EngineInfo.NameSpace.Length + ".".Length, typeName.Length - EngineInfo.NameSpace.Length - ".".Length);
                }
                throw new FaultInjectionException(string.Format(CultureInfo.CurrentCulture, XmlErrorMessages.TargetTypeNotFound, typeName));
            }
            return type;
        }

        /// <summary>
        /// String type expression
        /// </summary>
        /// <param name="stringExpression">string</param>
        /// <param name="type">return value type</param>
        /// <returns>return value</returns>
        private static object StringExpression(string stringExpression, out Type type)
        {
            type = typeof(string);
            return stringExpression.Replace("''", "'");
        }

        /// <summary>
        /// Struct type expression as well as null object expression
        /// </summary>
        /// <param name="structName">type name</param>
        /// <param name="parameter">parameter</param>
        /// <param name="assembly">assembly, optional</param>
        /// <param name="alternativeAssembly">alternative assembly, otional</param>
        /// <param name="type">return value type</param>
        /// <returns>return value</returns>
        private static object StructExpression(string structName, string parameter, string assembly, string alternativeAssembly, out Type type)
        {
            //get type
            type = GetType(structName, assembly, alternativeAssembly);

            if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    //Invoke enum�s Parse
                    return Enum.Parse(type, parameter);
                }
                else
                {
                    //Invoke Parse
                    MethodInfo parser = type.GetMethod("Parse", new Type[] { typeof(string) });
                    if (parser == null)
                    {
                        throw new FaultInjectionException(string.Format(CultureInfo.CurrentCulture, XmlErrorMessages.NoParser, structName));
                    }
                    return parser.Invoke(null, new object[] { parameter });
                }
            }
            else
            {
                //Return null for reference type, negleting parameter
                return null;
            }
        }

        /// <summary>
        /// Parameter list splitter, utility
        /// </summary>
        /// <param name="parameters">the whole string that represents parameter list</param>
        /// <returns>parameter list</returns>
        private static string[] ParameterListSplitter(string parameters)
        {
            parameters = parameters.Trim();
            if (string.IsNullOrEmpty(parameters)) return null;
            List<string> parameterList = new List<string>();
            StringBuilder currentParameter = new StringBuilder();
            int bracket = 0;
            int sharpBracket = 0;
            int bigBracket = 0;
            int squaredBracket = 0;
            bool quote = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                char c = parameters[i];
                if (c == '(')
                {
                    bracket++;
                }
                else if (c == ')')
                {
                    bracket--;
                }
                else if (c == '<')
                {
                    sharpBracket++;
                }
                else if (c == '>')
                {
                    sharpBracket--;
                }
                else if (c == '{')
                {
                    bigBracket++;
                }
                else if (c == '}')
                {
                    bigBracket--;
                }
                else if (c == '[')
                {
                    squaredBracket++;
                }
                else if (c == ']')
                {
                    squaredBracket--;
                }
                else if (c == '\'')
                {
                    if (i + 1 >= parameters.Length || parameters[i + 1] != '\'')
                    {
                        quote = !quote;
                    }
                }
                //only comma out of (),<>,{} is valid root level separater
                if (c == ',' && bracket == 0 && sharpBracket == 0 && bigBracket == 0 && squaredBracket == 0 && quote)
                {
                    parameterList.Add(currentParameter.ToString());
                    currentParameter = new StringBuilder();
                }
                else
                {
                    currentParameter.Append(c);
                }
            }
            parameterList.Add(currentParameter.ToString());
            return parameterList.ToArray();
        }

        /// <summary>
        /// Parse expression that represents an reference object
        /// </summary>
        /// <param name="className">class name</param>
        /// <param name="parameterString">constructor parameter string</param>
        /// <param name="assembly">assembly, optionaly</param>
        /// <param name="alternativeAssembly">alternative assembly, optional</param>
        /// <param name="type">object type</param>
        /// <returns>the object</returns>
        private static object ObjectExpression(string className, string parameterString, string assembly, string alternativeAssembly, out Type type)
        {
            //get type
            type = GetType(className, assembly, alternativeAssembly);

            //merge alternative assembly name
            if (string.IsNullOrEmpty(assembly))
            {
                assembly = alternativeAssembly;
            }

            //get constructor parameters
            string[] parameters = ParameterListSplitter(parameterString);
            object[] realParameters;
            Type[] parameterTypes;
            if (parameters == null || parameters.Length == 0)
            {
                realParameters = null;
                parameterTypes = new Type[0];
            }
            else
            {
                //make parameters
                List<object> realParameterList = new List<object>();
                List<Type> parameterTypeList = new List<Type>();
                foreach (string parameter in parameters)
                {
                    Type parameterType;
                    object realParameter = GeneralExpression(parameter, assembly, out parameterType);
                    realParameterList.Add(realParameter);
                    parameterTypeList.Add(parameterType);
                }
                realParameters = realParameterList.ToArray();
                parameterTypes = parameterTypeList.ToArray();
            }

            //get constructor
            ConstructorInfo constructor = type.GetConstructor(parameterTypes);
            if (constructor == null)
            {
                throw new FaultInjectionException(string.Format(CultureInfo.CurrentCulture, XmlErrorMessages.ConstructorNotFound, className));
            }

            //new object
            return constructor.Invoke(realParameters);
        }

        /// <summary>
        /// Parse expression that represents an array
        /// </summary>
        /// <param name="className">array type</param>
        /// <param name="parameterString">array item list</param>
        /// <param name="assembly">assembly, optional</param>
        /// <param name="alternativeAssembly">alternative assembly, optional</param>
        /// <param name="type">array type</param>
        /// <returns>return value</returns>
        private static object ArrayExpression(string className, string parameterString, string assembly, string alternativeAssembly, out Type type)
        {
            //get type
            Type itemType = GetType(className, assembly, alternativeAssembly);

            //merge alternative assembly name
            if (string.IsNullOrEmpty(assembly))
            {
                assembly = alternativeAssembly;
            }

            //get array list
            string[] parameters = ParameterListSplitter(parameterString);
            Array returnValue;
            type = itemType.MakeArrayType();
            if (parameters == null || parameters.Length == 0)
            {
                returnValue = Array.CreateInstance(itemType, 0);
            }
            else
            {
                returnValue = Array.CreateInstance(itemType, parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    string parameter = parameters[i];
                    Type parameterType;
                    object realParameter = GeneralExpression(parameter, assembly, out parameterType);
                    if (!(parameterType.Equals(itemType) || parameterType.IsSubclassOf(itemType)))
                    {
                        throw new FaultInjectionException(XmlErrorMessages.ArrayItemTypeError);
                    }
                    returnValue.SetValue(realParameter, i);
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Parse expression from string
        /// </summary>
        /// <param name="fullName">expression string</param>
        /// <param name="alternativeAssembly">alternative assembly name</param>
        /// <param name="type">return value type</param>
        /// <returns>return value</returns>
        private static object GeneralExpression(string fullName, string alternativeAssembly, out Type type)
        {
            //Trim white space and unwanted char
            fullName = fullName.Trim().Replace("\n", string.Empty);
            fullName = fullName.Replace("\r", string.Empty);
            fullName = fullName.Replace("\t", string.Empty);

            //does not support null object without type information
            /*if (string.Compare(FullName, "null") == 0)
            {
                return null;
            }*/
            Match matchString = isString.Match(fullName);
            Match matchStruct = isStruct.Match(fullName);
            Match matchObject = isObject.Match(fullName);
            Match matchArray = isArray.Match(fullName);

            //is string
            if (matchString.Success && matchString.Groups[0].Length == fullName.Length)
            {
                return StringExpression(matchString.Groups["String"].ToString(), out type);
            }

            //is struct or null object
            if (matchStruct.Success && matchStruct.Groups[0].Length == fullName.Length)
            {
                return StructExpression(matchStruct.Groups["StructName"].ToString(), matchStruct.Groups["Parameter"].ToString(), matchStruct.Groups["Assembly"].ToString(), alternativeAssembly, out type);
            }

            //is object
            else if (matchObject.Success && matchObject.Groups[0].Length == fullName.Length)
            {
                return ObjectExpression(matchObject.Groups["ClassName"].ToString(), matchObject.Groups["Parameters"].ToString(), matchObject.Groups["Assembly"].ToString(), alternativeAssembly, out type);
            }

            //is array
            else if (matchArray.Success && matchArray.Groups[0].Length == fullName.Length)
            {
                return ArrayExpression(matchArray.Groups["ClassName"].ToString(), matchArray.Groups["Parameters"].ToString(), matchArray.Groups["Assembly"].ToString(), alternativeAssembly, out type);
            }

            //format error
            else
            {
                throw new FaultInjectionException(XmlErrorMessages.ExpressionFormatError);
            }
        }

        #endregion

        
    }
}

