// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using BindingFlags = System.Reflection.BindingFlags;

#if !STANDALONE_BUILD
using TrustedPropertyInfo = Microsoft.Test.Security.Wrappers.PropertyInfoSW;
using TrustedFieldInfo = Microsoft.Test.Security.Wrappers.FieldInfoSW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
using Microsoft.Test.Graphics.TestTypes;
using Microsoft.Test.Graphics.Factories;
#else
using TrustedPropertyInfo = System.Reflection.PropertyInfo;
using TrustedFieldInfo = System.Reflection.FieldInfo;
using TrustedType = System.Type;
using Microsoft.Test.Graphics.Factories;
using Microsoft.Test.Graphics.TestTypes;
#endif

namespace Microsoft.Test.Graphics
{
    /// <summary>
    /// Utilities Class used throughout testing
    /// </summary>
    public class ObjectUtils
    {
        /// <summary/>
        new public static bool Equals(object obj1, object obj2)
        {
            if (obj1 == null)
            {
                return (obj2 == null);
            }
            return obj1.Equals(obj2);
        }

        /// <summary/>
        public static bool DeepEquals(object obj1, object obj2)
        {
            return DeepEquals(obj1, obj2, false);
        }

        /// <summary>
        /// Performs a deep equality check on all properties inherited from Animatable and lower in the hierarchy.
        /// Properties on Freezable (i.e. IsFrozen) and its ancestor classes are ignored.
        /// </summary>
        public static bool DeepEqualsToAnimatable(object obj1, object obj2)
        {
            return DeepEquals(obj1, obj2, true);
        }

        private static bool DeepEquals(object obj1, object obj2, bool skipUnimportant)
        {
            if (object.ReferenceEquals(obj1, obj2))
            {
                // Shortcut- if they are the same object, return true;
                return true;
            }
            Type type1 = obj1.GetType();
            Type type2 = obj2.GetType();
            if (type1 != type2)
            {
                return false;
            }
            if (type1 == typeof(string))
            {
                return obj1.ToString() == obj2.ToString();
            }

            bool equals;
            TrustedType trustedType = PT.Trust(type1);
            TrustedPropertyInfo[] properties = trustedType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (TrustedPropertyInfo property in properties)
            {
                if (skipUnimportant)
                {
                    // If we don't care about the property (declared by Freezable or its ancestors), skip it!
                    if (IsPropertyDeclaredByAncestorsOf(PT.Untrust(property.DeclaringType), typeof(Freezable)) ||
                         IsPropertyDeclaredByAncestorsOf(PT.Untrust(property.DeclaringType), typeof(FrameworkElement)))
                    {
                        continue;
                    }
                }
                if (IsPropertyProblematic(property))
                {
                    continue;
                }
                if (property.Name == "Item")
                {
                    if (obj1 is IEnumerable)
                    {
                        equals = DeepEquals((IEnumerable)obj1, (IEnumerable)obj2, skipUnimportant);
                    }
                    else
                    {
                        // This is an indexer.  We can't really compare it.
                        equals = true;
                    }
                }
                else
                {
                    object value1 = property.GetValue(obj1, null);
                    object value2 = property.GetValue(obj2, null);

                    if (property.PropertyType.IsValueType)
                    {
                        switch (property.PropertyType.Name)
                        {
                            case "Double": equals = MathEx.AreCloseEnough((double)value1, (double)value2); break;
                            case "Point": equals = MathEx.AreCloseEnough((Point)value1, (Point)value2); break;
                            case "Vector": equals = MathEx.AreCloseEnough((Vector)value1, (Vector)value2); break;
                            case "Rect": equals = MathEx.AreCloseEnough((Rect)value1, (Rect)value2); break;
                            case "Matrix": equals = MathEx.AreCloseEnough((Matrix)value1, (Matrix)value2); break;
                            case "Point3D": equals = MathEx.AreCloseEnough((Point3D)value1, (Point3D)value2); break;
                            case "Point4D": equals = MathEx.AreCloseEnough((Point4D)value1, (Point4D)value2); break;
                            case "Quaternion": equals = MathEx.AreCloseEnough((Quaternion)value1, (Quaternion)value2); break;
                            case "Vector3D": equals = MathEx.AreCloseEnough((Vector3D)value1, (Vector3D)value2); break;
                            case "Rect3D": equals = MathEx.AreCloseEnough((Rect3D)value1, (Rect3D)value2); break;
                            case "Matrix3D": equals = MathEx.AreCloseEnough((Matrix3D)value1, (Matrix3D)value2); break;
                            default: equals = object.Equals(value1, value2); break;
                        }
                    }
                    else
                    {
                        equals = DeepEquals(value1, value2, skipUnimportant);
                    }
                }
                if (!equals)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsPropertyDeclaredByAncestorsOf(Type declaringType, Type type)
        {
            for (Type t = type; t != typeof(object); t = t.BaseType)
            {
                if (declaringType == t)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsPropertyProblematic(TrustedPropertyInfo property)
        {
            // These properties cause exceptions, infinite loops, or are useless info
            switch (property.Name)
            {
                case "Empty":
                case "Resources":
                case "SyncRoot":
                case "CultureInfo":
                case "TargetType":
                case "RenderedGeometry":
                case "Parent":
                case "Metadata":
                case "Inverse":
                case "BackBuffer":
                    return true;

                default:
                    return false;
            }
        }

        private static bool DeepEquals(IEnumerable obj1, IEnumerable obj2, bool skipUnimportant)
        {
            IEnumerator e1 = obj1.GetEnumerator();
            IEnumerator e2 = obj2.GetEnumerator();

            while (true)
            {
                bool more1 = e1.MoveNext();
                bool more2 = e2.MoveNext();
                if (more1 != more2)
                {
                    // They don't have the same number of items.
                    return false;
                }
                if (more1 == false)
                {
                    // No more objects to compare
                    return true;
                }
                object value1 = e1.Current;
                object value2 = e2.Current;

                if (!DeepEquals(value1, value2, skipUnimportant))
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the string representing the property that a complex path refers to.
        /// </summary>
        /// <param name="complexPropertyPath">A "dot-down" path to a property</param>
        /// <returns>The property after the last '.', or the input parameter if no '.' is found</returns>
        public static string GetLastPropertyName(string complexPropertyPath)
        {
            int index = complexPropertyPath.LastIndexOf('.');
            if (index < 0)
            {
                return complexPropertyPath;
            }
            else
            {
                return complexPropertyPath.Substring(index + 1);
            }
        }

        /// <summary>
        /// Get the object that owns the property specified.
        /// </summary>
        /// <param name="complexPropertyPath">A "dot-down" path to a property</param>
        /// <param name="attachedTo">The object to start the search from</param>
        /// <returns>The object that owns the property specified</returns>
        /// <exception cref="ArgumentException">Thrown when the path does not lead to a valid object</exception>
        public static object GetPropertyOwner(string complexPropertyPath, object attachedTo)
        {
            // complexProperty will come in like this:
            //
            //  path == Thickness                   attachedTo == ScreenSpaceLines3D    return: ScreenSpaceLines3D
            //  path == Material[0].Brush.Color     attachedTo == GeometryModel3D       return: Brush
            //  path == Brush.Color                 attachedTo == Material              return: Brush
            //  path == Color                       attachedTo == Brush                 return: Brush
            //  path == Children[0]                 attachedTo == Viewport3D            return: Visual3DCollection

            int dotIndex = complexPropertyPath.IndexOf('.');
            if (dotIndex < 0)
            {
                int bracketIndex = complexPropertyPath.IndexOf('[');
                if (bracketIndex < 0)
                {
                    // The property should be on the object we're looking at right now.
                    // Throw an exception if the property does not exist on this object.
                    TrustedType trustedType = PT.Trust(attachedTo.GetType());
                    TrustedPropertyInfo property = trustedType.GetProperty(complexPropertyPath);
                    if (property == null)
                    {
                        throw new ArgumentException(complexPropertyPath + " does not exist on " + trustedType.Name);
                    }

                    return attachedTo;
                }
                else
                {
                    // Trim the index from the property (the collection is the owner)
                    string nonIndexedProperty = complexPropertyPath.Substring(0, bracketIndex);
                    return GetAttachedObject(nonIndexedProperty, attachedTo);
                }
            }

            // The property is not on the current object

            string localPropertyName = complexPropertyPath.Substring(0, dotIndex);
            string remainingProperties = complexPropertyPath.Substring(dotIndex + 1);

            object next = GetAttachedObject(localPropertyName, attachedTo);

            return GetPropertyOwner(remainingProperties, next);
        }

        /// <summary>
        /// Get the object specified by a path.
        /// </summary>
        /// <param name="complexPropertyPath">A "dot-down" path to a property</param>
        /// <param name="attachedTo">The object to start the search from</param>
        /// <returns>The object specified by the path</returns>
        /// <exception cref="ArgumentException">Thrown when the path does not lead to a valid DependencyObject</exception>
        public static DependencyObject GetDependencyObject(string complexPropertyPath, object attachedTo)
        {
            string lastProperty = GetLastPropertyName(complexPropertyPath);
            object lastObject = GetPropertyOwner(complexPropertyPath, attachedTo);

            DependencyObject returnValue = GetAttachedObject(lastProperty, lastObject) as DependencyObject;
            if (returnValue == null)
            {
                throw new ArgumentException(
                                "The path specified does not lead to a DependencyObject.  " +
                                "complexPropertyPath = " + complexPropertyPath + ", attachedTo = " + attachedTo.GetType());
            }
            return returnValue;
        }

        private static object GetAttachedObject(string propertyName, object attachedTo)
        {
            // This function assumes that the propertyName is not a path (no '.' in it)

            int index = propertyName.IndexOf('[');
            string nonIndexedProperty = (index < 0) ? propertyName : propertyName.Substring(0, index);

            TrustedType trustedType = PT.Trust(attachedTo.GetType());
            TrustedPropertyInfo property = trustedType.GetProperty(nonIndexedProperty);
            object returnValue = property.GetValue(attachedTo, null);

            if (index < 0)
            {
                // This isn't a collection, just return what we got.
                return returnValue;
            }
            else
            {
                // This is a collection. Parse the index string and return the value at that index.
                int indexLength = propertyName.Length - nonIndexedProperty.Length - 2;
                string s = propertyName.Substring(index + 1, indexLength).Trim();
                int i = StringConverter.ToInt(s);

                // Visual3DCollection does not implement IList like the other collections do :(
                if (returnValue is Visual3DCollection)
                {
                    return ((Visual3DCollection)returnValue)[i];
                }
                else
                {
                    return ((IList)returnValue)[i];
                }
            }
        }

        /// <summary>
        /// public wrapper of the internal API "DependencyProperty.FromName"
        /// </summary>
        /// <param name="propertyName">The name of the property to get</param>
        /// <param name="propertyType">The type of the property owner</param>
        /// <returns>The DependencyProperty represented by the name/type pair</returns>
        /// <exception cref="ArgumentException">Thrown when propertyName is not a valid DependencyProperty on propertyType</exception>
        public static DependencyProperty GetDependencyProperty(string propertyName, Type propertyType)
        {
            TrustedType trustedType = PT.Trust(propertyType);
            TrustedFieldInfo field = trustedType.GetField(
                                                propertyName + "Property",
                                                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                                                );
            if (field == null)
            {
                throw new ArgumentException("Could not locate property " + propertyName + " on " + propertyType.Name, "propertyName");
            }
            return (DependencyProperty)field.GetValue(null);
        }

        /// <summary>
        /// Attaches unique names to all of the Visual3Ds, Model3Ds, and MeshGeometry3Ds
        /// in a Visual3DCollection.
        /// </summary>
        /// <param name="visuals">The collection of Visual3Ds to name</param>
        public static void NameObjects(IEnumerable<Visual3D> visuals)
        {
            // This is a wrapper for the private methods
            int visualCount = 0;
            int modelCount = 0;
            NameObjects(visuals, ref visualCount, ref modelCount);
        }

        private static void NameObjects(IEnumerable<Visual3D> visuals, ref int visualCount, ref int modelCount)
        {
            if (visuals == null)
            {
                return;
            }
            foreach (Visual3D visual3D in visuals)
            {
                if (visual3D is ModelVisual3D)
                {
                    visual3D.SetValue(Const.NameProperty, "Visual_" + visualCount++);
                    NameObjects(((ModelVisual3D)visual3D).Content, ref modelCount);
                    NameObjects(((ModelVisual3D)visual3D).Children, ref visualCount, ref modelCount);
                }
            }
        }

        /// <summary>
        /// Attaches unique names to all of the Model3Ds and MeshGeometry3Ds
        /// in a Model3D.
        /// </summary>
        public static void NameObjects(Model3D model)
        {
            // This is just a wrapper for the private method
            int modelCount = 0;
            NameObjects(model, ref modelCount);
        }

        private static void NameObjects(Model3D model, ref int modelCount)
        {
            if (model == null)
            {
                return;
            }

            if (model.GetValue(Const.NameProperty) != Const.NameProperty.DefaultMetadata.DefaultValue)
            {
                // This model and sub-models (where possible) have already been named.
                return;
            }

            // Name this model and increment the count
            model.SetValue(Const.NameProperty, "Model_" + modelCount++);

            if (model is Model3DGroup)
            {
                foreach (Model3D child in GetChildren((Model3DGroup)model))
                {
                    NameObjects(child, ref modelCount);
                }
            }
            else if (model is GeometryModel3D)
            {
                if (((GeometryModel3D)model).Geometry != null)
                {
                    // Put a name on the geometry of this model too.
                    // Since count was already incremented, subtract 1 so that it matches the model's number.
                    ((GeometryModel3D)model).Geometry.SetValue(Const.NameProperty, "Geometry_" + (modelCount - 1));
                }
            }
        }

        /// <summary/>
        public static string GetName(DependencyObject obj)
        {
            return (string)obj.GetValue(Const.NameProperty);        
        }

        /// <summary/>
        public static void SetName(DependencyObject obj, string name)
        {
            obj.SetValue(Const.NameProperty, name);
        }

        /// <summary/>
        public static Model3DCollection GetChildren(Model3DGroup group)
        {
            if (group.Children == null)
            {
                return new Model3DCollection();
            }
            return group.Children;
        }
    }
}
