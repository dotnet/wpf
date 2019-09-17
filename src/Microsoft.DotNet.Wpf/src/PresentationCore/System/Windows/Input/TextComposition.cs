// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: TextComposition class is the object that contains
//              the input text. The text from keyboard input
//              is packed in this class when TextInput event is generated.
//              And this class also packs the state of the composition text when
//              the input text is being composed (for EA input, Speech).
//
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Security; 
using MS.Win32;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    //------------------------------------------------------
    //
    //  TextCompositionAutoComplete enum
    //
    //------------------------------------------------------

    /// <summary>
    /// The switch for automatic termination of the text composition
    /// </summary>
    public enum TextCompositionAutoComplete
    {
        /// <summary>
        /// AutomaticComplete is off.
        /// </summary>
        Off = 0,

        /// <summary>
        /// AutomaticComplete is on. 
        /// TextInput event will be generated automatically by TextCompositionManager after
        /// TextInputStart event is processed.
        /// </summary>
        On  = 1,
    }

    internal enum TextCompositionStage
    {
        /// <summary>
        /// The composition is not started yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// The composition has started.
        /// </summary>
        Started  = 1,

        /// <summary>
        /// The composition has completed or canceled.
        /// </summary>
        Done  = 2,
    }

    /// <summary>
    ///     Text Composition class contains the result text of the text input and the state of the composition text.
    /// </summary>
    public class TextComposition : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     The constrcutor of TextComposition class.
        /// </summary>
        public TextComposition(InputManager inputManager, IInputElement source, string resultText) : this(inputManager, source,  resultText, TextCompositionAutoComplete.On)
        {
        }

        /// <summary>
        ///     The constrcutor of TextComposition class.
        /// </summary>
        public TextComposition(InputManager inputManager, IInputElement source, string resultText, TextCompositionAutoComplete autoComplete) : this(inputManager, source, resultText, autoComplete, InputManager.Current.PrimaryKeyboardDevice)
        {
            // We should avoid using Enum.IsDefined for performance and correct versioning. 
            if ((autoComplete != TextCompositionAutoComplete.Off) &&
                (autoComplete != TextCompositionAutoComplete.On))
            {
                throw new InvalidEnumArgumentException("autoComplete", 
                                                       (int)autoComplete, 
                                                       typeof(TextCompositionAutoComplete));
            }
        }

        //
        // An internal constructore to specify InputDevice directly.
        //
        internal TextComposition(InputManager inputManager, IInputElement source, string resultText, TextCompositionAutoComplete autoComplete, InputDevice inputDevice)
        {
            _inputManager = inputManager;

            _inputDevice = inputDevice;

            if (resultText == null)
            {
                throw new ArgumentException(SR.Get(SRID.TextComposition_NullResultText));
            }

            _resultText = resultText;
            _compositionText = "";
            _systemText = "";
            _systemCompositionText = "";
            _controlText = "";

            _autoComplete = autoComplete;

            _stage = TextCompositionStage.None;

            // source of this text composition.
            _source = source;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods 
        //
        //------------------------------------------------------

        /// <summary>
        ///     Finalize the composition.
        /// </summary>
        public virtual void Complete()
        {
//             VerifyAccess();

            TextCompositionManager.CompleteComposition(this);
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     The result text of the text input.
        /// </summary>
        [CLSCompliant(false)]
        public string Text
        {
            get 
            {
//                 VerifyAccess();
                return _resultText;
            }
            protected set 
            {
//                 VerifyAccess();
                _resultText = value;
            }
        }

        /// <summary>
        ///     The current composition text.
        /// </summary>
        [CLSCompliant(false)]
        public string CompositionText
        {
            get 
            {
//                 VerifyAccess();
                return _compositionText;
            }

            protected set 
            {
//                 VerifyAccess();
                _compositionText = value;
            }
        }

        /// <summary>
        ///     The current system text.
        /// </summary>
        [CLSCompliant(false)]
        public string SystemText
        {
            get 
            {
//                 VerifyAccess();
                return _systemText;
            }

            protected set 
            {
//                 VerifyAccess();
                _systemText = value;
            }
        }

        /// <summary>
        ///     The current system text.
        /// </summary>
        [CLSCompliant(false)]
        public string ControlText
        {
            get 
            {
//                 VerifyAccess();
                return _controlText;
            }

            protected set 
            {
//                 VerifyAccess();
                _controlText = value;
            }
        }

        /// <summary>
        ///     The current system text.
        /// </summary>
        [CLSCompliant(false)]
        public string SystemCompositionText
        {
            get 
            {
//                 VerifyAccess();
                return _systemCompositionText;
            }

            protected set 
            {
//                 VerifyAccess();
                _systemCompositionText = value;
            }
        }

        /// <summary>
        ///     The switch for automatic termination.
        /// </summary>
        public TextCompositionAutoComplete AutoComplete
        {
            get 
            {
//                 VerifyAccess();
                return _autoComplete;
            }
        }


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     The current composition text.
        /// </summary>
        internal void SetText(string resultText)
        {
            _resultText = resultText;
        }

        /// <summary>
        ///     The current composition text.
        /// </summary>
        internal void SetCompositionText(string compositionText)
        {
            _compositionText = compositionText;
        }


        /// <summary>
        ///     Convert this composition to system composition.
        /// </summary>
        internal void MakeSystem()
        {
            _systemText = _resultText;
            _systemCompositionText = _compositionText;

            _resultText = "";
            _compositionText = "";
            _controlText = "";
        }

        /// <summary>
        ///     Convert this composition to system composition.
        /// </summary>
        internal void MakeControl()
        {
            // Onlt control char should be in _controlText.
            Debug.Assert((_resultText.Length == 1) && Char.IsControl(_resultText[0]));

            _controlText = _resultText;

            _resultText = "";
            _systemText = "";
            _compositionText = "";
            _systemCompositionText = "";
        }

        /// <summary>
        ///     Clear all the current texts.
        /// </summary>
        internal void ClearTexts()
        {
            _resultText = "";
            _compositionText = "";
            _systemText = "";
            _systemCompositionText = "";
            _controlText = "";
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     The source of this text composition.
        /// </summary>
        internal IInputElement Source
        {
            get 
            {
                return _source;
            }
        }

        // return the input device for this text composition.
        internal InputDevice _InputDevice
        {
            get {return _inputDevice;}
        }

        // return the input manager for this text composition.

        internal InputManager _InputManager
        {
            get 
            {
                return _inputManager;
            }
        }

        // the stage of this text composition
        internal TextCompositionStage Stage
        {
            get {return _stage;}
            set {_stage = value;}
        }

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // InputManager for this TextComposition.
        private readonly InputManager _inputManager;

        // InputDevice for this TextComposition.
        private readonly InputDevice _inputDevice;

        // The finalized and result string.
        private string _resultText;

        // The composition string.
        private string _compositionText;

        // The system string.
        private string _systemText;

        // The control string.
        private string _controlText;

        // The system composition string.
        private string _systemCompositionText;

        // If this is true, TextComposition Manager will terminate the compositon automatically.
        private readonly TextCompositionAutoComplete _autoComplete;

        // TextComposition stage.
        private TextCompositionStage _stage;

        // source of this text composition.
        private IInputElement _source;
    }
}
