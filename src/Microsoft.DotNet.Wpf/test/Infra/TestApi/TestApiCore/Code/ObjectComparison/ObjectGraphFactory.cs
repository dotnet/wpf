// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ObjectComparison
{
    /// <summary>
    /// Creates a graph for the provided object.
    /// </summary>
    /// <example>
    /// The following example demonstrates the use of a simple factory to do shallow comparison of two objects.
    /// <code>
    /// class Person
    /// {
    ///     public Person(string name)
    ///     {
    ///         Name = name;
    ///         Children = new Collection&lt;Person&gt;();
    ///     }
    ///     public string Name { get; set; }
    ///     public Collection&lt;Person&gt; Children { get; private set; }
    /// }
    /// </code>
    /// <code>
    /// class SimpleObjectGraphFactory : ObjectGraphFactory
    /// {
    ///     public override GraphNode CreateObjectGraph(object o)
    ///     {
    ///         // Build the object graph with nodes that need to be compared.
    ///         // in this particular case, we only pick up the object itself
    ///         GraphNode node = new GraphNode();
    ///         node.Name = "PersonObject";
    ///         node.ObjectValue = (o as Person).Name;
    ///         return node;
    ///     }
    /// }
    /// </code>
    /// <code>
    /// Person p1 = new Person("John");
    /// p1.Children.Add(new Person("Peter"));
    /// p1.Children.Add(new Person("Mary"));
    ///
    /// Person p2 = new Person("John");
    /// p2.Children.Add(new Person("Peter"));
    ///
    /// ObjectGraphFactory factory = new SimpleObjectGraphFactory();
    /// ObjectComparer comparer = new ObjectComparer(factory);
    /// Console.WriteLine(
    ///     "Objects p1 and p2 {0}",
    ///     comparer.Compare(p1, p2) ? "match!" : "do NOT match!");
    /// </code>
    /// </example>
    public abstract class ObjectGraphFactory
    {
        /// <summary>
        /// Creates a graph for the given object.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>The root node of the created graph.</returns>
        public virtual GraphNode CreateObjectGraph(object value)
        {
            throw new NotSupportedException("Please provide a behavior for this method in a derived class");
        }
    }
}
