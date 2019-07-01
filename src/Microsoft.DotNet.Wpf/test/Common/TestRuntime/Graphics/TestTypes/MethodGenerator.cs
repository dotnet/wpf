// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if !STANDALONE_BUILD
using TrustedMethodInfo = Microsoft.Test.Security.Wrappers.MethodInfoSW;
using System.Security.Permissions;
#else
using TrustedMethodInfo = System.Reflection.MethodInfo;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary/>
    // TODO-Miguep: cleanup
    //[PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class MethodGenerator : MethodGeneratorBase
    {
        /// <summary/>
        [System.CLSCompliant(false)]
        public MethodGenerator(TrustedMethodInfo method)
            : base(method)
        {
        }

        /// <summary/>
        protected override string MethodSignature
        {
            get
            {
                return "\noverride " + Modifiers + ReturnType + Method.Name + "( " + Parameters + " )\n";
            }
        }

        /// <summary/>
        protected override string MethodBody
        {
            get
            {
                string body = "{\n";
                if (Method.ReturnType != null)
                {
                    if (Method.ReturnType.IsValueType)
                    {
                        body += Indent("return new " + Method.ReturnType + "();\n");
                    }
                    else
                    {
                        body += Indent("return null;\n");
                    }
                }
                body += "}\n";

                return body;
            }
        }

        /// <summary/>
        protected string ReturnType
        {
            get
            {
                if (Method.ReturnType == null)
                {
                    return "void ";
                }
                else
                {
                    return Method.ReturnType + " ";
                }
            }
        }

        /// <summary/>
        [System.CLSCompliant(false)]
        protected TrustedMethodInfo Method { get { return (TrustedMethodInfo)methodBase; } }
    }
}
