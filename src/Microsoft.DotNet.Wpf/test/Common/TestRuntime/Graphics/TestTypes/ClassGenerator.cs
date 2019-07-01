// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

using MethodBody = System.Reflection.MethodBody;

#if !STANDALONE_BUILD
using TrustedMethodBase = Microsoft.Test.Security.Wrappers.MethodBaseSW;
using TrustedMethodInfo = Microsoft.Test.Security.Wrappers.MethodInfoSW;
using TrustedPropertyInfo = Microsoft.Test.Security.Wrappers.PropertyInfoSW;
using TrustedParameterInfo = Microsoft.Test.Security.Wrappers.ParameterInfoSW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedMethodBase = System.Reflection.MethodBase;
using TrustedMethodInfo = System.Reflection.MethodInfo;
using TrustedPropertyInfo = System.Reflection.PropertyInfo;
using TrustedParameterInfo = System.Reflection.ParameterInfo;
using TrustedType = System.Type;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{    
    /// <summary>
    /// Generate a class that derives from another
    /// </summary>
    public sealed class ClassGenerator : CodeGenerator
    {
        /// <summary/>
        [System.CLSCompliant(false)]
        public ClassGenerator(TrustedType classToInherit)
        {
            this.type = classToInherit;
            this.declarationsGenerator = new DeclarationsGenerator(classToInherit, newClassName);
        }

        /// <summary>
        /// Generated code that tests if inheritance is possible with the current className
        /// </summary>
        public override string GenerateCode()
        {
            return Indent(
                    "public class " + newClassName + " : " + type.Name + "\n" +
                    "{\n" +
                    Indent(declarationsGenerator.GenerateCode()) +
                    "}\n");
        }

        private TrustedType type;
        private DeclarationsGenerator declarationsGenerator;

        private const string newClassName = "GeneratedClass";        

        private class DeclarationsGenerator
        {
            public DeclarationsGenerator(TrustedType type, string generatedClassName)
            {
                ArrayList list = new ArrayList();

                list.Add(new ConstructorGenerator(type, generatedClassName));

                if (type.IsAbstract)
                {
                    TrustedMethodInfo[] methodInfos = type.GetMethods(flags);
                    if (methodInfos != null)
                    {
                        foreach (TrustedMethodInfo method in methodInfos)
                        {
                            // PropertyGenerators will be created in the next step.  Don't add them here.
                            if (!method.Name.Contains("get_") && !method.Name.Contains("set_") &&
                                MethodNeedsOverride(method, type))
                            {
                                list.Add(new MethodGenerator(method));
                            }
                        }
                    }

                    TrustedPropertyInfo[] properties = type.GetProperties(flags);
                    if (properties != null)
                    {
                        foreach (TrustedPropertyInfo property in properties)
                        {
                            TrustedMethodInfo get = property.GetGetMethod();
                            TrustedMethodInfo set = property.GetSetMethod();

                            if (MethodNeedsOverride(get, type) || MethodNeedsOverride(set, type))
                            {
                                list.Add(new PropertyGenerator(property));
                            }
                        }
                    }
                }

                methods = new MethodGeneratorBase[list.Count];
                list.CopyTo(methods);
            }

            public string GenerateCode()
            {
                string code = string.Empty;

                foreach (MethodGeneratorBase method in methods)
                {
                    code += method.GenerateCode();
                }

                return code;
            }

            private bool MethodNeedsOverride(TrustedMethodBase method, TrustedType type)
            {
                // Property code generator will sometimes pass in null
                if (method == null)
                {
                    return false;
                }
                return method.IsAbstract && (method.IsPublic || method.IsFamily) && !IsOverridden(method, type);
            }

            /// <summary>
            ///  Walk up the inheritance tree until we find an override
            /// or the class where the abstract method was defined
            /// </summary>
            private bool IsOverridden(TrustedMethodBase method, TrustedType type)
            {
                if (method.DeclaringType == type)
                {
                    return false;
                }

                TrustedMethodInfo[] methods = type.GetMethods(flags);
                if (methods != null)
                {
                    foreach (TrustedMethodInfo info in methods)
                    {
                        if (MethodSignaturesMatch(info, method))
                        {
                            MethodBody body = info.GetMethodBody();
                            if (body != null)
                            {
                                return true;
                            }
                        }
                    }
                }
                return IsOverridden(method, type.BaseType);
            }

            private bool MethodSignaturesMatch(TrustedMethodBase method1, TrustedMethodBase method2)
            {
                if (method1.Name == method2.Name)
                {
                    TrustedParameterInfo[] params1 = method1.GetParameters();
                    TrustedParameterInfo[] params2 = method2.GetParameters();

                    if (params1.Length == params2.Length)
                    {
                        bool paramsMatch = true;
                        for (int n = 0; n < params1.Length; n++)
                        {
                            if (params1[n].ParameterType != params2[n].ParameterType)
                            {
                                paramsMatch = false;
                            }
                        }
                        return paramsMatch;
                    }
                }
                return false;
            }

            private MethodGeneratorBase[] methods;
        }
    }
}
