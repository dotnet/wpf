// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CDF", Scope = "namespace", Target = "Microsoft.Test.CDFInfrastructure")]
[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.BugAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.BugAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.BugAttribute")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.BugAttribute.#BugNumbers")]
[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.MethodAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.MethodAttribute")]
[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.SecurityLevelAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.SecurityLevelAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.SecurityLevelAttribute")]
[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.TestCaseAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.TestCaseAttribute")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestCaseAttribute.#.ctor(Microsoft.Test.CDFInfrastructure.TestCaseAttribute)")]
[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.TestCaseGeneratorAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestCaseGeneratorAttribute.#GetTestCaseGenerator(System.Reflection.MethodInfo)")]
[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.TestCaseMetadataAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.TestCaseMetadataAttribute")]
[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.TestCaseParameterAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Scope = "type", Target = "Microsoft.Test.CDFInfrastructure.TestCaseParameterAttribute")]
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "BVT", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestCategory.#BVT")]
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CIT", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestCategory.#CIT")]
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "IDW", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestCategory.#IDW")]
[module: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestMethodAttribute.#GetTestCaseMetaAttributes(System.Reflection.MethodInfo)")]
[module: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestMethodAttribute.#GetTestCaseParameterAttributes(System.Reflection.MethodInfo)")]
[module: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestMethodAttribute.#GetTestMethodAttribute(System.Reflection.MethodInfo)")]
[module: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Test.CDFInfrastructure.TestMethodAttribute.#GetTestMethodAttributes(System.Reflection.MethodInfo)")]

namespace Microsoft.Test.CDFInfrastructure
{
    /// <summary />
    [Serializable, AttributeUsage(AttributeTargets.Method)]
    public class MethodAttribute : Attribute
    {
        // Fields
        private string author;
        private string description;
        private string owner;
        private string title;

        // Methods
        /// <summary />
        public MethodAttribute()
            : this(null, null, null)
        {
        }

        /// <summary />
        public MethodAttribute(string title)
            : this(title, null, null)
        {
        }

        /// <summary />
        public MethodAttribute(string title, string description)
            : this(title, description, null)
        {
        }

        /// <summary />
        public MethodAttribute(string title, string description, string author)
        {
            this.title = title;
            this.description = description;
            this.author = author;
        }

        // Properties
        /// <summary />
        public string Author
        {
            get
            {
                return this.author;
            }
            set
            {
                this.author = value;
            }
        }

        /// <summary />
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }

        /// <summary />
        public string Owner
        {
            get
            {
                return this.owner;
            }
            set
            {
                this.owner = value;
            }
        }

        /// <summary />
        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                this.title = value;
            }
        }
    }

    /// <summary />
    [Serializable, AttributeUsage(AttributeTargets.All)]
    public abstract class TestMethodAttribute : MethodAttribute
    {
        // Fields
        private string displayName = string.Empty;

        // Methods
        /// <summary />
        protected TestMethodAttribute()
        {
        }

        /// <summary />
        public static bool ContainsTestMethodAttribute(MethodInfo method)
        {
            return (GetTestMethodAttributes(method).Length > 0);
        }

        /// <summary />
        public static TestCaseMetadataAttribute[] GetTestCaseMetaAttributes(MethodInfo method)
        {
            return (TestCaseMetadataAttribute[])method.GetCustomAttributes(typeof(TestCaseMetadataAttribute), true);
        }

        /// <summary />
        public static TestCaseParameterAttribute[] GetTestCaseParameterAttributes(MethodInfo method)
        {
            return (TestCaseParameterAttribute[])method.GetCustomAttributes(typeof(TestCaseParameterAttribute), true);
        }

        /// <summary />
        public static TestMethodAttribute GetTestMethodAttribute(MethodInfo method)
        {
            TestMethodAttribute[] customAttributes = null;
            customAttributes = (TestMethodAttribute[])method.GetCustomAttributes(typeof(TestMethodAttribute), true);
            if (customAttributes.Length > 0)
            {
                return customAttributes[0];
            }
            return null;
        }

        /// <summary />
        public static TestMethodAttribute[] GetTestMethodAttributes(MethodInfo method)
        {
            return (TestMethodAttribute[])method.GetCustomAttributes(typeof(TestMethodAttribute), true);
        }

        // Properties
        /// <summary />
        public virtual string DisplayName
        {
            get
            {
                return this.displayName;
            }
            set
            {
                this.displayName = value;
            }
        }
    }

    /// <summary />
    [Serializable, AttributeUsage(AttributeTargets.All)]
    public class TestCaseAttribute : TestMethodAttribute
    {
        // Fields
        private long expectedDuration;
        private string onExceptionMethod;
        private string testCaseCleanupMethod;
        private string testCaseStartupMethod;
        private int testCaseTimeoutPeriod;
        private TestCategory testCategory;
        private TestLevel testLevel;
        private TestState testState;
        private TestType testType;
        private string keywords;

        // Methods
        /// <summary />
        public TestCaseAttribute()
        {
            this.testCategory = TestCategory.BVT;
            this.testCaseTimeoutPeriod = -1;
        }

        /// <summary />
        public TestCaseAttribute(TestCaseAttribute att)
        {
            this.testCategory = TestCategory.BVT;
            this.testCaseTimeoutPeriod = -1;
            base.Author = att.Author;
            this.Category = att.Category;
            this.Cleanup = att.Cleanup;
            base.Description = att.Description;
            this.DisplayName = att.DisplayName;
            this.ExpectedDuration = att.ExpectedDuration;
            this.Level = att.Level;
            base.Owner = att.Owner;
            this.Startup = att.Startup;
            this.TestState = att.TestState;
            this.TestType = att.TestType;
            this.Timeout = att.Timeout;
            base.Title = att.Title;
            this.OnException = att.OnException;
            this.Keywords = att.Keywords;
        }

        // Properties

        /// <summary />
        public virtual string Keywords
        {
            get
            {
                return this.keywords;
            }
            set
            {
                this.keywords = value;
            }
        }

        /// <summary />
        public virtual TestCategory Category
        {
            get
            {
                return this.testCategory;
            }
            set
            {
                this.testCategory = value;
            }
        }

        /// <summary />
        public virtual string Cleanup
        {
            get
            {
                return this.testCaseCleanupMethod;
            }
            set
            {
                this.testCaseCleanupMethod = value;
            }
        }

        /// <summary />
        public virtual long ExpectedDuration
        {
            get
            {
                return this.expectedDuration;
            }
            set
            {
                this.expectedDuration = value;
            }
        }

        /// <summary />
        public virtual TestLevel Level
        {
            get
            {
                return this.testLevel;
            }
            set
            {
                this.testLevel = value;
            }
        }

        /// <summary />
        public virtual string OnException
        {
            get
            {
                return this.onExceptionMethod;
            }
            set
            {
                this.onExceptionMethod = value;
            }
        }

        /// <summary />
        public virtual string Startup
        {
            get
            {
                return this.testCaseStartupMethod;
            }
            set
            {
                this.testCaseStartupMethod = value;
            }
        }

        /// <summary />
        public virtual TestState TestState
        {
            get
            {
                return this.testState;
            }
            set
            {
                this.testState = value;
            }
        }

        /// <summary />
        public virtual TestType TestType
        {
            get
            {
                return this.testType;
            }
            set
            {
                this.testType = value;
            }
        }

        /// <summary />
        public virtual int Timeout
        {
            get
            {
                return this.testCaseTimeoutPeriod;
            }
            set
            {
                this.testCaseTimeoutPeriod = value;
            }
        }
    }

    /// <summary />
    [Serializable, AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class TestCaseMetadataAttribute : Attribute
    {
        // Fields
        private string paraName;
        private string paraValue;

        // Methods
        /// <summary />
        public TestCaseMetadataAttribute()
        {
        }

        /// <summary />
        public TestCaseMetadataAttribute(TestCaseMetadataAttribute att)
        {
            this.paraName = att.Name;
            this.paraValue = att.Value;
        }

        /// <summary />
        public TestCaseMetadataAttribute(string name, string value)
        {
            this.paraName = name;
            this.paraValue = value;
        }

        // Properties
        /// <summary />
        public virtual string Name
        {
            get
            {
                return this.paraName;
            }
            set
            {
                this.paraName = value;
            }
        }

        /// <summary />
        public virtual string Value
        {
            get
            {
                return this.paraValue;
            }
            set
            {
                this.paraValue = value;
            }
        }
    }

    /// <summary />
    [Serializable, AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class TestCaseParameterAttribute : Attribute
    {
        // Fields
        private string name;
        private string value;

        // Methods
        /// <summary />
        public TestCaseParameterAttribute()
        {
        }

        /// <summary />
        public TestCaseParameterAttribute(TestCaseParameterAttribute att)
        {
            this.name = att.Name;
            this.value = att.Value;
        }

        /// <summary />
        public TestCaseParameterAttribute(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        // Properties
        /// <summary />
        public virtual string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        /// <summary />
        public virtual string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
            }
        }
    }

    /// <summary />
    [Serializable, AttributeUsage(AttributeTargets.Method)]
    public class TestCaseGeneratorAttribute : Attribute
    {
        // Methods
        /// <summary />
        public static TestCaseGeneratorAttribute GetTestCaseGenerator(MethodInfo method)
        {
            object[] customAttributes = null;
            customAttributes = method.GetCustomAttributes(typeof(TestCaseGeneratorAttribute), true);
            if (customAttributes.Length > 0)
            {
                return (customAttributes[0] as TestCaseGeneratorAttribute);
            }
            return null;
        }
    }

    /// <summary />
    public enum SecurityLevel
    {
        /// <summary />
        PartialTrust,
        /// <summary />
        FullTrust
    }

    /// <summary />
    public class SecurityLevelAttribute : Attribute
    {
        /// <summary />
        public SecurityLevel SecurityLevel { get; set; }

        /// <summary />
        public SecurityLevelAttribute(SecurityLevel securityLevel)
        {
            this.SecurityLevel = securityLevel;
        }
    }

    /// <summary />
    public class BugAttribute : Attribute
    {
        /// <summary />
        public List<int> BugNumbers { get; set; }

        /// <summary />
        public string Source { get; set; }

        /// <summary />
        public BugAttribute() { }

        /// <summary />
        public BugAttribute(string source, params int[] bugNumber)
        {
            this.Source = source;
            this.BugNumbers = new List<int>();
            for (int i = 0; i < bugNumber.Length; i++)
            {
                this.BugNumbers.Add(bugNumber[i]);
            }
        }
    }

    /// <summary />
    public enum TestCategory
    {
        /// <summary />
        BVT = 1,
        /// <summary />
        CIT = 6,
        /// <summary />
        Default = 1,
        /// <summary />
        Functional = 3,
        /// <summary />
        IDW = 2,
        /// <summary />
        Performance = 4,
        /// <summary />
        Stress = 5,
        /// <summary />
        Unit = 0
    }

    /// <summary />
    public enum TestLevel
    {
        /// <summary />
        Default = 0,
        /// <summary />
        One = 1,
        /// <summary />
        Three = 3,
        /// <summary />
        Two = 2,
        /// <summary />
        Zero = 0
    }

    /// <summary />
    public enum TestType
    {
        /// <summary />
        Automated = 0,
        /// <summary />
        BlockedProductIssue = 4,
        /// <summary />
        Default = 0,
        /// <summary />
        FutureMilestone = 3,
        /// <summary />
        Manual = 1,
        /// <summary />
        NotComplete = 2
    }

    /// <summary />
    public enum TestState
    {
        /// <summary />
        Active = 0,
        /// <summary />
        Default = 0,
        /// <summary />
        NotInMilestone = 1,
        /// <summary />
        Obsolete = 2
    }
}
