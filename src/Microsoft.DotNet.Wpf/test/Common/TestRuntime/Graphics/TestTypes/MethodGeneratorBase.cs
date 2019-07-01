// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if !STANDALONE_BUILD
using TrustedMethodBase = Microsoft.Test.Security.Wrappers.MethodBaseSW;
using TrustedMethodInfo = Microsoft.Test.Security.Wrappers.MethodInfoSW;
using TrustedParameterInfo = Microsoft.Test.Security.Wrappers.ParameterInfoSW;
#else
using TrustedMethodBase = System.Reflection.MethodBase;
using TrustedMethodInfo = System.Reflection.MethodInfo;
using TrustedParameterInfo = System.Reflection.ParameterInfo;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary/>
    public abstract class MethodGeneratorBase : CodeGenerator
    {
        /// <summary/>
        [System.CLSCompliant(false)]
        protected MethodGeneratorBase(TrustedMethodBase method)
        {
            this.methodBase = method;
        }


        /// <summary/>
        public override string GenerateCode()
        {
            return MethodSignature + MethodBody;
        }

        /// <summary/>
        protected abstract string MethodSignature { get; }
        /// <summary/>
        protected abstract string MethodBody { get; }

        /// <summary/>
        protected string Parameters
        {
            get
            {
                string parameters = string.Empty;

                TrustedParameterInfo[] methodParameters = methodBase.GetParameters();
                if (methodParameters != null && methodParameters.Length > 0)
                {
                    foreach (TrustedParameterInfo methodParameter in methodParameters)
                    {
                        parameters += CleanParameterType(methodParameter) + " " + methodParameter.Name + ", ";
                    }

                    // Remove trailing comma and whitespace
                    parameters = parameters.Substring(0, parameters.Length - 2);
                }
                return parameters;
            }
        }

        private string CleanParameterType(TrustedParameterInfo parameter)
        {
            // .NET assemblies store some Types in a way that cannot be automatically recompiled
            // Fix these types so that we can compile them.

            // Nested classes are represented with '+' but won't compile that way.
            string param = parameter.ParameterType.ToString().Replace('+', '.');

            // Generics are stored in a very wacky way.
            if (param.Contains("`1["))
            {
                param = param.Replace("`1[", "<");
                param = param.Replace(']', '>');
            }
            return param;
        }

        /// <summary>
        /// Create the part of the signature with the public/protected and/or static modifiers
        /// </summary>
        protected string Modifiers
        {
            get
            {
                string result = string.Empty;

                if (methodBase.IsStatic)
                {
                    result += "static ";
                }

                if (methodBase.IsPublic)
                {
                    result += "public ";
                }
                else if (methodBase.IsFamily)
                {
                    result += "protected ";
                }

                return result;
            }
        }

        /// <summary/>
        [System.CLSCompliant(false)]
        protected TrustedMethodBase methodBase;
    }
}
