// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Stability.Extensions
{
    /// <summary>
    /// Interface for Discoverable Property objects
    /// The properties of said objects can be populated for 
    /// producing content/performing actions.
    /// </summary>
    public interface IDiscoverableObject
    {

    }

    /// <summary>
    /// Defines input source for Properties on IDiscoverableObject
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class InputAttribute : Attribute
    {
        #region Private Data
        
        /// The maximum size of a list, default value 3. 
        private int maxListSize = 3;

        #endregion
        #region Constructor

        /// <summary/>
        public InputAttribute(ContentInputSource source)
        {
            ContentInputSource = source;
        }

        #endregion

        #region public Implementation

        /// <summary>
        /// Create content from Factory 
        /// </summary>
        public static readonly InputAttribute CreateFromFactory = new InputAttribute(ContentInputSource.CreateFromFactory);

        ///<Summary>
        /// Gather content from WindowService
        /// </Summary>
        public static readonly InputAttribute GetFromLogicalTree = new InputAttribute(ContentInputSource.GetFromLogicalTree);

        ///<Summary>
        /// Gather content from WindowService
        /// </Summary>
        public static readonly InputAttribute GetFromVisualTree = new InputAttribute(ContentInputSource.GetFromVisualTree);

        ///<Summary>
        /// Gather object from Object tree, this include each element in Visual Tree, as well as value of each property.  
        /// </Summary>
        internal static readonly InputAttribute GetFromObjectTree = new InputAttribute(ContentInputSource.GetFromObjectTree);

        /// <summary>
        /// Create from Constraints
        /// </summary>
        public static readonly InputAttribute CreateFromConstraints = new InputAttribute(ContentInputSource.CreateFromConstraints);

        /// <summary>
        /// Get WinodowList property from state
        /// </summary>
        public static readonly InputAttribute GetWindowListFromState = new InputAttribute(ContentInputSource.GetWindowListFromState);

        #endregion

        #region public Properties

        /// <summary>
        /// Describes where content input is supplied from.
        /// </summary>
        public ContentInputSource ContentInputSource { get; set; }

        /// <summary>
        /// Used to describe content which is essential to successful test execution.
        /// This is only relevant to control for Factory inputs on potentially infinite recursion scenarios.
        /// </summary>
        public bool IsEssentialContent { get; set; }

        /// <summary>
        /// The minimum size of a list, default value 0.
        /// Used to describe size of List<T> of objects for consumption.
        /// This is only relevant for Lists of Factory produced objects.
        /// </summary>
        public int MinListSize { get; set; }

        /// <summary>
        /// The maximum size of a list, default value 3.
        /// Used to describe size of List<T> of objects for consumption.
        /// This is only relevant for Lists of Factory produced objects.
        /// </summary>
        public int MaxListSize 
        {
            get
            {
                return maxListSize;
            }
            set
            {
                maxListSize = value;
            }
        }

        #endregion
    }
    /// <summary>
    /// Content Input Source enumeration
    /// </summary>
    public enum ContentInputSource
    {
        /// <summary>
        /// Create an object from a factory.
        /// This is the default behavior, including in scenarios 
        /// where no attribute is set.
        /// </summary>
        CreateFromFactory = 0,
        /// <summary>
        /// Get an object from the existing logical Tree state
        /// </summary>
        GetFromLogicalTree = 1,
        /// <summary>
        /// Get an object from the existing Visual Tree 
        /// </summary>
        GetFromVisualTree = 2,
        /// <summary>
        /// Create an object from constraints table
        /// </summary>
        CreateFromConstraints = 3,
        /// <summary>
        /// Get WindowList property from state
        /// </summary>
        GetWindowListFromState = 4,
        /// <summary>
        /// Get an object from existing Object Tree 
        /// </summary>
        GetFromObjectTree = 5
    }
}
