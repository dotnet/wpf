// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Markup;

using FileMode = System.IO.FileMode;
using BindingFlags = System.Reflection.BindingFlags;
using MemoryStream = System.IO.MemoryStream;

#if !STANDALONE_BUILD
using TrustedFileStream = Microsoft.Test.Security.Wrappers.FileStreamSW;
using TrustedPropertyInfo = Microsoft.Test.Security.Wrappers.PropertyInfoSW;
using TrustedMethodInfo = Microsoft.Test.Security.Wrappers.MethodInfoSW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedFileStream = System.IO.FileStream;
using TrustedPropertyInfo = System.Reflection.PropertyInfo;
using TrustedMethodInfo = System.Reflection.MethodInfo;
using TrustedType = System.Type;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Sanity Check for Serialization/Deserialization of Media3D types
    /// </summary>
    public class SerializationTest : CoreGraphicsTest
    {
        /// <summary/>
        public override void Init(Variation v)
        {
            base.Init(v);
            xamlFile = v["Xaml"];
            if (xamlFile == null)
            {
                throw new ApplicationException("SerializationTest is missing Xaml=");
            }

            string bad = v["Bad"];
            shouldFail = (bad == null) ? false : StringConverter.ToBool(bad);

            if (shouldFail)
            {
                Log("Expecting bad parameters");
            }
        }

        /// <summary/>
        public override void RunTheTest()
        {
            // Can't parse XAML outside of Dispatcher (whether resulting tree is used or not)
            ParseSerializeParse(null);
        }

        private object ParseSerializeParse(object o)
        {
            // Parse the xaml file, Serialize the resulting tree, and parse again
            try
            {
                object root1 = ParseXaml(xamlFile);
                object root2 = RoundTrip(root1);

                if (priority > 0)
                {
                    // Only do this for non-BVTs now
                    Log("RootElement : " + root1.GetType().Name);
                    CompareProperties(root1.GetType(), root1, root2, string.Empty);
                }
            }
            catch (XamlParseException ex)
            {
                if (shouldFail)
                {
                    Log("Bad parameter caught");
                }
                else
                {
                    AddFailure("XamlParseException thrown:\r\n{0}", ex);
                }
            }
            return null;
        }

        /// <summary/>
        public static object ParseXaml(string filename)
        {
            object root = null;
            using (TrustedFileStream stream = new TrustedFileStream(filename, FileMode.Open))
            {
                root = XamlReader.Load(PT.Untrust(stream));
            }
            return root;
        }

        /// <summary>
        /// Take the root of a WPF tree, serialize it, and parse it again.
        /// Return the result of the parsing.
        /// </summary>
        public static object RoundTrip(object root)
        {
            string xaml = XamlWriter.Save(root);

            // Convert the string into a unicode byte stream
            //  (2 bytes per unicode char) + 2 bytes for endian marker
            UnicodeEncoding encoder = new UnicodeEncoding();
            byte[] bytes = new byte[(xaml.Length * 2) + 2];

            // Put the endian marker at the beginning of the array
            //  so that it is recognized as Unicode by the parser
            encoder.GetPreamble().CopyTo(bytes, 0);
            encoder.GetBytes(xaml).CopyTo(bytes, 2);

            return XamlReader.Load(new MemoryStream(bytes));
        }

        private void CompareProperties(Type type, object obj1, object obj2, string space)
        {
            space += "    ";

            // We don't want static properties.  They're problematic.
            TrustedPropertyInfo[] properties = PT.Trust(type).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (TrustedPropertyInfo property in properties)
            {
                string propertyName = property.Name;
                TrustedType propertyType = property.PropertyType;

                if (!property.DeclaringType.IsPublic)
                {
                    continue;
                }

                Log(space + "{0} : {1}", propertyName, propertyType.Name);

                if (IsPropertyProblematic(propertyName))
                {
                    Log(space + "- Skipping problematic property");
                    continue;
                }

                // "Item" is the indexer property's name.
                // This property requires parameters to be evaluated and is therefore a problem.
                // -  For IEnumerables, we can iterate through its items and compare them.
                // -  We don't know how to dynamically deal with other types of indexers so we skip them.
                if (propertyName == "Item")
                {
                    if (obj1 is IEnumerable)
                    {
                        CompareItems(type, (IEnumerable)obj1, (IEnumerable)obj2, space);
                    }
                    else
                    {
                        Log(space + "- Skipping indexer");
                    }
                    continue;
                }

                object value1 = property.GetValue(obj1, null);
                object value2 = property.GetValue(obj2, null);

                if (propertyType.IsValueType)
                {
                    CompareValueTypes(value1, value2, space);
                }
                else
                {
                    CompareRefTypes(value1, value2, space);
                }
            }
        }

        private bool IsPropertyProblematic(string propertyName)
        {
            // These properties cause exceptions, infinite loops, or are useless info

            return propertyName == "Empty" || propertyName == "Dispatcher" ||
                   propertyName == "Resources" || propertyName == "DependencyObjectType" ||
                   propertyName == "SyncRoot" || propertyName == "Parent" ||
                   propertyName == "CultureInfo" || propertyName == "TargetType" ||
                   propertyName == "Inverse" || propertyName == "RenderedGeometry" ||
                   propertyName == "IsFrozen" || propertyName == "IsSealed";
        }

        private void CompareValueTypes(object value1, object value2, string space)
        {
            if (object.Equals(value1, value2))
            {
                Log(space + "       {0} == {1}", value1, value2);
            }
            else
            {
                AddFailure(space + "{0} != {1}", value1, value2);
            }
        }

        private void CompareRefTypes(object value1, object value2, string space)
        {
            if (value1 == null && value2 == null)
            {
                Log(space + "       null == null");
                return;
            }
            Type type1 = value1.GetType();
            Type type2 = value2.GetType();

            if (type1 != type2)
            {
                AddFailure(space + "{0} != {1}", type1, type2);

                // Can't go any further since everything will fail, bail out!
                return;
            }

            if (type1.Namespace.Contains("Media"))
            {
                // While we're at it, call the clone methods too...
                VerifyCloneForFreezable(value1 as Freezable);
            }

            if (type1 == typeof(string))
            {
                if (value1.ToString() == value2.ToString())
                {
                    Log(space + "       \"{0}\" == \"{1}\"", value1, value2);
                }
                else
                {
                    AddFailure(space + "\"{0}\" != \"{1}\"", value1, value2);
                }
                return;
            }

            CompareProperties(type1, value1, value2, space);
        }

        private void CompareItems(Type type, IEnumerable obj1, IEnumerable obj2, string space)
        {
            space += "    ";

            if (type.Namespace.Contains("Media"))
            {
                VerifyCloneForFreezable(obj1 as Freezable);
            }

            IEnumerator enum1 = obj1.GetEnumerator();
            IEnumerator enum2 = obj2.GetEnumerator();
            int objectsCounted = 0;

            while (enum1.MoveNext() && enum2.MoveNext())
            {
                object value1 = enum1.Current;
                object value2 = enum2.Current;

                Log(space + "Item[{0}] : {1}", objectsCounted, value1.GetType().Name);
                if (value1.GetType().IsValueType)
                {
                    CompareValueTypes(value1, value2, space);
                }
                else
                {
                    CompareRefTypes(value1, value2, space);
                }

                objectsCounted++;
            }
            if (objectsCounted == 0)
            {
                Log(space + "   (empty)");
            }
        }

        private void VerifyCloneForFreezable(Freezable frozenObject)
        {
            if (frozenObject == null)
            {
                return;
            }

            TrustedType type = PT.Trust(frozenObject.GetType());
            TrustedMethodInfo method = type.GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                throw new ApplicationException("Could not find Clone method on " + type.Name);
            }

            Freezable copy = (Freezable)method.Invoke(frozenObject, null);

            if (!ObjectUtils.DeepEqualsToAnimatable(frozenObject, copy))
            {
                AddFailure("{0}.Copy failed to produce an exact copy", type.Name);
            }

            method = type.GetMethod("CloneCurrentValue", BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                throw new ApplicationException("Could not find CloneCurrentValue method on " + type.Name);
            }

            copy = (Freezable)method.Invoke(frozenObject, null);

            if (!ObjectUtils.DeepEqualsToAnimatable(frozenObject, copy))
            {
                AddFailure("{0}.CloneCurrentValue failed to produce an exact copy", type.Name);
            }
        }

        private string xamlFile;
        private bool shouldFail;
    }
}
