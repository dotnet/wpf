// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection.Constants
{
    internal static class XmlErrorMessages
    {
        // Error messages for Expression
        public const string ArrayItemTypeError = "Array item type not compatible with array type";
        public const string ConstructorNotFound = "Constructor for class {0} with specific parameter(s) not found.";
        public const string ExpressionFormatError = "Expression format error";
        public const string NoParser = "No parser for type {0}";
        public const string TargetAssemblyNotFound = "Target assembly {0} not found";
        public const string TargetTypeNotFound = "Target type {0} not found";

        // Error messages for XmlFaultSession
        public const string ElementError = "{0} element is missing or is too many";
        public const string FileNotFound = "Xml file {0} not found. Try using (SomeType){0}";
        public const string IndexOutOfBound = "Specified test case index {0} out of bound";
        public const string SchemaValidationError = "Schema validation error";
        public const string SchemaValidationErrorHeader = "\r\n\tSchema Validation error: ";
        public const string TestCaseNotFound = "Specified test case \"{0}\" not found";
        public const string ThreeItemMessage = "\n   at {0} {1} '{2}'";
        public const string TwoItemMessage = "\n   at {0} {1}";
        public const string XmlFileError = "Xml File {0} error";
    }
}