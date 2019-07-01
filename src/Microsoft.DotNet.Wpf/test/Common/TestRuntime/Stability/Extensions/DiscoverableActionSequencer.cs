// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Actions;
using Microsoft.Test.Stability.Extensions.Factories;
using Microsoft.Test.Stability.Extensions.State;


namespace Microsoft.Test.Stability.Extensions
{
    /// <summary>
    /// This Actionsequencer consumes a stress test assembly and gathers all the actions for future use.
    /// </summary>
    [Serializable]
    internal class DiscoverableActionSequencer : IActionSequencer
    {
        //HACK: DiscoverableActionSequencer is meant to be stateless, and should not be accessing TestDefinition object.
        //These settings should be loaded via Stress State initialization.
        public DiscoverableActionSequencer()
        {            
            ContentPropertyBag testParameters = DriverState.DriverParameters;
            string standardRecursionLimitString = testParameters["StandardRecursionLimit"];
            if (!String.IsNullOrEmpty(standardRecursionLimitString))
            {
                standardRecursionLimit = Int32.Parse(standardRecursionLimitString);                
                Trace.WriteLine("[DiscoverableActionSequencer] standardRecursionLimit: " + standardRecursionLimit);
                
            }

            string absoluteRecursionLimitString = testParameters["AbsoluteRecursionLimit"];
            if (!String.IsNullOrEmpty(absoluteRecursionLimitString))
            {
                absoluteRecursionLimit = Int32.Parse(absoluteRecursionLimitString);
                Trace.WriteLine("[DiscoverableActionSequencer] absoluteRecursionLimit: " + absoluteRecursionLimit);
            }
            
            if (standardRecursionLimit > absoluteRecursionLimit)
            {
                throw new InvalidOperationException("AbsoluteRecursionLimit must be greater than StandardRecursionLimit");
            }
            if (standardRecursionLimit <= 0)
            {
                throw new InvalidOperationException("Recursion Limits must exceed 0");
            }
        }

        #region IActionSequencer Members

        public Sequence GetNext(IState state, DeterministicRandom random)
        {
            Sequence sequence = null;
            ConvenienceStressState stressState = (ConvenienceStressState)state;
            Queue<Type> possibleActions = stressState.GetActions(random);
            while (possibleActions.Count > 0 && sequence == null)
            {
                DiscoverableAction action = (DiscoverableAction)Activator.CreateInstance(possibleActions.Dequeue());
                Trace.WriteLine("[DiscoverableActionSequencer]Populating Action.");
                
                bool hasValidInputs = PopulateDiscoverableInputs(action, random, stressState, 0);

                //Add action to sequence only CanPerform is true, and input has been created successfully. 
                if (hasValidInputs && action.CanPerform())
                {
                    sequence = new Sequence();
                    sequence.AddAction(action);
                }
            }
            if (sequence == null)
            {
                throw new InvalidOperationException("Stress could not find a new Action to perform at this state.");
            }
            return sequence;
        }

        /// <summary>
        /// Populates Input properties for a Factory or Action
        /// </summary>
        /// <param name="consumer">Object to be populated</param>
        /// <param name="random">Deterministic Random source</param>
        /// <param name="state">Supporting State Object</param>
        /// <param name="recursionLimit">Recursion depth - ie - how many parent invocations does the current code have?</param>
        /// <returns></returns>
        private static bool PopulateDiscoverableInputs(IDiscoverableObject consumer, DeterministicRandom random, ConvenienceStressState state, int recursionDepth)
        {
            List<PropertyDescriptor> inputProperties = DiscoverableInputHelper.GetInputProperties(consumer.GetType());
            foreach (PropertyDescriptor property in inputProperties)
            {
                //Determine desired type
                Type consumedType = property.PropertyType;
                Object result = null;

                InputAttribute inputAttribute = (InputAttribute)property.Attributes[typeof(InputAttribute)];
                if (inputAttribute == null) { inputAttribute = InputAttribute.CreateFromFactory; }

                switch (inputAttribute.ContentInputSource)
                {
                    //TODO: Consolidate Logical, Visual Tree, and windows as being sourced from State object. We should move to a child property.
                    case ContentInputSource.GetFromLogicalTree:
                        // Factories can not consume existing objects - Only actions can do so
                        if (consumer.GetType().IsSubclassOf(typeof(DiscoverableFactory)))
                        {
                            throw new InvalidOperationException("A Factory cannot consume from state");
                        }
                        //Get Property from window surface Area
                        Trace.WriteLine("[DiscoverableActionSequencer] Getting a Logical Tree Item");
                        result = state.GetFromLogicalTree(consumedType, random);

                        //For object from Logical tree, null means item not found, thus failed.
                        if (result == null)
                        {
                            return false;
                        }
                        break;

                    case ContentInputSource.GetFromVisualTree:
                        // Factories can not consume existing objects - Only actions can do so
                        if (consumer.GetType().IsSubclassOf(typeof(DiscoverableFactory)))
                        {
                            throw new InvalidOperationException("A Factory cannot consume from state");
                        }
                        //Get Property from window surface Area
                        Trace.WriteLine("[DiscoverableActionSequencer] Getting a Visual Tree item");
                        result = state.GetFromVisualTree(consumedType, random);

                        //For object from Visual tree, null result means item not found, thus failed.
                        if (result == null)
                        {
                            return false;
                        }
                        break;

                    case ContentInputSource.GetWindowListFromState:
                        // Factories can not consume existing objects - Only actions can do so
                        if (consumer.GetType().IsSubclassOf(typeof(DiscoverableFactory)))
                        {
                            throw new InvalidOperationException("A Factory cannot consume from state");
                        }

                        Trace.WriteLine("[DiscoverableActionSequencer] Getting WindowList property of state");
                        result = state.WindowList;
                        break;

                    //Populate data from Type.Property specific constraints system
                    // TODO: Auto-magically use constraints by default for constrained properties
                    case ContentInputSource.CreateFromConstraints:
                        Trace.WriteLine("[DiscoverableActionSequencer] Getting a Constraint based item");
                        result = state.MakeFromConstraint(consumer.GetType(), property.Name, random);                
                        break;

                    case ContentInputSource.GetFromObjectTree:
                        Trace.WriteLine("[DiscoverableActionSequencer] Getting a object from Object tree.");
                        result = state.GetFromObjectTree(consumedType, random);

                        //For object from Object tree, null result means item not found, thus failed.
                        if (result == null)
                        {
                            return false;
                        }
                        break;

                    //Default behavior is to produce content from factories
                    default:
                        if (recursionDepth < standardRecursionLimit ||
                            (recursionDepth < absoluteRecursionLimit && inputAttribute.IsEssentialContent))
                        {
                            Trace.WriteLine("[DiscoverableActionSequencer] Producing factory content");
                            //Basic workflow for producing Factory content
                            if (!consumedType.IsGenericType)
                            {
                                result = MakeItem(consumedType, state, random, recursionDepth + 1);
                            }
                            else
                            {
                                if (consumedType.GetGenericTypeDefinition() == typeof(List<>))
                                {
                                    result = MakeList(consumedType, state, random, recursionDepth + 1, inputAttribute.MinListSize, inputAttribute.MaxListSize);
                                }
                                else
                                {
                                    throw new InvalidOperationException("We can't work with this type yet:. " + consumedType.GetGenericTypeDefinition());
                                }
                            }
                        }
                        else if (recursionDepth >= absoluteRecursionLimit && inputAttribute.IsEssentialContent)
                        {
                            throw new InvalidOperationException("The stress test factory has reached the absolute recursion limit and is still demanding essential content. Is there a cyclic chain only involving factories requiring their inputs?");
                        }
                        else  //Factory Recursion depth exceeded. We are leaving non-essential property null
                        {
                            //No Operation
                        }
                        break;
                }

                //Set value to desired input
                property.SetValue(consumer, result);
                if (result != null)
                {
                    Trace.WriteLine(String.Format("[DiscoverableActionSequencer] Provided a [{0}] of type {1} for {2}", result, result.GetType(), consumer));
                }
                else
                {
                    Trace.WriteLine(String.Format("[DiscoverableActionSequencer] Provided a null for {1}", result, consumer));
                }
            }

            return true;
        }

        private static object MakeItem(Type consumedType, ConvenienceStressState state, DeterministicRandom random, int recursionDepth)
        {
            //Look up a Factory supporting this type
            DiscoverableFactory factory = state.GetFactory(consumedType, random, FavorSimpleFactories(recursionDepth));

            //Provide the factory with what it needs
            PopulateDiscoverableInputs(factory, random, state, recursionDepth);

            //Create desired type via factory
            return factory.Create(consumedType, random);
        }

        private static bool FavorSimpleFactories(int recursionDepth)
        {
            return recursionDepth >= standardRecursionLimit;
        }

        private static object MakeList(Type consumedType, ConvenienceStressState state, DeterministicRandom random, int recursionDepth, int minListSize, int maxListSize)
        {
            //The Stress consumer is asking for a list of objects. This is a bit of a complicated topic. Refer to:
            //Reflecting on Generics - This explains how I am identifying List Generic
            //http://cc.msnscache.com/cache.aspx?q=72938808547333&mkt=en-US&lang=en-US&w=d704070d&FORM=CVRE7
            //
            //From there, we need to create an instance of the requested generic, as explained here.
            //http://www.codeproject.com/KB/cs/ReflectGenerics.aspx

            //The type of objects instantiated in the list is instanceType:
            Type[] genericArguments = consumedType.GetGenericArguments();
            Type instanceType = genericArguments[0];

            //Prepare our List
            object list = Activator.CreateInstance(consumedType, 1);

            //Create a single element  array as payload for adding objects to list via reflection
            object[] methodCallPayload = new object[1];

            int count = random.Next(maxListSize - minListSize) + minListSize;
            for (int i = 0; i < count; i++)
            {
                //We produce an item from a differently selected factory each time.
                methodCallPayload[0] = MakeItem(instanceType, state, random, recursionDepth);
                consumedType.InvokeMember("Add", BindingFlags.InvokeMethod, null, list, methodCallPayload);
            }
            return list;
        }

        #endregion
        #region private Variables

        static int standardRecursionLimit = 10;
        static int absoluteRecursionLimit = 15;

        #endregion

    }
}
