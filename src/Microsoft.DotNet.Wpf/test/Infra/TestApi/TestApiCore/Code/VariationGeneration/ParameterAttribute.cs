// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;

namespace Microsoft.Test.VariationGeneration
{

// Suppressing CS3015 'ParameterAttribute' has no accessible constructors 
// which use only CLS-compliant types (params keyword is not CLS compliant)
// Providing empty constructor is not an option since attribute must always 
// be created with list of values and validation can be performed only at
// runtime
#pragma warning disable 3015

    /// <summary>
    /// Specifies an attribute to decorate the properties of the class
    /// that is used for generating model parameters.
    /// </summary>
    ///
    /// <remarks>
    /// When a <see cref="Model"/> property is decorated with a single attribute,
    /// the values from the attribute are added to the parameters that are
    /// created on the model and used for variation generation.
    /// When multiple attributes decorate the same property, a single
    /// value is selected from the values of each attribute (the number of
    /// values in the Model parameter is equal to the number of attributes 
    /// that are set on the property).
    /// </remarks>
    ///
    /// <example>
    /// The example below demonstrates how to use the Parameter attribute to declare a variation model. 
    /// <code>
    /// // First, provide a model class definition
    /// class OsConfigurationEx
    /// {
    ///     [Parameter(512, 1024, 2048)]
    ///     public int Memory { get; set; }
    ///
    ///     [Parameter("WinXP", "Vista", "Win2K8", "Win7")]
    ///     public string OS { get; set; }
    ///
    ///     [Parameter("enu", "jpn", "deu", "chs", "ptb")]
    ///     public string Language { get; set; }
    ///
    ///     [Parameter("NVidia", "ATI", "Intel")]
    ///     public string Graphics { get; set; }
    /// }
    ///
    /// // Then instantiate a model 
    /// var model = new Model&lt;OsConfigurationEx&gt;();
    /// foreach (OsConfigurationEx c in model.GenerateVariations(2))
    /// {
    ///     Console.WriteLine("{0} {1} {2} {3}", 
    ///         c.Memory, 
    ///         c.OS,
    ///         c.Language,
    ///         c.Graphics);
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// This example shows how to use attributes of the model definition 
    /// class to support equivalence classes with different weights.
    /// <code>
    /// class OsConfigurationEx
    /// {
    ///     [Parameter("WinXP", Weight = .3F)]
    ///     [Parameter("Vista", "Win7", Weight = .5F)]
    ///     public string OS { get; set; }
    ///
    ///     [Parameter("EN-US")]
    ///     [Parameter("JP-JP", "CN-CH")]
    ///     [Parameter("HE-IL", "AR-SA")]
    ///     public string Language { get; set; }
    /// }
    ///
    /// var model = new Model&lt;OsConfigurationEx&gt;();
    /// foreach (OsConfigurationEx c in model.GenerateVariations(1))
    /// {
    ///     Console.WriteLine("{0} {1}",
    ///         c.OS,
    ///         c.Language);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, 
                    AllowMultiple = true, 
                    Inherited = true)]    
    public class ParameterAttribute : Attribute
    {
        /// <summary>
        /// The values that are used when generating a model parameter.
        /// </summary>
        public object[] Values 
        { 
            get; 
            private set; 
        }

        /// <summary>
        /// Instantiates an attribute with a list of values.
        /// </summary>
        /// <param name="values">A list of values.</param>
        public ParameterAttribute(params object[] values)
        {
            Values = values.Clone() as object[];
        }

        /// <summary>
        /// Optional. The weight to assign to a model parameter 
        /// that is created based on an attribute.
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// Optional. The tag to assign to a model parameter 
        /// that is created based on an attribute.
        /// </summary>
        public object Tag { get; set; }
    }
#pragma warning restore 3015
}
