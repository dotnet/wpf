// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Test.ObjectComparison
{
    /// <summary>
    /// Represents a generic object comparer. This class uses an 
    /// <see cref="ObjectGraphFactory"/> instance to convert objects to graph
    /// representations before comparing the representations.
    /// </summary>
    /// <remarks>
    /// Comparing two objects for equivalence is a relatively common task during test validation. 
    /// One example would be to test whether a type follows the rules required by a particular 
    /// serializer by saving and loading the object and comparing the two. A deep object 
    /// comparison is one where all the properties and its properties are compared repeatedly 
    /// until primitives are reached. The .NET Framework provides mechanisms to perform such comparisons but 
    /// requires the types in question to implement part of the comparison logic 
    /// (IComparable, .Equals). However, there are often types that do not follow 
    /// these mechanisms. This API provides a mechanism to deep compare two objects using 
    /// reflection. 
    /// </remarks>
    /// 
    /// <example>
    /// The following example demonstrates how to compare two objects using a general-purpose object 
    /// comparison strategy (represented by <see cref="PublicPropertyObjectGraphFactory"/>).
    /// 
    /// <code>
    /// Person p1 = new Person("John");
    /// p1.Children.Add(new Person("Peter"));
    /// p1.Children.Add(new Person("Mary"));
    ///
    /// Person p2 = new Person("John");
    /// p2.Children.Add(new Person("Peter"));
    /// 
    /// ObjectGraphFactory factory = new PublicPropertyObjectGraphFactory();
    /// ObjectComparer comparer = new ObjectComparer(factory);
    /// Console.WriteLine(
    ///     "Objects p1 and p2 {0}", 
    ///     comparer.Compare(p1, p2) ? "match!" : "do NOT match!");
    /// </code>
    ///
    /// where Person is declared as follows:
    ///
    /// <code>
    /// class Person
    /// {
    ///     public Person(string name) 
    ///     { 
    ///         Name = name;
    ///         Children = new Collection&lt;Person&gt;();
    ///     }
    ///     public string Name { get; set; }
    ///     public Collection&lt;Person&gt; Children { get; private set;  }
    /// }
    /// </code>
    /// </example>
    ///
    /// <example>
    /// In addition, the object comparison API allows the user to get back a list of comparison mismatches. 
    /// For an example, see <see cref="ObjectComparisonMismatch"/> objects). 
    /// </example>
    public sealed class ObjectComparer
    {
        #region Constuctors

        /// <summary>
        /// Creates an instance of the ObjectComparer class.
        /// </summary>
        /// <param name="factory">An ObjectGraphFactory to use for 
        /// converting objects to graphs.</param>
        public ObjectComparer(ObjectGraphFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            this.objectGraphFactory = factory;
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Gets the ObjectGraphFactory used to convert objects
        /// to graphs.
        /// </summary>
        public ObjectGraphFactory ObjectGraphFactory
        {
            get
            {
                return this.objectGraphFactory;
            }
        }

        /// <summary>
        /// Performs a deep comparison of two objects.
        /// </summary>
        /// <param name="leftValue">The left object.</param>
        /// <param name="rightValue">The right object.</param>
        /// <returns>true if the objects match.</returns>
        public bool Compare(object leftValue, object rightValue)
        {
            IEnumerable<ObjectComparisonMismatch> mismatches;
            return Compare(leftValue, rightValue, out mismatches);
        }

        /// <summary>
        /// Performs a deep comparison of two objects and provides 
        /// a list of mismatching nodes.
        /// </summary>
        /// <param name="leftValue">The left object.</param>
        /// <param name="rightValue">The right object.</param>
        /// <param name="mismatches">The list of mismatches.</param>
        /// <returns>true if the objects match.</returns>
        public bool Compare(object leftValue, object rightValue, out IEnumerable<ObjectComparisonMismatch> mismatches)
        {
            List<ObjectComparisonMismatch> mismatch;
            bool isMatch = this.CompareObjects(leftValue, rightValue, out mismatch);
            mismatches = mismatch;

            return isMatch;
        }

        #endregion

        #region Private Members

        private bool CompareObjects(object leftObject, object rightObject, out List<ObjectComparisonMismatch> mismatches)
        {
            mismatches = new List<ObjectComparisonMismatch>();

            // Get the graph from the objects 
            GraphNode leftRoot = this.ObjectGraphFactory.CreateObjectGraph(leftObject);
            GraphNode rightRoot = this.ObjectGraphFactory.CreateObjectGraph(rightObject);

            // Get the nodes in breadth first order 
            List<GraphNode> leftNodes = new List<GraphNode>(leftRoot.GetNodesInDepthFirstOrder());
            List<GraphNode> rightNodes = new List<GraphNode>(rightRoot.GetNodesInDepthFirstOrder());

            // For each node in the left tree, search for the
            // node in the right tree and compare them
            for (int i = 0; i < leftNodes.Count; i++)
            {
                GraphNode leftNode = leftNodes[i];

                var nodelist = from node in rightNodes
                               where leftNode.QualifiedName.Equals(node.QualifiedName)
                               select node;

                List<GraphNode> matchingNodes = nodelist.ToList<GraphNode>();
                if (matchingNodes.Count != 1)
                {
                    ObjectComparisonMismatch mismatch = new ObjectComparisonMismatch(leftNode, null, ObjectComparisonMismatchType.MissingRightNode);
                    mismatches.Add(mismatch);
                    continue;
                }

                GraphNode rightNode = matchingNodes[0];

                // Compare the nodes 
                ObjectComparisonMismatch nodesMismatch = CompareNodes(leftNode, rightNode);
                if (nodesMismatch != null)
                {
                    mismatches.Add(nodesMismatch);
                }
            }

            bool passed = mismatches.Count == 0 ? true : false;

            return passed;
        }

        private static ObjectComparisonMismatch CompareNodes(GraphNode leftNode, GraphNode rightNode)
        {
            // Check if both are null 
            if (leftNode.ObjectValue == null && rightNode.ObjectValue == null)
            {
                return null;
            }

            // check if one of them is null 
            if (leftNode.ObjectValue == null || rightNode.ObjectValue == null)
            {
                ObjectComparisonMismatch mismatch = new ObjectComparisonMismatch(
                    leftNode,
                    rightNode,
                    ObjectComparisonMismatchType.ObjectValuesDoNotMatch);
                return mismatch;
            }

            // compare type names //
            if (!leftNode.ObjectType.Equals(rightNode.ObjectType))
            {
                ObjectComparisonMismatch mismatch = new ObjectComparisonMismatch(
                    leftNode,
                    rightNode,
                    ObjectComparisonMismatchType.ObjectTypesDoNotMatch);
                return mismatch;
            }

            // compare primitives, strings
            if (leftNode.ObjectType.IsPrimitive || leftNode.ObjectType == typeof(string))
            {
                if (!leftNode.ObjectValue.Equals(rightNode.ObjectValue))
                {
                    ObjectComparisonMismatch mismatch = new ObjectComparisonMismatch(
                        leftNode,
                        rightNode,
                        ObjectComparisonMismatchType.ObjectValuesDoNotMatch);
                    return mismatch;
                }
                else
                {
                    return null;
                }
            }

            // compare the child count 
            if (leftNode.Children.Count != rightNode.Children.Count)
            {
                var type = leftNode.Children.Count > rightNode.Children.Count ?
                    ObjectComparisonMismatchType.RightNodeHasFewerChildren : ObjectComparisonMismatchType.LeftNodeHasFewerChildren;

                ObjectComparisonMismatch mismatch = new ObjectComparisonMismatch(
                    leftNode,
                    rightNode,
                    type);
                return mismatch;
            }

            // No mismatch //
            return null;
        }

        #endregion

        #region Private Data

        private ObjectGraphFactory objectGraphFactory;

        #endregion
    }
}
