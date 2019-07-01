// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    public abstract class TextBoxBaseFactory<T> : DiscoverableFactory<T> where T : TextBoxBase
    {
        protected void ApplyTextBoxBaseProperties(T textBoxBase, DeterministicRandom random)
        {
            textBoxBase.SpellCheck.IsEnabled = random.NextBool();
            textBoxBase.SpellCheck.SpellingReform = random.NextEnum<SpellingReform>();
        }
    }
}
