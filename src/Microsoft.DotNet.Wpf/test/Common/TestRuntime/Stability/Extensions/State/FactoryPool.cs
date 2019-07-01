// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.Factories;

namespace Microsoft.Test.Stability.Extensions.State
{
    /// <summary>
    /// A pool of factories
    /// </summary>
    class FactoryPool
    {
        #region Private Data

        //A dictionary of<Type,List<Type>>
        private TypedListDictionary factoryDictionary = new TypedListDictionary();

        #endregion

        #region Constructor

        /// <summary>
        /// Create Factories mapped to satisfy the demanded types(and all the intermediate dependencies).
        /// Any failures to locate a needed factory will cause an exception.
        /// </summary>
        /// <param name="factoriesPath">Path to the Factories List file</param>
        /// <param name="demandedTypes">A list of types needed for stress test execution</param>
        public FactoryPool(string factoriesPath, List<Type> demandedTypes, ConstraintsTable constraintsTable)
        {
            List<Type> factoryTypes = TypeListReader.ParseTypeList(factoriesPath, typeof(DiscoverableFactory));
            List<DiscoverableFactory> factories = InstantiateFactories(factoryTypes);
            RegisterFactories(factories, demandedTypes);
            constraintsTable.VerifyConstraints(factoryTypes);
        }
        #endregion

        #region Public Implementation

        /// <summary>
        /// Returns a randomly selected Factory.
        /// If favorSimpleFactories is true, will instead attempt to return first instance of a factory with no essential factory inputs, to avoid further recursion, before doing default algorithm.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public DiscoverableFactory GetFactory(Type t, DeterministicRandom random, bool favorSimpleFactories)
        {
            ArrayList list = factoryDictionary.GetObjectsOfType(t);

            if (favorSimpleFactories)
            {
                foreach (Type factoryType in list)
                {
                    if (IsSimpleFactory(factoryType))
                    {
                        return (DiscoverableFactory)Activator.CreateInstance(factoryType);
                    }
                }
            }
            {

                Type factoryType = (Type)list[random.Next(list.Count)];

                return (DiscoverableFactory)Activator.CreateInstance(factoryType);
            }
        }

        private bool IsSimpleFactory(Type t)
        {
            PropertyInfo[] props = t.GetProperties();
            foreach (PropertyInfo p in props)
            {
                object[] attrs = p.GetCustomAttributes(typeof(InputAttribute), true);
                if (attrs.Length > 0)
                {
                    InputAttribute inputAttribute = (InputAttribute)attrs[0];
                    if (inputAttribute != null && inputAttribute.IsEssentialContent == true)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region Private Implementation

        private void RegisterFactories(List<DiscoverableFactory> factories, List<Type> inputTypes)
        {
            List<Type> supportedOutputs = new List<Type>();
            HomelessTestHelpers.Merge(inputTypes, GatherFactoryInputs(factories));
            MapFactories(factories, inputTypes);
        }

        //Register all the factories supporting inputs to their consumed type. Fail if no factory supports a demanded input.
        private void MapFactories(List<DiscoverableFactory> factories, List<Type> inputTypes)
        {
            List<Type> unsupportedOutputs = new List<Type>();

            Trace.WriteLine("[FactoryPool] Checking for satisfaction of inputs.");
            foreach (Type inputType in inputTypes)
            {
                Type testedType = inputType;
                bool inputMatchFound = false;
                //TODO: Clean up to align as a strategy with the actual factory operations
                if (inputType.IsGenericType)
                {
                    if (testedType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        testedType = testedType.GetGenericArguments()[0];
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported inputType:" + inputType);
                    }
                }

                Trace.WriteLine("[FactoryPool] Checking producers of "+inputType);
                //Find all the matching factories for a given type
                for (int factoryIndex = 0; factoryIndex < factories.Count; factoryIndex++)
                {
                    DiscoverableFactory factory = factories[factoryIndex];
                    if (factory.CanCreate(testedType))
                    {
                        Trace.WriteLine("[FactoryPool]    Matched on Factory:" + factory.GetType());
                        inputMatchFound = true;
                        factoryDictionary.Add(testedType, factory.GetType());
                    }
                }

                if (!inputMatchFound)
                {
                    unsupportedOutputs.Add(testedType);
                }
            }

            if (unsupportedOutputs.Count > 0)
            {
                throw new ArgumentException(String.Format("These requested inputTypes could not be fulfilled by Factory pool:{0}", DumpList(unsupportedOutputs)));
            }
        }

        private string DumpList(List<Type> unsupportedOutputs)
        {
            StringBuilder sb=new StringBuilder();
            sb.AppendLine();
            foreach (Type t in unsupportedOutputs)
            {
                sb.AppendLine(t.Name);
            }
            return sb.ToString();
        }

        // Identify the types needed to produce content for Factories
        private List<Type> GatherFactoryInputs(List<DiscoverableFactory> factories)
        {
            List<Type> testInputTypes = new List<Type>();
            foreach (DiscoverableFactory factory in factories)
            {
                List<Type> factoryInputTypes = DiscoverableInputHelper.GetFactoryInputTypes(factory.GetType());
                HomelessTestHelpers.Merge(testInputTypes, factoryInputTypes);
            }
            return testInputTypes;
        }

        #endregion


        #region Private Implementation

        //Identify all the types needed to perform actions
        private List<Type> GetActionInputs(List<Type> actions)
        {
            List<Type> actionInputs = new List<Type>();
            foreach (Type t in actions)
            {
                List<Type> inputTypes = DiscoverableInputHelper.GetFactoryInputTypes(t);
                foreach (Type type in inputTypes)
                {
                    if (!actionInputs.Contains(type))
                    {
                        actionInputs.Add(type);
                    }
                }
            }
            return actionInputs;
        }

        //Create all the factories based on their types
        private List<DiscoverableFactory> InstantiateFactories(List<Type> factoryTypes)
        {
            List<DiscoverableFactory> factories = new List<DiscoverableFactory>();
            foreach (Type t in factoryTypes)
            {
                DiscoverableFactory factory = (DiscoverableFactory)Activator.CreateInstance(t);
                factories.Add(factory);
            }
            return factories;
        }

        #endregion
    }
}
