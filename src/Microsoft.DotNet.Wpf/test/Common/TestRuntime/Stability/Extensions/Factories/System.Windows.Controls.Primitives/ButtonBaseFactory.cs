// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete Button factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ButtonBaseFactory<T> : ContentControlFactory<T> where T : ButtonBase
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an object to set Button CommandParameter property.
        /// </summary>
        public Object CommandParameter { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common button properties.
        /// </summary>
        /// <param name="buttonBase"></param>
        /// <param name="random"></param>
        protected void ApplyButtonBaseProperties(T buttonBase, DeterministicRandom random)
        {
            ApplyContentControlProperties(buttonBase);
            buttonBase.ClickMode = random.NextEnum<ClickMode>();
            buttonBase.Command = random.NextStaticProperty<RoutedUICommand>(typeof(ApplicationCommands));
            buttonBase.CommandParameter = CommandParameter;
            buttonBase.Click += new RoutedEventHandler(ButtonBaseClick);
        }

        #endregion

        #region Private Events

        private void ButtonBaseClick(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("ButtonBase was clicked.");
        }

        #endregion
    }
}
