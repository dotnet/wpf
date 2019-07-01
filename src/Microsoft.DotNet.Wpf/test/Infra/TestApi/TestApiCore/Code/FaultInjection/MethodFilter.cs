// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection
{
    internal static class MethodFilterHelper
    {
        #region Public Members

        public static void WriteMethodFilter(string file, FaultRule[] rules)
        {
            using (Stream stream = File.Open(file, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    foreach (FaultRule rule in rules)
                    {
                        if (rule != null)
                        {
                            string signature = Signature.ConvertSignature(rule.MethodSignature, SignatureStyle.Com);
                            writer.WriteLine(signature);
                        }
                    }
                }
            }
        }

        #endregion  
    }
}
