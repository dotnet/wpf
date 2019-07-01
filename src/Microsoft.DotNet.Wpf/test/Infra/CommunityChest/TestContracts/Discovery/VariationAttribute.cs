// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// Test variation attribute multiplies cases defined by Test
    /// by adding class constructor parameters.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
    public class VariationAttribute : TestAttribute
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public VariationAttribute() : base(null)
        {
        }

        /// <summary>
        /// Constructor allowing an arbitrary number of parameters that map to
        /// constructor parameters. Deprecated.
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        /// <param name="parameters"></param>
        //[Obsolete("Parameters should specified a comma separated list for the ConstructorParameters property. This constructor is not CLS-compliant")]
        public VariationAttribute(params object[] parameters) : base(null)
        {
            // Since the PropertyBag that ConstructorParameters is fed into for the driver to consume
            // only takes strings, we have to convert the strongly typed parameters into a comma separated
            // list.
            ConstructorParameters = parameters.ToCommaSeparatedList();
        }

        #endregion

        #region Public Members

        /// <summary/>
        /// Comma separated list of constructor parameter values.
        /// <summary/>
        public string ConstructorParameters { get; set; }

        #endregion
    }
}