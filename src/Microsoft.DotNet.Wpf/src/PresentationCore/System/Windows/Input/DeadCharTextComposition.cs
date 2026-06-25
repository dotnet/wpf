// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
// Description: the DeadCharTextComposition class
//
//

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
