// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Indicates an error in the generated internal variation table prevents completion of generation.
    /// This indicates a bug in internal generation engine.
    /// </summary>
    [Serializable]
    internal class InternalVariationGenerationException : Exception
    {
        public InternalVariationGenerationException()
            : base()
        {
        }

        public InternalVariationGenerationException(string message)
            : base(message)
        {
        }

        protected InternalVariationGenerationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
