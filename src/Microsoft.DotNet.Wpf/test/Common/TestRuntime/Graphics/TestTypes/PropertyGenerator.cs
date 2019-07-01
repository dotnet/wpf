// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if !STANDALONE_BUILD
using TrustedMethodInfo = Microsoft.Test.Security.Wrappers.MethodInfoSW;
using TrustedPropertyInfo = Microsoft.Test.Security.Wrappers.PropertyInfoSW;
#else
using TrustedMethodInfo = System.Reflection.MethodInfo;
using TrustedPropertyInfo = System.Reflection.PropertyInfo;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Generate a property override
    /// </summary>

    public sealed class PropertyGenerator : MethodGenerator
    {

        /// <summary/>
        [System.CLSCompliant(false)]
        public PropertyGenerator(TrustedPropertyInfo property)
            : base(null)
        {
            get = property.GetGetMethod();
            set = property.GetSetMethod();
        }

        /// <summary/>
        protected override string MethodSignature
        {
            get
            {
                methodBase = (get != null) ? get : set;
                string signature = "\noverride " + Modifiers + ReturnType;

                // trim the "get_" or "set_" from the function name;
                signature += Method.Name.Substring(4) + "\n";

                return signature;
            }
        }

        /// <summary/>
        protected override string MethodBody
        {
            get
            {
                string body = "{\n";
                body += Indent(GetBody);
                body += Indent(SetBody);
                body += "}\n";
                return body;
            }
        }

        private string GetBody
        {
            get
            {
                string body = string.Empty;
                if (get != null)
                {
                    body += "get\n";
                    body += "{\n";
                    if (get.ReturnType.IsValueType)
                    {
                        body += Indent("return new " + get.ReturnType + "();\n");
                    }
                    else
                    {
                        body += Indent("return null;\n");
                    }
                    body += "}\n";
                }
                return body;
            }
        }

        private string SetBody
        {
            get
            {
                string body = string.Empty;
                if (set != null)
                {
                    body += "set\n";
                    body += "{\n";
                    if (set.ReturnType.IsValueType)
                    {
                        body += Indent("return new " + set.ReturnType + "();\n");
                    }
                    else
                    {
                        body += Indent("return null;\n");
                    }
                    body += "}\n";
                }
                return body;
            }
        }

        private TrustedMethodInfo get;
        private TrustedMethodInfo set;
    }
}
