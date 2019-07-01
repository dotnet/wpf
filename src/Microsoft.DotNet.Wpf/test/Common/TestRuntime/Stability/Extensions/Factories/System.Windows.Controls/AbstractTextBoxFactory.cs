// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete TextBox factory.
    /// </summary>
    /// <typeparam name="TextBoxType"></typeparam>
    [TargetTypeAttribute(typeof(TextBox))]
    internal abstract class AbstractTextBoxFactory<TextBoxType> : TextBoxBaseFactory<TextBoxType> where TextBoxType : TextBox
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a string to set TextBox Text property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Text { get; set; }

        /// <summary>
        /// Gets or sets a value to set TextBox MinLines property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MinLines { get; set; }

        /// <summary>
        /// Gets or sets a value to set TextBox MaxLength property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MaxLength { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common TextBox properties.
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="random"></param>
        protected void ApplyTextBoxProperties(TextBoxType textBox, DeterministicRandom random)
        {
            ApplyTextBoxBaseProperties(textBox, random);
            textBox.MinLines = MinLines;
            textBox.MaxLines = MinLines + random.Next(50);
            textBox.MaxLength = MaxLength;
            textBox.Text = Text;
        }

        #endregion
    }
}
