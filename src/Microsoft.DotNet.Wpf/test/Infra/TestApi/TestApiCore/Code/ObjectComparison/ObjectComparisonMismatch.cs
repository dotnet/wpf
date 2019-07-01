// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.Test.ObjectComparison
{
    /// <summary>
    /// Represents one comparison mismatch.
    /// </summary>
    ///
    /// <example>
    /// The following example shows how to derive the list of comparison mismatches.
    ///
    /// <code>
    /// Person p1 = new Person("John");
    /// p1.Children.Add(new Person("Peter"));
    /// p1.Children.Add(new Person("Mary"));
    ///
    /// Person p2 = new Person("John");
    /// p2.Children.Add(new Person("Peter"));
    ///
    /// // Perform the compare operation
    /// ObjectGraphFactory factory = new PublicPropertyObjectGraphFactory();
    /// ObjectComparer comparer = new ObjectComparer(factory);
    /// IEnumerable&lt;ObjectComparisonMismatch&gt; m12 = new List&lt;ObjectComparisonMismatch&gt;();
    /// Console.WriteLine(
    ///     "Objects p1 and p2 {0}", 
    ///     comparer.Compare(p1, p2, out m12) ? "match!" : "do NOT match!");
    ///     
    /// foreach (ObjectComparisonMismatch m in m12)
    /// {
    ///     Console.WriteLine(
    ///         "Nodes '{0}' and '{1}' do not match. Mismatch message: '{2}'",
    ///         m.LeftObjectNode != null ? m.LeftObjectNode.Name : "null",
    ///         m.RightObjectNode != null ? m.LeftObjectNode.Name : "null",
    ///         m.MismatchType); 
    /// }
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

    [DebuggerDisplay("{MismatchType}: LeftNodeName={LeftObjectNode.QualifiedName}")]
    public sealed class ObjectComparisonMismatch
    {
        #region Constructors

        /// <summary>
        /// Creates an instance of the ObjectComparisonMismatch class.
        /// </summary>
        /// <param name="leftObjectNode">The node from the left object.</param>
        /// <param name="rightObjectNode">The node from the right object.</param>
        /// <param name="mismatchType">Represents the type of mismatch.</param>
        public ObjectComparisonMismatch(GraphNode leftObjectNode, GraphNode rightObjectNode, ObjectComparisonMismatchType mismatchType)
        {
            this.leftObjectNode = leftObjectNode;
            this.rightObjectNode = rightObjectNode;
            this.mismatchType = mismatchType;
        }

        #endregion Public Members

        #region Public Members

        /// <summary>
        /// Gets the node in the left object.
        /// </summary>
        public GraphNode LeftObjectNode
        {
            get
            {
                return this.leftObjectNode;
            }
        }

        /// <summary>
        /// Gets the node in the right object.
        /// </summary>
        public GraphNode RightObjectNode
        {
            get
            {
                return this.rightObjectNode;
            }
        }

        /// <summary>
        /// Represents the type of mismatch.
        /// </summary>
        public ObjectComparisonMismatchType MismatchType 
        {
            get
            {
                return this.mismatchType;
            }
        }

        #endregion

        #region Private Data

        private GraphNode leftObjectNode;
        private GraphNode rightObjectNode;
        private ObjectComparisonMismatchType mismatchType;

        #endregion
    }
}
