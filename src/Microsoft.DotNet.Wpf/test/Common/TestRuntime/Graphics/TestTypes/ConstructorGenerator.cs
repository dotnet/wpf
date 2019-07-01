// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if !STANDALONE_BUILD
using TrustedConstructorInfo = Microsoft.Test.Security.Wrappers.ConstructorInfoSW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedConstructorInfo = System.Reflection.ConstructorInfo;
using TrustedType = System.Type;
#endif


namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Generate a constructor for a derived type
    /// </summary>
    public sealed class ConstructorGenerator : MethodGeneratorBase
    {
        /// <summary/>
        [System.CLSCompliant(false)]
        public ConstructorGenerator(TrustedType typeToInherit, string className)
            : base(GetBestConstructor(typeToInherit))
        {
            this.type = typeToInherit;
            this.className = className;
        }

        /// <summary/>
        protected override string MethodSignature
        {
            get
            {
                string code = string.Empty;

                if (Constructor != null)
                {
                    string methodParams = Parameters;
                    code += Modifiers + className + "(" + methodParams + ")";

                    if (methodParams.Length > 0)
                    {
                        string callBaseConstructor = string.Empty;

                        // All Method Parameters are type/name pairs separated by commas
                        string[] pair = methodParams.Split(',');
                        foreach (string s in pair)
                        {
                            // type/name pairs are separated by a single space
                            callBaseConstructor += s.Trim().Split(' ')[1] + ", ";
                        }

                        // remove trailing comma and whitespace
                        callBaseConstructor = callBaseConstructor.Substring(0, callBaseConstructor.Length - 2);
                        code += " : base( " + callBaseConstructor + " )";
                    }
                    code += "\n";
                }
                else
                {
                    code += "public " + className + "()\n";
                }

                return code;
            }
        }

        /// <summary/>
        protected override string MethodBody
        {
            get
            {
                return "{\n" +
                        "}\n";
            }
        }

        private TrustedConstructorInfo Constructor { get { return (TrustedConstructorInfo)methodBase; } }

        /// <summary>
        /// Iterate through the constructors available and choose the best one.
        /// Return null if the type is a struct or there are no "good" constructors.
        /// </summary>
        private static TrustedConstructorInfo GetBestConstructor(TrustedType type)
        {
            TrustedConstructorInfo[] constructors = type.GetConstructors(flags);
            TrustedConstructorInfo bestOverride = null;

            // We cheat with value types and return null
            // Reason:
            //      Value types don't list the default constructor as theirs.
            //  So rather than override a multiparameter constructor, we interpret a
            //  null return value from this function as having a default constructor.

            if (constructors.Length > 0 && !type.IsValueType)
            {
                foreach (TrustedConstructorInfo constructor in constructors)
                {
                    // Rules for choosing best constructor:
                    //      1- The constructor must be public or protected.
                    //      2- We prefer overriding public constructors to protected ones.
                    //      3- We prefer overriding the constructor with the fewest parameters.

                    if (constructor.IsPublic)
                    {
                        if (bestOverride == null || bestOverride.IsFamily)
                        {
                            bestOverride = constructor;
                            continue;
                        }
                    }
                    else if (constructor.IsFamily)
                    {
                        if (bestOverride == null)
                        {
                            bestOverride = constructor;
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    if (constructor.GetParameters().Length < bestOverride.GetParameters().Length)
                    {
                        bestOverride = constructor;
                    }
                }
            }

            return bestOverride;
        }

        private TrustedType type;
        private string className;
    }
}
