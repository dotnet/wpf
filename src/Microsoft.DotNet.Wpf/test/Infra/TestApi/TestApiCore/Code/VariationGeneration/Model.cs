// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Contains all the parameters and constraints for the system under test, and produces a 
    /// set of variations by using combinatorial testing techniques.
    /// </summary>
    /// <typeparam name="T">The type of variations that should be generated.</typeparam>
    /// 
    /// <example>
    /// For examples, refer to <see cref="Model"/> and <see cref="ParameterAttribute"/>.
    /// </example>
    public class Model<T> where T : new()
    {
        /// <summary>
        /// Initializes a new model with parameters to be inferred by reflection.
        /// </summary>
        public Model()
        {
        }

        /// <summary>
        /// Initializes a new model with the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public Model(IEnumerable<ParameterBase> parameters) : this(parameters, null)
        {
        }

        /// <summary>
        /// Initializes a new model with the specified parameters and constraints.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="constraints">The constraints.</param>
        public Model(IEnumerable<ParameterBase> parameters, IEnumerable<Constraint<T>> constraints)
        {
            if (parameters != null)
            {
                this.parameters.AddRange(parameters);
            }

            if (constraints != null)
            {
                this.constraints.AddRange(constraints);
            }
        }

        List<ParameterBase> parameters = new List<ParameterBase>();

        /// <summary>
        /// The parameters in the model.
        /// </summary>
        public IList<ParameterBase> Parameters { get { return parameters; } }

        List<Constraint<T>> constraints = new List<Constraint<T>>();

        /// <summary>
        /// The constraints in the model.
        /// </summary>
        public ICollection<Constraint<T>> Constraints { get { return constraints; } }

        /// <summary>
        /// The default tag for generated variations. Set this property when no value in the variation has been tagged. The default is null.
        /// </summary>
        public object DefaultVariationTag { get; set; }

        /// <summary>
        /// Generates an order-wise set of variations using a constant seed.
        /// </summary>
        /// <param name="order">The order of the selected combinations (2 is every pair, 3 is every triple, and so on). Must be between 1 and the number of parameters.</param>
        /// <returns>The variations.</returns>
        public virtual IEnumerable<T> GenerateVariations(int order)
        {
            return GenerateVariations(order, defaultSeedValue);
        }

        /// <summary>
        /// Generates an order-wise set of variations using the specified seed for random generation.
        /// </summary>
        /// <param name="order">The order of the selected combinations (2 is every pair, 3 is every triple, and so on). Must be between 1 and the number of parameters.</param>
        /// <param name="seed">The seed that is used for random generation.</param>
        /// <returns>The variations.</returns>
        public virtual IEnumerable<T> GenerateVariations(int order, int seed)
        {
            // create parameters and set values if model is supplied in a template
            propertiesMap = null;
            if (typeof(T) != typeof(Variation))
            {
                if (parameters.Count == 0)
                {
                    propertiesMap = CreateParameters(seed);
                }
                else
                {
                    propertiesMap = CreatePropertyMapFromParameters();
                }
            }
            

            // validate parameters
            if (order < 1 || order > Parameters.Count)
            {
                throw new ArgumentOutOfRangeException("order", order, "order must be between 1 and the number of parameters.");
            }

            if (typeof(T) == typeof(Variation))
            {
                return VariationGenerator.GenerateVariations(this, order, seed).Cast<T>();
            }

            return new VariationsWrapper<T>(propertiesMap,
                    VariationGenerator.GenerateVariations(this, order, seed));
        }

        private Dictionary<string, PropertyInfo> CreatePropertyMapFromParameters()
        {
            var propertyMap = new Dictionary<string, PropertyInfo>();
            foreach (ParameterBase parameter in Parameters)
            {
                PropertyInfo prop = typeof(T).GetProperty(parameter.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (prop == null || !prop.CanWrite)
                {
                    throw new InvalidOperationException("Unable to find valid property for '" + parameter.Name + "'.  Parameter name must match with a writable property.");
                }
                propertyMap[parameter.Name] = prop;
            }

            return propertyMap;
        }

        private const int defaultSeedValue = 12345;
        internal Dictionary<string, PropertyInfo> propertiesMap;

        #region Declarative scenario methods
        /// <summary>
        /// Retrieves array of the attributes of specified type
        /// given member is decorated with.
        /// </summary>
        /// <typeparam name="TAttribute">Attribute type.</typeparam>
        /// <param name="memberInfo">Class member.</param>
        /// <returns>Array of attributes defined for the member; if none 
        /// defined the array is zero length.</returns>
        static TAttribute[] GetAttributes<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute
        {
            object[] attributes = Attribute.GetCustomAttributes(memberInfo, typeof(TAttribute), true);
            if (attributes.Length < 1)
            {
                return new TAttribute[0];
            }
            return attributes as TAttribute[];
        }

        /// <summary>
        /// Iterates over model properties, validates them and 
        /// retrieves properly attributed ones.
        /// </summary>
        /// <returns>Dictionary of properties keyed off by the name.</returns>
        static Dictionary<string, PropertyInfo> ReadPropertiesMetadata()
        {
            Dictionary<string, PropertyInfo> propertiesMap = new Dictionary<string, PropertyInfo>();
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                ParameterAttribute[] attributes = GetAttributes<ParameterAttribute>(property);
                if (attributes.Length == 0)
                {
                    continue;
                }

                if (!property.CanWrite)
                {
                    throw new InvalidOperationException(string.Format("'{0}' is not writable.  Only writable properties can define parameters.", property.Name));
                }

                // validate all attributes defined
                // each attribute must have at least one value
                foreach (ParameterAttribute attribute in attributes)
                {
                    if (attribute.Values.Length == 0)
                    {
                        throw new Exception(string.Format("ParameterAttribute for {0} property has no values", property.Name));
                    }
                }
                propertiesMap[property.Name] = property;
            }

            return propertiesMap;
        }

        /// <summary>
        /// Creates model parameters based on attributes set on
        /// model properties.
        /// </summary>
        /// <param name="seed">Seed used to select values for equivalnce 
        /// classes generation.</param>
        Dictionary<string, PropertyInfo> CreateParameters(int seed)
        {
            // get set of relevantly attributed properties
            Dictionary<string, PropertyInfo> propertiesMap = ReadPropertiesMetadata();
            List<object> values = new List<object>();
            parameters.Clear();

            // for each property create model parameter and fill it with values
            foreach (string propertyName in propertiesMap.Keys)
            {
                values.Clear();
                PropertyInfo property = propertiesMap[propertyName];
                Type propertyType = property.PropertyType;
                ParameterAttribute[] attributes = GetAttributes<ParameterAttribute>(property);

                // adding values from attributes defined into the list
                if (attributes.Length > 1)
                {
                    // if more than one attribute defined, equivalence classes are
                    // defined for the property; random value needs to be picked from
                    // each one
                    values.AddRange(CreateEquivalenceClassValues(seed, propertyType, attributes));
                }
                else
                {
                    // since the properties here are always attributed
                    // there exist at least one attribute; 
                    // just adding all values from attribute
                    values.AddRange(attributes[0].Values);
                }

                // create a parameter with specified name, type and list of values
                // and add it to the model
                this.parameters.Add(CreateParameter(propertyName, propertyType, values));
            }

            return propertiesMap;
        }

        /// <summary>
        /// Returns list of values selected\created based on attributes 
        /// provided.
        /// <remarks>Equivalence class functionality is achieved by using
        /// seed provided to create random generator and then use it to
        /// select one random value from each attribute.</remarks>
        /// </summary>
        /// <param name="seed">Seed used to select value from equivalence class.</param>
        /// <param name="valueType">Type used to create ParameterValue instance to incorporate
        /// Weight\Tag (if defined).</param>
        /// <param name="attributes">List of attributes to select values from.</param>
        /// <returns>List of values selected\created from attrubutes.</returns>
        static object[] CreateEquivalenceClassValues(int seed, Type valueType, ParameterAttribute[] attributes)
        {
            List<object> values = new List<object>();
            foreach (ParameterAttribute attribute in attributes)
            {
                Type parameterValueType = null;
                Random random = new Random(seed);
                
                object value = attribute.Values[random.Next(attribute.Values.Length - 1)];
                
                // if weight or tag is defined, need to create parameter value
                // o\w just using value from attribute
                if (attribute.Weight > 0 || attribute.Tag != null)
                {
                    if (parameterValueType == null)
                    {
                        parameterValueType = typeof(ParameterValue<>).MakeGenericType(valueType);
                    }
                    ParameterValueBase parameterValue = Activator.CreateInstance(parameterValueType, value, attribute.Tag, attribute.Weight)
                        as ParameterValueBase;
                    values.Add(parameterValue);
                }
                else
                {
                    values.Add(value);
                }
            }
            return values.ToArray();
        }

        /// <summary>
        /// Creates parameter with name and type as specified and 
        /// add values to it.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterType">Type of the parameter.</param>
        /// <param name="parameterValues">List of values to add to created parameter.</param>
        static ParameterBase CreateParameter(string parameterName, Type parameterType, List<object> parameterValues)
        {
            // create generic parameter of the right type (based off type supplied)
            Type genericParameterType = typeof(Parameter<>).MakeGenericType(parameterType);            
            ParameterBase parameter = Activator.CreateInstance(genericParameterType, parameterName)
                    as ParameterBase;

            // as parameter is generic, to add value need to call appropriate
            // generic method - either one using T or ParameterValue<T>
            MethodInfo methodValue = parameter.GetType().GetMethod("Add",
                new Type[] { parameterType });
            MethodInfo methodParameterValue = parameter.GetType().GetMethod("Add",
                new Type[] { typeof(ParameterValue<>).MakeGenericType(parameterType) });
            
            // call appropriate generic add method based on type of every value
            foreach (object value in parameterValues)
            {
                if (value is ParameterValueBase)
                {
                    methodParameterValue.Invoke(parameter, new object[] { value });
                }
                else
                {
                    methodValue.Invoke(parameter, new object[] { value });
                }
            }
            return parameter;
        }
        #endregion
    }




    /// <summary>
    /// Provides a general-purpose model that generates a <see cref="Variation"/>. 
    /// See also <see cref="Model{T}"/>.
    /// </summary>
    /// 
    /// <remarks>
    /// Exhaustively testing all possible inputs to any nontrivial software component is generally not possible
    /// because of the enormous number of variations. Combinatorial testing is one approach to achieve high coverage
    /// with a much smaller set of variations. Pairwise, the most common combinatorial strategy, tests every possible 
    /// pair of values. Higher orders of combinations (three-wise, four-wise, and so on) can also be used for higher coverage
    /// at the expense of more variations. See <a href="http://pairwise.org">Pairwise Testing</a> and 
    /// <a href="http://www.pairwise.org/docs/pnsqc2006/PNSQC%20140%20-%20Jacek%20Czerwonka%20-%20Pairwise%20Testing%20-%20BW.pdf">
    /// Pairwise Testing in Real World</a> for more resources.
    /// </remarks>
    /// 
    /// <example>
    /// The following example shows how to create a set of test-run configurations by using 
    /// a model that only uses variables.
    /// <code>
    /// // Specify the parameters and parameter values
    /// var os = new Parameter&lt;string&gt;("OS") { "WinXP", "Win2k3", "Vista", "Win7" };
    /// var memory = new Parameter&lt;int&gt;("Memory") { 512, 1024, 2048, 4096 };
    /// var graphics = new Parameter&lt;string&gt;("Graphics") { "Nvidia", "ATI", "Intel" };
    /// var lang = new Parameter&lt;string&gt;("Lang") { "enu", "jpn", "deu", "chs", "ptb" };
    ///
    /// var parameters = new List&lt;ParameterBase&gt; { os, memory, graphics, lang };
    ///
    /// var model = new Model(parameters);
    ///
    /// // The model is complete; now generate the configurations and print out
    /// foreach (var config in model.GenerateVariations(2))
    /// {
    ///     Console.WriteLine("{0} {1} {2} {3}",
    ///         config["OS"],
    ///         config["Memory"],
    ///         config["Graphics"],
    ///         config["Lang"]);
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// The following example shows how to create variations for a vacation planner that has a signature like this:
    /// CallVacationPlanner(string destination, int hotelQuality, string activity). This example demonstrates that certain
    /// activities are only available for certain destinations.
    /// <code>
    /// var de = new Parameter&lt;string&gt;("Destination") { "Whistler", "Hawaii", "Las Vegas" };
    /// var ho = new Parameter&lt;int&gt;("Hotel Quality") { 5, 4, 3, 2, 1 };
    /// var ac = new Parameter&lt;string&gt;("Activity") { "gambling", "swimming", "shopping", "skiing" };
    ///
    /// var parameters = new List&lt;ParameterBase&gt; { de, ho, ac };
    /// var constraints = new List&lt;Constraint&lt;Variation&gt;&gt;
    /// {
    ///     // If going to Whistler or Hawaii, then no gambling
    ///     Constraint&lt;Variation&gt;
    ///         .If(v =&gt; de.GetValue(v) == "Whistler" || de.GetValue(v) == "Hawaii")
    ///         .Then(v =&gt; ac.GetValue(v) != "gambling"),
    ///
    ///     // If going to Las Vegas or Hawaii, then no skiing
    ///     Constraint&lt;Variation&gt;
    ///         .If(v =&gt; de.GetValue(v) == "Las Vegas" || de.GetValue(v) == "Hawaii")
    ///         .Then(v =&gt; ac.GetValue(v) != "skiing"),
    ///
    ///     // If going to Whistler, then no swimming
    ///     Constraint&lt;Variation&gt;
    ///         .If(v =&gt; de.GetValue(v) == "Whistler")
    ///         .Then(v =&gt; ac.GetValue(v) != "swimming"),
    /// };
    /// var model = new Model(parameters, constraints);
    ///
    ///
    /// foreach (var vacationOption in model.GenerateVariations(2))
    /// {
    ///     Console.WriteLine("{0}, {1} stars -- {2}",
    ///         vacationOption["Destination"],
    ///         vacationOption["Hotel Quality"],
    ///         vacationOption["Activity"]);
    /// }
    /// </code>
    /// </example>
    ///
    /// <example>
    /// The following example shows how to create variations for a vacation planner that adds weights and tags
    /// to certain values.  Adding weights changes the frequency in which a value will occur. Adding tags allows expected values
    /// to be added to variations.
    /// <code>
    /// var de = new Parameter&lt;string&gt;("Destination") 
    /// { 
    ///     "Whistler", 
    ///     "Hawaii",
    ///     new ParameterValue&lt;string&gt;("Las Vegas") { Weight = 10.0 },
    ///     new ParameterValue&lt;string&gt;("Cleveland") { Tag = false }
    /// };
    /// var ho = new Parameter&lt;int&gt;("Hotel Quality") { 5, 4, 3, 2, 1 };
    /// var ac = new Parameter&lt;string&gt;("Activity") { "gambling", "swimming", "shopping", "skiing" };
    /// var parameters = new List&lt;ParameterBase&gt; { de, ho, ac };
    ///
    /// var model = new Model(parameters) { DefaultVariationTag = true };
    ///
    /// foreach (var v in model.GenerateVariations(1))
    /// {
    ///     Console.WriteLine("{0} {1} {2} {3}",
    ///         v["Destination"],
    ///         v["Hotel Quality"],
    ///         v["Activity"],
    ///         ((bool)v.Tag == false) ? "--&gt; don't go!" : "");
    /// }
    /// </code>
    /// </example>
    public class Model : Model<Variation>
    {
        /// <summary>
        /// Initializes a new model with the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public Model(IEnumerable<ParameterBase> parameters)
            : base(parameters, null)
        {
        }

        /// <summary>
        /// Initializes a new model with the specified parameters and constraints.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="constraints">The constraints.</param>
        public Model(IEnumerable<ParameterBase> parameters, IEnumerable<Constraint<Variation>> constraints) 
            : base(parameters, constraints)
        {
        }
    }
}
