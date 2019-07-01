// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.CrossProcess
{
    internal static class DictionaryContract
    {
        public const string DictionaryPipeName = "WpfTestCrossProcessDictionaryPipe";
        public const int DictionaryPipeConnectionTimeoutMs = 1000;
    }
}