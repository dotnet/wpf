// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: the DeadCharTextComposition class
//
//

using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Security; 
using MS.Win32;
using Microsoft.Win32; // for RegistryKey class

namespace System.Windows.Input
{
    /// <summary>
    ///     the DeadCharTextComposition class is the composition object for Dead key scequence.
    /// </summary>
    internal sealed class DeadCharTextComposition : TextComposition
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DeadCharTextComposition(InputManager inputManager, IInputElement source, string text, TextCompositionAutoComplete autoComplete, InputDevice inputDevice) : base(inputManager, source, text, autoComplete, inputDevice)
        {
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  internal Properties
        //
        //------------------------------------------------------

        internal bool Composed
        {
            get {return _composed;}
            set {_composed = value;}
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // If this is true, the text has been composed with actual char.
        private bool _composed;
    }
}
