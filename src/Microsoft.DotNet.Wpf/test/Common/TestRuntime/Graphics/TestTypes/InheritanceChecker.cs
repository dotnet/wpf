// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;

#if !STANDALONE_BUILD
using TrustedAssembly = Microsoft.Test.Security.Wrappers.AssemblySW;
using TrustedPath = Microsoft.Test.Security.Wrappers.PathSW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedAssembly = System.Reflection.Assembly;    
using TrustedPath = System.IO.Path;
using TrustedType = System.Type;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Test whether or not you can inherit from a given class
    /// </summary>
    public class InheritanceChecker : CoreGraphicsTest
    {
        /// <summary/>
        public override void Init(Variation v)
        {
            base.Init(v);
            v.AssertExistenceOf("ClassName", "Namespace", "CanInherit", "Assembly");

            this.className = v["ClassName"];
            this.nameSpace = v["Namespace"];
            string inherit = v["CanInherit"];
            string asm = v["Assembly"];

            dotnetPath = v["DotnetPath"];     // We define this for local testing if we don't use avalon.msi

            if (dotnetPath == null)
            {
                try
                {
                    dotnetPath = EnvironmentWrapper.GetEnvironmentVariable("LAPI");
                }
                catch (System.Security.SecurityException ex)
                {
                    AddFailure("Could not access the Environment variable: LAPI");
                    Log("Exception: " + ex);
                }

                if (dotnetPath == null)
                {
                    throw new ApplicationException("LAPI is not defined. I can't find my binaries.");
                }
            }
            Log("DotnetPath = " + dotnetPath);

            TrustedAssembly assembly = TrustedAssembly.LoadFile(TrustedPath.Combine(dotnetPath, asm));
            this.type = assembly.GetType(nameSpace + "." + className);
            if (type == null)
            {
                throw new ApplicationException(nameSpace + "." + className + " was not found in " + asm);
            }

            this.classGenerator = new ClassGenerator(type);
            this.canInherit = StringConverter.ToBool(inherit);
        }

        /// <summary>
        /// Generate some code that inherits from the class specified by VARIATION.
        /// If we can't inherit from the class we should see compiler errors.
        /// </summary>
        public override void RunTheTest()
        {
            // Create the compiler and set necessary options.
            // We will compile the code as a library so that we don't have to write a Main method.

            CSharpCodeProvider compiler = new CSharpCodeProvider();
            CompilerParameters options = new CompilerParameters();
            options.GenerateInMemory = true;
            options.GenerateExecutable = false;
            options.IncludeDebugInformation = false;
            options.OutputAssembly = "inherit.dll";

            options.ReferencedAssemblies.Add("System.dll");
            options.ReferencedAssemblies.Add(TrustedPath.Combine(dotnetPath, "PresentationCore.dll"));
            options.ReferencedAssemblies.Add(TrustedPath.Combine(dotnetPath, "PresentationFramework.dll"));
            options.ReferencedAssemblies.Add(TrustedPath.Combine(dotnetPath, "WindowsBase.dll"));

            // Compile the generated code

            CompilerResults results = compiler.CompileAssemblyFromSource(options, GeneratedSource);

            // Check the results of compilation

            if (results.Errors.HasErrors)
            {
                if (canInherit)
                {
                    AddFailure("I should be able to inherit from: " + className);
                    Log("Compiler Errors:\n{0}", CompilerErrorsToString(results.Errors));
                    Log("\r\nGenerated source:\r\n{0}", FormattedSource);
                }
                else
                {
                    // Make sure we have the correct compiler errors
                    string[] expectedText = (type.IsSealed) ? new string[] { "cannot", "derive" }
                                                              : new string[] { "no", "constructors" };

                    if (!VerifyCorrectCompilerErrors(results.Errors, expectedText))
                    {
                        AddFailure("This test is broken and needs to be investigated");
                        Log("Compiler Errors:\n{0}", CompilerErrorsToString(results.Errors));
                        Log("\r\nGenerated source:\r\n{0}", FormattedSource);
                    }
                }
            }
            else // compilation succeeded
            {
                if (!canInherit)
                {
                    AddFailure("I should not be able to inherit from: " + className);
                    Log("\r\nGenerated source:\r\n{0}", FormattedSource);
                }
            }
        }

        /// <summary>
        /// Generated code that tests if inheritance is possible with the current type
        /// </summary>
        private string GeneratedSource
        {
            get
            {
                return "using System;\n" +
                        "using " + nameSpace + ";\n" +
                        "\n" +
                        "namespace InheritanceTest\n" +
                        "{\n" +
                        classGenerator.GenerateCode() +
                        "}\n";
            }
        }

        private string FormattedSource
        {
            get
            {
                return GeneratedSource.Replace("\n", "\r\n");
            }
        }
        
        private string CompilerErrorsToString(CompilerErrorCollection errors)
        {
            string result = string.Empty;
            foreach (CompilerError e in errors)
            {
                result += e.Line + ": " + e.ErrorText + "\n";
            }
            return result;
        }

        private bool VerifyCorrectCompilerErrors(CompilerErrorCollection errors, string[] expectedText)
        {
            // There should only be one error
            //  and it should have the text I'm searching for in it.

            if (errors.Count != 1)
            {
                return false;
            }

            CompilerError e = errors[0];

            foreach (string s in expectedText)
            {
                if (!e.ErrorText.Contains(s))
                {
                    return false;
                }
            }
            return true;
        }

        private string className;
        private string nameSpace;
        private string dotnetPath;
        private bool canInherit;
        private TrustedType type;
        private ClassGenerator classGenerator;
    }
}
