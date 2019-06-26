// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections;

namespace DrtXaml.XamlTestFramework
{
    public static class Assert
    {
        public static void AreEqual(decimal expected, decimal actual)
        {
            AreEqual(expected, actual, "Are not equal");
        }

        public static void AreEqual(int expected, int actual)
        {
            AreEqual(expected, actual, "Are not equal");
        }

        public static void AreEqual(uint expected, uint actual)
        {
            AreEqual(expected, actual, "Are not equal");
        }

        public static void AreEqual(string expected, string actual)
        {
            AreEqual(expected, actual, "Are not equal");
        }

        public static void AreEqual(float expected, float actual)
        {
            AreEqual(expected, actual, "Are not equal");
        }

        public static void AreEqual(double expected, double actual)
        {
            AreEqual(expected, actual, "Are not equal");
        }

        public static void AreEqual(object expected, object actual)
        {
            AreEqual(expected, actual, "Are not equal");
        }

        public static void AreEqual(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
            {
                Fail(message);
            }
        }

        public static void AreEqual(int expected, int actual, string message)
        {
            if (expected != actual)
            {
                Fail(message);
            }
        }

        public static void AreEqual(uint expected, uint actual, string message)
        {
            if (expected != actual)
            {
                Fail(message);
            }
        }

        public static void AreEqual(string expected, string actual, string message)
        {
            if (expected != actual)
            {
                Fail(message);
            }
        }

        public static void AreEqual(float expected, float actual, string message)
        {
            if (expected != actual)
            {
                Fail(message);
            }
        }

        public static void AreEqual(double expected, double actual, string message)
        {
            if (expected != actual)
            {
                Fail(message);
            }
        }

        public static void AreEqual(object expected, object actual, string message)
        {
            if (!(expected.Equals(actual)))
            {
                Fail(message);
            }
        }

        public static void AreEqualOrdered<T>(IList<T> actual, params T[] expected)
        {
            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        public static void AreEqualUnordered<T>(ICollection<T> actual, params T[] expected)
        {
            AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                IsTrue(actual.Contains(expected[i]));
            }
        }
        
        public static void AreNotEqual(decimal expected, decimal actual)
        {
            AreNotEqual(expected, actual, "Should not be equal");
        }

        public static void AreNotEqual(int expected, int actual)
        {
            AreNotEqual(expected, actual, "Should not be equal");
        }

        public static void AreNotEqual(uint expected, uint actual)
        {
            AreNotEqual(expected, actual, "Should not be equal");
        }

        public static void AreNotEqual(string expected, string actual)
        {
            AreNotEqual(expected, actual, "Should not be equal");
        }

        public static void AreNotEqual(float expected, float actual)
        {
            AreNotEqual(expected, actual, "Should not be equal");
        }
        
        public static void AreNotEqual(double expected, double actual)
        {
            AreNotEqual(expected, actual, "Should not be equal");
        }

        public static void AreNotEqual(object expected, object actual)
        {
            AreNotEqual(expected, actual, "Should not be equal");
        }

        public static void AreNotEqual(decimal expected, decimal actual, string message)
        {
            if (expected == actual)
            {
                Fail(message);
            }
        }

        public static void AreNotEqual(int expected, int actual, string message)
        {
            if (expected == actual)
            {
                Fail(message);
            }
        }

        public static void AreNotEqual(uint expected, uint actual, string message)
        {
            if (expected == actual)
            {
                Fail(message);
            }
        }

        public static void AreNotEqual(string expected, string actual, string message)
        {
            if (expected == actual)
            {
                Fail(message);
            }
        }

        public static void AreNotEqual(float expected, float actual, string message)
        {
            if (expected == actual)
            {
                Fail(message);
            }
        }

        public static void AreNotEqual(double expected, double actual, string message)
        {
            if (expected == actual)
            {
                Fail(message);
            }
        }

        public static void AreNotEqual(object expected, object actual, string message)
        {
            if (object.Equals(expected, actual))
            {
                Fail(message);
            }
        }

        public static void AreSame(object expected, object actual)
        {
            AreSame(expected, actual, "Objects are not the same");
        }

        public static void AreSame(object expected, object actual, string message)
        {
            if (!(object.ReferenceEquals(expected, actual)))
            {
                Assert.Fail(message);
            }
        }

        public static void AreNotSame(object expected, object actual)
        {
            AreNotSame(expected, actual, "Objects are the same");
        }

        public static void AreNotSame(object expected, object actual, string message)
        {
            if (object.ReferenceEquals(expected, actual))
            {
                Assert.Fail(message);
            }
        }

        public static void Fail()
        {
            Fail("Please call Fail(message) not Fail()");
        }

        public static void Fail(string message)
        {
            throw new InvalidOperationException(message);
        }

        public static void IsEmpty(ICollection collection)
        {
            IsEmpty(collection, "Collection is not empty");
        }
        
        public static void IsEmpty(ICollection collection, string message)
        {
            if (collection != null)
            {
                AreEqual(0, collection.Count, message);
            }
        }
        
        public static void IsFalse(bool condition)
        {
            IsFalse(condition, "Is not False");
        }

        public static void IsFalse(bool condition, string message)
        {
            if (condition)
            {
                Assert.Fail(message);
            }
        }

        public static void IsNotNull(object o)
        {
            IsNotNull(o, "Object is null");
        }

        public static void IsNotNull(object o, string message)
        {
            if (o == null)
            {
                Assert.Fail(message);
            }
        }

        public static void IsNull(object o)
        {
            IsNull(o, "Reference is not null");
        }

        public static void IsNull(object o, string message)
        {
            if (o != null)
            {
                Assert.Fail(message);
            }
        }

        public static void IsTrue(bool condition)
        {
            IsTrue(condition, "is not True");
        }

        public static void IsTrue(bool condition, string message)
        {
            AreEqual(true, condition, message);
        }

        public static void IsInstanceOfType(Type expected, object actual)
        {
            IsInstanceOfType(expected, actual, String.Format("Object is not an instance of type '{0}'", expected.ToString()));
        }

        public static void IsInstanceOfType(Type expected, object actual, string message)
        {
            if (!(expected.IsInstanceOfType(actual)))
            {
                Assert.Fail(message);
            }
        }
    }
}
