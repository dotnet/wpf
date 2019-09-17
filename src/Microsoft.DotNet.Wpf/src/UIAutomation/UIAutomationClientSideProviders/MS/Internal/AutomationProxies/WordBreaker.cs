// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/***************************************************************************\
*
* Description:
* Plain text word breaker to support the word text unit in Win32 edit controls.
*
* NLG supplied Avalon Text a DLL called ProofingService which included a 
* WordBreaker class. The WordBreaker class uses efficient heuristics
* for determining word boundaries for western languages.
* For Japanese and Chinese it uses System.NaturalLanguage.dll to
* determine word boundaries.
*
* We can't use the ProofingService DLL directly because it depends on Avalon Text 
* types which live in Framework. UIA does not want Avalon dependencies beyond Base 
* so we can port to downlevel platforms. 
*
* So we resort to cut-and-paste reusability. Although we could save some code if we
* refactored it for our specific use we would then have diverging code bases for 
* the same function and fixes in one code tree wouldn't map to fixes in the other.
* So we declare our own trivial versions of Avalon Text types TextContainer, 
* TextPosition, and TextNavigator that are sufficient to use the WordBreaker class 
* directly.
*
\***************************************************************************/

using System;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Collections;
using System.NaturalLanguage;

namespace MS.Internal.AutomationProxies
{
    #region Stand-in classes for Avalon Text Types

    // These are the Avalon Text types that the WordBreaker class uses. We have trivial versions that wraps a string for
    // a document and an integer for a position within a document. There are some other auxillary types necessary to get
    // code to compile.

    // our TextContainer simply wraps a string. The contents of the string are the entire document.
    internal class TextContainer
    {
        public TextContainer(string text)
        {
            _text = text;
        }

        public event TextContainerChangedEventHandler TextChanged;

        public TextPosition Start
        {
            get
            {
                return new TextPosition(0, this);
            }
        }

        public TextPosition End
        {
            get
            {
                return new TextPosition(_text.Length, this);
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }
        }

        // this exists to solely to prevent "error CS0067: Warning as Error: The event 'MS.Internal.AutomationProxies.TextContainer.TextChanged' is never used"
        private void BogusFunctionToAvoidWarning(object target, TextContainerChangedEventArgs args)
        {
            TextChanged += new TextContainerChangedEventHandler(BogusFunctionToAvoidWarning);
        }

        private string _text;
    }

    // our TextPosition wraps an integer indicating a position in the string.
    internal class TextPosition : IComparable
    {
        public TextPosition(int position, TextContainer container)
        {
            _position = position;
            _container = container;
        }

        public TextPosition(TextPosition position)
            : this(position._position, position._container)
        { }

        public static bool operator <(TextPosition position1, TextPosition position2)
        {
            return position1._position < position2._position;
        }

        public static bool operator >(TextPosition position1, TextPosition position2)
        {
            return position1._position > position2._position;
        }

        public int Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        public TextContainer TextContainer
        {
            get
            {
                return _container;
            }
        }

        public TextNavigator CreateNavigator()
        {
            return new TextNavigator(this);
        }

        public int CompareTo(object obj)
        {
            TextPosition other = (TextPosition)obj;
            return _position - other._position;
        }

        public int GetDistanceTo(TextPosition pos)
        {
            return pos._position - _position;
        }

        public Type GetElementType(LogicalDirection direction)
        {
            throw new NotImplementedException();
        }

        public TextSymbolType GetSymbolType(LogicalDirection direction)
        {
            // everything is a character except beyond the ends of the file.
            return (_position <= 0 && direction == LogicalDirection.Backward)
                || (_position >= _container.Text.Length && direction == LogicalDirection.Forward) ?
                TextSymbolType.None :
                TextSymbolType.Character;
        }

        public int GetText(LogicalDirection direction, int maxLength, TextPosition limit, char[] chars, int startIndex)
        {
            // simplifying assumptions based on usage by word breaker.
            Debug.Assert(direction == LogicalDirection.Forward);
            Debug.Assert(maxLength == 1);
            Debug.Assert(limit._position == _container.Text.Length);
            Debug.Assert(_position < _container.Text.Length);
            Debug.Assert(startIndex == 0);

            chars[0] = _container.Text[_position];
            return 1;
        }

        public int GetTextLength(LogicalDirection direction)
        {
            // simplifying assumptions based on usage by word breaker.
            Debug.Assert(direction == LogicalDirection.Forward);

            return _container.End._position - _position;
        }

        protected TextContainer _container;
        protected int _position;
    }

    // a TextNavigator inherits from TextPosition and adds a couple functions for moving around.
    internal class TextNavigator : TextPosition
    {
        public TextNavigator(int position, TextContainer container)
            : base(position, container)
        { }

        public TextNavigator(TextPosition position)
            : base(position)
        { }

        public TextPosition CreatePosition()
        {
            return new TextPosition(this);
        }

        public void Move(LogicalDirection direction)
        {
            // this should never get called.
            throw new NotImplementedException();
        }

        public void MoveToPosition(TextPosition position)
        {
            _position = position.Position;
        }

        public void MoveByDistance(int distance)
        {
            // throw ArgumentException if position would be moved out of bounds.
            int newPosition = _position + distance;
            if (newPosition < 0 || newPosition > _container.Text.Length)
            {
                throw new System.ArgumentException();
            }

            _position = newPosition;
        }
    }

    // the following additional Avalon Text types are required so the WordBreaker class will compile as-is.
    internal delegate void TextContainerChangedEventHandler(object target, TextContainerChangedEventArgs args);
    internal class TextContainerChangedEventArgs 
    {
        // To appease the FxCop
        private TextContainerChangedEventArgs()
        { }
    }
    internal class InlineElement
    {
        // To appease the FxCop
        private InlineElement()
        { }
    }
    internal enum LogicalDirection  { Forward, Backward }
    internal enum TextSymbolType { Character, EmbeddedObject, ElementStart, ElementEnd, None }

    #endregion Stand-in classes for Avalon Text Types

    // THIS WORDBREAKER CLASS IS IDENTICAL TO THE WORDBREAKER CLASS IN PROOFINGSERVICES DLL 
    // EXCEPT IT HAS BEEN CHANGED FROM PUBLIC TO INTERNAL. DO NOT MODIFY THIS CLASS. INSTEAD,
    // REPORT BUGS TO NLG AND GET A FIXED VERSION FROM THEM.

    internal class WordBreaker
    {
        #region Constants
        private const int DefaultWindowSize = 1;
        private const char CharObject = '\xe000';  // Private Use = E000F8FF
        #endregion

        #region Variables
        bool includeWhiteSpaceBefore = false;
        bool includeWhiteSpaceAfter = true;
        char[] buffer;

        static readonly byte[] lexTable = new byte[] {
            0x00, // 00        
            0x00, // 01        
            0x00, // 02        
            0x00, // 03        
            0x00, // 04        
            0x00, // 05        
            0x00, // 06        
            0x00, // 07        
            0x00, // 08        
            0x06, // 09    X   
            0x04, // 0a    X   
            0x00, // 0b    X   
            0x00, // 0c    X   
            0x05, // 0d    X   
            0x00, // 0e        
            0x00, // 0f        
            0x00, // 10        
            0x00, // 11        
            0x00, // 12        
            0x00, // 13        
            0x00, // 14        
            0x00, // 15        
            0x00, // 16        
            0x00, // 17        
            0x00, // 18        
            0x00, // 19        
            0x00, // 1a        
            0x00, // 1b        
            0x00, // 1c        
            0x00, // 1d        
            0x00, // 1e        
            0x00, // 1f        
            0x06, // 20    X   
            0x03, // 21     P   (!)
            0x03, // 22     P   (")
            0x03, // 23     P   (#)
            0x03, // 24     P   ($)
            0x03, // 25     P   (%)
            0x03, // 26     P   (&)
            0x02, // 27     PE  (')
            0x03, // 28     P   (()
            0x03, // 29     P   ())
            0x03, // 2a     P   (*)
            0x03, // 2b     P   (+)
            0x03, // 2c     P   (,)
            0x03, // 2d     PE  (-)
            0x03, // 2e     PE  (.)
            0x03, // 2f     PE  (/)
            0x01, // 30   D  E  (0)
            0x01, // 31   D  E  (1)
            0x01, // 32   D  E  (2)
            0x01, // 33   D  E  (3)
            0x01, // 34   D  E  (4)
            0x01, // 35   D  E  (5)
            0x01, // 36   D  E  (6)
            0x01, // 37   D  E  (7)
            0x01, // 38   D  E  (8)
            0x01, // 39   D  E  (9)
            0x03, // 3a     P   (:)
            0x03, // 3b     P   (;)
            0x03, // 3c     P   (<)
            0x03, // 3d     P   (=)
            0x03, // 3e     P   (>)
            0x03, // 3f     P   (?)
            0x03, // 40     P   (@)
            0x01, // 41 U    EV (A)
            0x01, // 42 U    E  (B)
            0x01, // 43 U    E  (C)
            0x01, // 44 U    E  (D)
            0x01, // 45 U    EV (E)
            0x01, // 46 U    E  (F)
            0x01, // 47 U    E  (G)
            0x01, // 48 U    E  (H)
            0x01, // 49 U    EV (I)
            0x01, // 4a U    E  (J)
            0x01, // 4b U    E  (K)
            0x01, // 4c U    E  (L)
            0x01, // 4d U    E  (M)
            0x01, // 4e U    E  (N)
            0x01, // 4f U    EV (O)
            0x01, // 50 U    E  (P)
            0x01, // 51 U    E  (Q)
            0x01, // 52 U    E  (R)
            0x01, // 53 U    E  (S)
            0x01, // 54 U    E  (T)
            0x01, // 55 U    EV (U)
            0x01, // 56 U    E  (V)
            0x01, // 57 U    E  (W)
            0x01, // 58 U    E  (X)
            0x01, // 59 U    E  (Y)
            0x01, // 5a U    E  (Z)
            0x03, // 5b     P   ([)
            0x03, // 5c     P   (\)
            0x03, // 5d     P   (])
            0x03, // 5e     P   (^)
            0x03, // 5f     P   (_)
            0x03, // 60     P   (`)
            0x01, // 61  L   EV (a)
            0x01, // 62  L   E  (b)
            0x01, // 63  L   E  (c)
            0x01, // 64  L   E  (d)
            0x01, // 65  L   EV (e)
            0x01, // 66  L   E  (f)
            0x01, // 67  L   E  (g)
            0x01, // 68  L   E  (h)
            0x01, // 69  L   EV (i)
            0x01, // 6a  L   E  (j)
            0x01, // 6b  L   E  (k)
            0x01, // 6c  L   E  (l)
            0x01, // 6d  L   E  (m)
            0x01, // 6e  L   E  (n)
            0x01, // 6f  L   EV (o)
            0x01, // 70  L   E  (p)
            0x01, // 71  L   E  (q)
            0x01, // 72  L   E  (r)
            0x01, // 73  L   E  (s)
            0x01, // 74  L   E  (t)
            0x01, // 75  L   EV (u)
            0x01, // 76  L   E  (v)
            0x01, // 77  L   E  (w)
            0x01, // 78  L   E  (x)
            0x01, // 79  L   E  (y)
            0x01, // 7a  L   E  (z)
            0x03, // 7b     P   ({)
            0x03, // 7c     P   (|)
            0x03, // 7d     P   (})
            0x03, // 7e     P   (~)
            0x00, // 7f         ()
            /*
            0x01, // 80     P   (�)
            0x00, // 81         (�)
            0x00, // 82         (�)
            0x00, // 83         (�)
            0x00, // 84         (�)
            0x00, // 85         (�)
            0x00, // 86         (�)
            0x00, // 87         (�)
            0x00, // 88         (�)
            0x00, // 89         (�)
            0x00, // 8a         (�)
            0x00, // 8b         (�)
            0x00, // 8c         (�)
            0x00, // 8d         (�)
            0x00, // 8e         (�)
            0x00, // 8f         (�)
            0x00, // 90         (�)
            0x00, // 91         (�)
            0x00, // 92         (�)
            0x00, // 93         (�)
            0x00, // 94         (�)
            0x00, // 95         (�)
            0x00, // 96         (�)
            0x00, // 97         (�)
            0x00, // 98         (�)
            0x00, // 99         (�)
            0x00, // 9a         (�)
            0x00, // 9b         (�)
            0x00, // 9c         (�)
            0x00, // 9d         (�)
            0x00, // 9e         (�)
            0x00, // 9f         (�)
            0x00, // a0    X    (�)
            0x00, // a1     P   (�)
            0x00, // a2     P   (�)
            0x00, // a3     P   (�)
            0x00, // a4     P   (�)
            0x00, // a5     P   (�)
            0x00, // a6     P   (�)
            0x00, // a7     P   (�)
            0x00, // a8     P   (�)
            0x00, // a9     P   (�)
            0x00, // aa     P   (�)
            0x00, // ab     P   (�)
            0x00, // ac     P   (�)
            0x00, // ad     P   (�)
            0x00, // ae     P   (�)
            0x00, // af     P   (�)
            0x00, // b0     P   (�)
            0x00, // b1     P   (�)
            0x00, // b2   D PE  (�)
            0x00, // b3   D PE  (�)
            0x00, // b4     P   (�)
            0x00, // b5     P   (�)
            0x00, // b6     P   (�)
            0x00, // b7     P   (�)
            0x00, // b8     P   (�)
            0x00, // b9   D PE  (�)
            0x00, // ba     P   (�)
            0x00, // bb     P   (�)
            0x00, // bc   D PE  (�)
            0x00, // bd   D PE  (�)
            0x00, // be   D PE  (�)
            0x00, // bf     P   (�)
            0x00, // c0 U    EV (�)
            0x00, // c1 U    EV (�)
            0x00, // c2 U    EV (�)
            0x00, // c3 U    EV (�)
            0x00, // c4 U    EV (�)
            0x00, // c5 U    EV (�)
            0x00, // c6 U    E  (�)
            0x00, // c7 U    E  (�)
            0x00, // c8 U    EV (�)
            0x00, // c9 U    EV (�)
            0x00, // ca U    EV (�)
            0x00, // cb U    EV (�)
            0x00, // cc U    EV (�)
            0x00, // cd U    EV (�)
            0x00, // ce U    EV (�)
            0x00, // cf U    EV (�)
            0x00, // d0 U    E  (�)
            0x00, // d1 U    E  (�)
            0x00, // d2 U    EV (�)
            0x00, // d3 U    EV (�)
            0x00, // d4 U    EV (�)
            0x00, // d5 U    EV (�)
            0x00, // d6 U    EV (�)
            0x00, // d7     P   (�)
            0x00, // d8 U    EV (�)
            0x00, // d9 U    EV (�)
            0x00, // da U    EV (�)
            0x00, // db U    EV (�)
            0x00, // dc U    EV (�)
            0x00, // dd U    E  (�)
            0x00, // de U    E  (�)
            0x00, // df  L   E  (�)
            0x00, // e0  L   EV (�)
            0x00, // e1  L   EV (�)
            0x00, // e2  L   EV (�)
            0x00, // e3  L   EV (�)
            0x00, // e4  L   EV (�)
            0x00, // e5  L   EV (�)
            0x00, // e6  L   E  (�)
            0x00, // e7  L   E  (�)
            0x00, // e8  L   EV (�)
            0x00, // e9  L   EV (�)
            0x00, // ea  L   EV (�)
            0x00, // eb  L   EV (�)
            0x00, // ec  L   EV (�)
            0x00, // ed  L   EV (�)
            0x00, // ee  L   EV (�)
            0x00, // ef  L   EV (�)
            0x00, // f0  L   E  (�)
            0x00, // f1  L   E  (�)
            0x00, // f2  L   EV (�)
            0x00, // f3  L   EV (�)
            0x00, // f4  L   EV (�)
            0x00, // f5  L   EV (�)
            0x00, // f6  L   EV (�)
            0x00, // f7     P   (�)
            0x00, // f8  L   EV (�)
            0x00, // f9  L   EV (�)
            0x00, // fa  L   EV (�)
            0x00, // fb  L   EV (�)
            0x00, // fc  L   EV (�)
            0x00, // fd  L   E  (�)
            0x00, // fe  L   E  (�)
            0x00, // ff  L   E  (�)
            */
        };

        static readonly byte[] lexTable2 = new byte[] {
            0x00, // 00        
            0x03, // 01        
            0x03, // 02        
            0x03, // 03        
            0x03, // 04        
            0x03, // 05        
            0x03, // 06        
            0x02, // 07        
            0x03, // 08        
            0x03, // 09    X   
            0x03, // 0a    X   
            0x03, // 0b    X   
            0x03, // 0c    X   
            0x03, // 0d    X   
            0x03, // 0e        
            0x03, // 0f        
            0x01, // 10        
            0x01, // 11        
            0x01, // 12        
            0x01, // 13        
            0x01, // 14        
            0x01, // 15        
            0x01, // 16        
            0x01, // 17        
            0x01, // 18        
            0x01, // 19        
            0x03, // 1a        
            0x03, // 1b        
            0x03, // 1c        
            0x03, // 1d        
            0x03, // 1e        
            0x03, // 1f        
            0x03, // 20    X   
            0x01, // 21     P   (!)
            0x01, // 22     P   (")
            0x01, // 23     P   (#)
            0x01, // 24     P   ($)
            0x01, // 25     P   (%)
            0x01, // 26     P   (&)
            0x01, // 27     PE  (')
            0x01, // 28     P   (()
            0x01, // 29     P   ())
            0x01, // 2a     P   (*)
            0x01, // 2b     P   (+)
            0x01, // 2c     P   (,)
            0x01, // 2d     PE  (-)
            0x01, // 2e     PE  (.)
            0x01, // 2f     PE  (/)
            0x01, // 30   D  E  (0)
            0x01, // 31   D  E  (1)
            0x01, // 32   D  E  (2)
            0x01, // 33   D  E  (3)
            0x01, // 34   D  E  (4)
            0x01, // 35   D  E  (5)
            0x01, // 36   D  E  (6)
            0x01, // 37   D  E  (7)
            0x01, // 38   D  E  (8)
            0x01, // 39   D  E  (9)
            0x01, // 3a     P   (:)
            0x03, // 3b     P   (;)
            0x03, // 3c     P   (<)
            0x03, // 3d     P   (=)
            0x03, // 3e     P   (>)
            0x03, // 3f     P   (?)
            0x03, // 40     P   (@)
            0x01, // 41 U    EV (A)
            0x01, // 42 U    E  (B)
            0x01, // 43 U    E  (C)
            0x01, // 44 U    E  (D)
            0x01, // 45 U    EV (E)
            0x01, // 46 U    E  (F)
            0x01, // 47 U    E  (G)
            0x01, // 48 U    E  (H)
            0x01, // 49 U    EV (I)
            0x01, // 4a U    E  (J)
            0x01, // 4b U    E  (K)
            0x01, // 4c U    E  (L)
            0x01, // 4d U    E  (M)
            0x01, // 4e U    E  (N)
            0x01, // 4f U    EV (O)
            0x01, // 50 U    E  (P)
            0x01, // 51 U    E  (Q)
            0x01, // 52 U    E  (R)
            0x01, // 53 U    E  (S)
            0x01, // 54 U    E  (T)
            0x01, // 55 U    EV (U)
            0x01, // 56 U    E  (V)
            0x01, // 57 U    E  (W)
            0x01, // 58 U    E  (X)
            0x01, // 59 U    E  (Y)
            0x01, // 5a U    E  (Z)
            0x03, // 5b     P   ([)
            0x03, // 5c     P   (\)
            0x03, // 5d     P   (])
            0x03, // 5e     P   (^)
            0x00, // 5f     P   (_)
        };

        private TextContainer textContainer;
        private TextContainerChangedEventHandler textChangedHandler;
        private ArrayList breakPositions;
        // private ArrayList                            spellPositions;
        private TextChunk textChunk;
        #endregion

        // If LCID indicates Japanese, use TextChunk to do word breaking.
        // 1. Get range from punct/space to punct/space
        // 2. Tell TextChunk to use the determined range

        #region Construction
        public WordBreaker() : this(null, null, DefaultWindowSize, null)
        {
        }

        public WordBreaker(int windowSize) : this(null, null, windowSize, null)
        {
        }

        public WordBreaker(TextChunk chunk) : this(null, chunk, DefaultWindowSize, null)
        {
        }

        public WordBreaker(CultureInfo culture) : this(null, null, DefaultWindowSize, culture)
        {
        }

        public WordBreaker(TextContainer container) : this(container, null, DefaultWindowSize, null)
        {
        }

        public WordBreaker(TextContainer container, int windowSize) : this(container, null, windowSize, null)
        {
        }

        public WordBreaker(TextContainer container, TextChunk chunk) : this(container, chunk, DefaultWindowSize, null)
        {
        }

        public WordBreaker(TextContainer container, CultureInfo culture) : this(container, null, DefaultWindowSize, culture)
        {
        }

        public WordBreaker(TextChunk chunk, int windowSize) : this(null, chunk, windowSize, null)
        {
        }

        public WordBreaker(TextContainer container, TextChunk chunk, int windowSize, CultureInfo culture)
        {
            if (windowSize < 1)
                throw (new ArgumentOutOfRangeException("windowSize"));
            buffer = new char[windowSize];

            textContainer = container;
            if (null != container)
            {
                textChangedHandler = new TextContainerChangedEventHandler(OnTextChanged);
                textContainer.TextChanged += textChangedHandler;
            }

            if (null == chunk)
            {
                Context context = new Context();

                context.IsSpellChecking = true;

                textChunk = new TextChunk(context);

                if (null == culture)
                    textChunk.Culture = CultureInfo.CurrentCulture; // Or Japanese, if we can find it.
                else
                    textChunk.Culture = culture;
            }
            else
            {
                textChunk = chunk;
            }

            Sync();
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the Text Changed Routine Handler
        /// </summary>
        /// <value></value>
        public TextContainerChangedEventHandler TextChangedRoutine
        {
            get
            {
                if (textChangedHandler == null)
                    textChangedHandler = new TextContainerChangedEventHandler(OnTextChanged);

                return textChangedHandler;
            }
        }

        #endregion

        #region Text Helpers

        #region Navigation
        internal static string GenerateText(TextPosition begin, TextPosition end)
        {
            StringBuilder output = new StringBuilder();
            TextNavigator navigator = begin.CreateNavigator();
            TextSymbolType type;
            char[] buffer = new char[1];
            char ch;

            if (begin.TextContainer != end.TextContainer)
                throw new ArgumentException(SR.Get(SRID.BeginEndTextContainerMismatch));

            navigator.MoveToPosition(begin);
            type = navigator.GetSymbolType(LogicalDirection.Forward);
            while (navigator < end)
            {
                switch (type)
                {
                    case TextSymbolType.Character:
                        navigator.GetText(LogicalDirection.Forward, 1, navigator.TextContainer.End, buffer, 0);
                        ch = buffer[0];
                        output.Append(ch);
                        break;

                    case TextSymbolType.EmbeddedObject:
                        ch = '\xF8FF';      // Private use Unicode.
                        output.Append(ch);
                        break;

                    case TextSymbolType.ElementStart:
                    case TextSymbolType.ElementEnd:
                        if (IsBreakingSymbol(navigator, LogicalDirection.Forward))
                        {
                            output.Append(" ");
                        }
                        break;
                }

                navigator.MoveByDistance(1);
                type = navigator.GetSymbolType(LogicalDirection.Forward);
            }

            return output.ToString();
        }

        // Attach non-breaking elements to the text or object on
        // their left.
        internal static bool MoveNaviForward(TextNavigator nav)
        {
            TextSymbolType type;
            bool continueLoop = true;
            TextPosition position = nav.CreatePosition();

            while (continueLoop)
            {
                if (nav.CompareTo(nav.TextContainer.End) == 0)
                    return false;

                nav.MoveByDistance(1);
                type = nav.GetSymbolType(LogicalDirection.Forward);

                switch (type)
                {
                    case TextSymbolType.Character:
                    case TextSymbolType.EmbeddedObject:
                        continueLoop = false;
                        break;

                    case TextSymbolType.ElementStart:
                    case TextSymbolType.ElementEnd:
                        if (IsBreakingSymbol(nav, LogicalDirection.Forward))
                            continueLoop = false;
                        break;
                }
            }
            return (nav.CompareTo(position) > 0);
        }

        // Attach non-breaking elements to the text or object on
        // their left.
        internal static bool MoveNaviBackward(TextNavigator nav)
        {
            TextSymbolType type;
            bool continueLoop = true;
            TextPosition position = nav.CreatePosition();

            type = nav.GetSymbolType(LogicalDirection.Backward);
            while (continueLoop)
            {
                if (nav.CompareTo(nav.TextContainer.Start) == 0)
                    return false;

                switch (type)
                {
                    case TextSymbolType.Character:
                    case TextSymbolType.EmbeddedObject:
                        continueLoop = false;
                        break;

                    case TextSymbolType.ElementStart:
                    case TextSymbolType.ElementEnd:
                        if (IsBreakingSymbol(nav, LogicalDirection.Backward))
                            continueLoop = false;
                        break;
                }
                nav.MoveByDistance(-1);
                type = nav.GetSymbolType(LogicalDirection.Backward);
            }
            return (nav.CompareTo(position) < 0);
        }

        internal static int GetLexClass(char input)
        {
            int cType;

            if (input < 0x0080)
                cType = lexTable[input];
            else
            {
                if (input < 0x2000)
                    cType = 1;          // "Alphanumeric"
                else if ((input >= 0x2000) && (input < 0x3000))
                    cType = 3;          // Misc. Punct
                else
                {
                    cType = 7;          // Assume CJK

                    // Fullwidth etc..  Normalize?
                    if ((input >= 0xFF00) && (input < 0xFF60))
                        cType = lexTable2[(input & 0x00FF)];
                    if (input == 0xF8FF)
                        cType = 8;      // Embedded Object.
                }
            }

            return cType;
        }

        // Roughly Word-like textbreaking state machine.
        //   used in word selection (unidirectional)
        // A: Letters & Digits
        // B: Apostrophe
        // C: Other Punct
        // D: LF
        // E: CR
        // F: Spc, Horiz. Tab
        // G: Non-Western (JA, CH, etc)
        // H: Embedded Object
        //
        // 1: A+(BA*)*F*
        // 2: (B|C)C*F*
        // 3: (ED?)|D
        // 4: F+
        // 5: G+
        // 6: H
        internal static bool ComplexPreproc(string input, int startIndex, out int begin, out int length)
        {
            int state = 0;
            int index = startIndex;
            int end = -2;
            int cType;
            const int height = 8;   // Number of character classes
            int[] machine = new int[] {
                 1, 4, 4, 7, 6, 8, 9,10 ,
                 1, 2,-1,-1,-1, 3,-1,-1 ,
                 2,-1,-1,-1,-1, 3,-1,-1 ,
                -1,-1,-1,-1,-1, 3,-1,-1 ,
                -1,-1, 4,-1,-1, 5,-1,-1 ,
                -1,-1,-1,-1,-1, 5,-1,-1 ,
                -1,-1,-1, 7,-1,-1,-1,-1 ,
                -1,-1,-1,-1,-1,-1,-1,-1 ,
                -1,-1,-1,-1,-1, 8,-1,-1 ,
                -1,-1,-1,-1,-1,-1, 9,-1 ,
                -1,-1,-1,-1,-1,-1,-1,-1 };

            begin = -1;

            while (index < input.Length)
            {
                cType = GetLexClass(input[index]);

                if (cType != 0)
                {
                    --cType;
                    if (-1 == begin)
                        begin = index;
                    state = machine[((state * height) + cType)];
                    if (-1 != state)
                    {
                        end = index;
                    }
                    else
                        break;
                }
                index++;
            }

            length = end - begin + 1;
            return (end > begin);
            // To get the string, use input.Substring(begin, length)
        }

        // Simple WS-oriented textbreaking state machine
        //   used for spelling (bidirectional)
        //
        // 1: (A|B|C|F)+    "Western" text and punct, and non-MWE-breaking space
        // 3: (D|E|G|H)+    Breaking space || breaking symbols
        internal static bool SimplePreproc(string input, int startIndex, out int begin, out int length, out bool isSpellable)
        {
            int state = 0;
            int index = startIndex;
            int end = -2;
            int cType;
            const int height = 8;   // Number of character classes
            int[] machine = new int[] {
                 1, 1, 1, 2, 2, 1, 2, 2 ,
                 1, 1, 1,-1,-1, 1,-1,-1 ,
                -1,-1,-1, 2, 2,-1, 2, 2 };

            begin = -1;
            isSpellable = false;

            while (index < input.Length)
            {
                cType = GetLexClass(input[index]);

                if (cType != 0)
                {
                    --cType;
                    if (-1 == begin)
                        begin = index;
                    state = machine[((state * height) + cType)];

                    if (1 == state)
                        isSpellable = true;

                    if (-1 != state)
                        end = index;
                    else
                        break;
                }
                index++;
            }

            length = end - begin + 1;
            return (end >= begin);
            // To get the string, use input.Substring(begin, length)
        }

        // Spelling:
        //   Copy Navigator passed in (navLeft & navRight)
        //   MoveNavi navLeft left to find WS/Beginning of buffer
        //   MoveNavi navRight right to find WS/EOF
        //   Create string using GenerateText(navLeft, navRight)
        //   Text is adjacent only if separated only by F-type
        //     for purposes of spelling (MWEs in particular)
        //   In "Western" locales, use locale's TextChunk to do
        //     spelling on resultant text run.  In non-"Western"
        //     locales, use English TextChunk to do spelling on
        //     embedded English text.
        //
        // Word Selection:
        //   Copy Navigator passed in (navLeft & navRight)
        //   MoveNavi navLeft left to find WS/Beginning of buffer
        //   MoveNavi navRight right to find WS/EOF
        //   Create string using GenerateText(navLeft, navRight)
        //   Use ComplexPreproc to break generated string
        //   . Generate list of TextPositions which are breaking points?
        //   Find current position inside generated string
        //   In non-"Western" locales, use TextChunk to further break
        //     down current text run.  Generate and return list of
        //     word breaks somehow?  These should be cached, because
        //     using the TextChunk in non-"Western" locales can be
        //     expensive.

        internal static TextPosition GeneratePosition(TextPosition start, int offset)
        {
            TextNavigator navigator;
            int textLength;

            Debug.Assert(offset >= 0);

            navigator = start.CreateNavigator();

            while (offset > 0)
            {
                switch (navigator.GetSymbolType(LogicalDirection.Forward))
                {
                    case TextSymbolType.EmbeddedObject:
                        navigator.Move(LogicalDirection.Forward);
                        offset--;
                        break;

                    case TextSymbolType.Character:
                        textLength = Math.Min(offset, navigator.GetTextLength(LogicalDirection.Forward));
                        navigator.MoveByDistance(textLength);
                        offset -= textLength;
                        break;

                    case TextSymbolType.ElementStart:
                    case TextSymbolType.ElementEnd:
                        if (IsBreakingSymbol(navigator, LogicalDirection.Forward))
                        {
                            offset--;
                        }
                        navigator.Move(LogicalDirection.Forward);
                        break;

                    case TextSymbolType.None:
                        Debug.Assert(false);
                        break;
                }
            }

            return navigator;
        }

        internal bool BreakText(TextPosition position, bool isSpelling, out ArrayList positionList)
        {
            TextNavigator start = position.CreateNavigator();
            TextNavigator end = position.CreateNavigator();
            TextNavigator checkLeft = position.CreateNavigator();
            TextNavigator checkRight = position.CreateNavigator();
            bool leftEdge = false;
            bool rightEdge = false;
            int distance = 32;
            int index;
            bool foundBreaks = false;

            positionList = null;

            while (!(leftEdge && rightEdge))
            {
                if (!leftEdge)
                {
                    if (start.TextContainer.Start.GetDistanceTo(start) > distance)
                    {
                        start.MoveByDistance(-(distance));
                    }
                    else
                    {
                        start.MoveToPosition(start.TextContainer.Start);
                        leftEdge = true;
                    }

                    MoveNaviForward(start);
                    MoveNaviBackward(start);
                }

                if (!rightEdge)
                {
                    if (end.GetDistanceTo(end.TextContainer.End) > distance)
                    {
                        end.MoveByDistance(distance);
                    }
                    else
                    {
                        end.MoveToPosition(end.TextContainer.End);
                        rightEdge = true;
                    }

                    MoveNaviBackward(end);
                    MoveNaviForward(end);
                }

                if (BreakText(start, end, isSpelling, out positionList))
                    foundBreaks = true;
                else
                    continue;

                if (!leftEdge)
                {
                    index = positionList.BinarySearch(checkLeft);
                    if (index < 0)
                        index = (~index);
                    index -= 1;

                    leftEdge = (index >= 0);
                }

                if (!rightEdge)
                {
                    index = positionList.BinarySearch(checkRight);
                    if (index < 0)
                        index = (~index);
                    else
                        index += 1;

                    rightEdge = (index < positionList.Count);
                }
            }
            return foundBreaks;
        }

        internal bool BreakText(TextPosition start, TextPosition end, bool isSpelling, out ArrayList positionList)
        {
            string breakingString;
            ArrayList indexList;
            int currentIndex = 0;
            int lastIndex = 0;
            int i;
            TextNavigator nav = start.CreateNavigator();

            positionList = null;

            // Convert input and execute main work.
            if (nav.CompareTo(nav.TextContainer.Start) != 0)
            {
                MoveNaviBackward(nav);
                MoveNaviForward(nav);
            }

            breakingString = GenerateText(nav, end);
            if (!BreakText(breakingString, isSpelling, out indexList))
                return false;

            // Convert from indices to TextPositions and return.
            positionList = new ArrayList();
            if (0 == start.CompareTo(start.TextContainer.Start))
                positionList.Add(start);

            foreach (Object item in indexList)
            {
                currentIndex = (int)item;
                for (i = 0; i < (currentIndex - lastIndex); ++i)
                    MoveNaviForward(nav);
                positionList.Add(nav.CreatePosition());
                lastIndex = currentIndex;
            }

            if (0 == end.CompareTo(end.TextContainer.End))
            {
                // Add the end of the container as the last break.
                positionList.Add(end);
            }
            return true;
        }

        internal bool BreakText(string input, bool isSpelling, out ArrayList indexList)
        {
            string subString;
            int offset = 0;
            int begin = 0;
            int length = 0;
            int inputLanguage = textChunk.Culture.LCID;
            bool isSpacePunctLang;
            TextChunk chunk;
            bool isSpellable;
            bool foundBreaks = false;

            isSpacePunctLang = !((inputLanguage == 1041)             // Ja
                || (inputLanguage == 0x0804));      // CHS
            indexList = null;

            // Don't assume that we can safely call the first character a
            // "break"..  Jump to the next break and use that as the first
            // because we may be starting in the middle of a token depending
            // on the caller.
            if (isSpelling)
                SimplePreproc(input, begin, out begin, out length, out isSpellable);
            else
                ComplexPreproc(input, begin, out begin, out length);
            begin += length;
            length = 0;

            if (begin < 0)
                return foundBreaks;

            if ((begin + length) < input.Length)
            {
                foundBreaks = true;
                indexList = new ArrayList();
            }

            while ((begin + length) < input.Length)
            {
                if (isSpelling)
                {
                    SimplePreproc(input, begin, out begin, out length, out isSpellable);
                    indexList.Add(begin);
                    begin += length;
                    length = 0;
                }
                else
                {
                    ComplexPreproc(input, begin, out begin, out length);
                    if (!isSpacePunctLang)
                    {
                        // If non-"Western", use TextChunk to break Kanji.
                        subString = input.Substring(begin, length);
                        offset = begin;
                        chunk = textChunk;
                        chunk.InputText = subString;
                        if (chunk.Sentences.Count > 0)
                        {
                            foreach (Sentence sentence in chunk)
                            {
                                foreach (Segment segment in sentence)
                                {
                                    indexList.Add(offset + segment.Range.Start);
                                    begin = offset + segment.Range.Start + segment.Range.Length;
                                }
                            }
                        }
                        else
                        {
                            indexList.Add(begin);
                            begin += length;
                        }
                        length = 0;
                    }
                    else
                    {
                        indexList.Add(begin);
                        begin += length;
                        length = 0;
                    }
                }
            }
            return foundBreaks;
        }

        #endregion

        #endregion

        #region Words Movement
        private static bool IsBreakingSymbol(TextNavigator navigator, LogicalDirection direction)
        {
            TextSymbolType type = navigator.GetSymbolType(direction);

            // (JCS) - This will need to be reworked after the Avalon team has
            // an API that can be called to determine if an Element should be
            // "breaking" or not.
            return ((type == TextSymbolType.None)
                    || (type == TextSymbolType.EmbeddedObject)
                    || (((type == TextSymbolType.ElementStart)
                            || (type == TextSymbolType.ElementEnd)
                            )
                        && (navigator.GetElementType(direction).IsAssignableFrom(typeof(InlineElement)))
                        )
                    );
        }

        /// <summary>
        /// Returns if the current TextPosition is at a selection boundary
        /// </summary>
        public bool IsAtWordBreak(TextPosition position)
        {
            int index;

            // If the array of break positions hasn't been created and
            // populated yet, do that work now.
            if (null == breakPositions)
            {
                if (!BreakText(position, false, out breakPositions))
                    return false;
            }

            index = breakPositions.BinarySearch(position);
            if (index < 0)
            {
                if (((~index) == 0)                         // Off the front
                    || ((~index) == breakPositions.Count))   // Off the end
                {
                    breakPositions = null;
                    if (!BreakText(position, false, out breakPositions))
                        return false;
                    return !(breakPositions.BinarySearch(position) < 0);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to find a selection boundary LogicalDirection.Forward of
        /// the current position.
        /// </summary>
        public bool MoveToNextWordBreak(TextNavigator navigator)
        {
            int index;

            // If the array of break positions hasn't been created and
            // populated yet, do that work now.
            if (null == breakPositions)
            {
                if (!BreakText(navigator, false, out breakPositions))
                    return false;
            }

            index = breakPositions.BinarySearch(navigator);
            if (index < 0)
                index = (~index);
            else
                index += 1;

            if ((index > breakPositions.Count - 1)
                && (0 != navigator.TextContainer.End.CompareTo((TextPosition)breakPositions[breakPositions.Count - 1])))
            {
                breakPositions = null;
                if (!BreakText(navigator, false, out breakPositions))
                    return false;
                index = breakPositions.BinarySearch(navigator);
                if (index < 0)
                    index = (~index);
                else
                    index += 1;
            }

            if ((index < 0) || (index > breakPositions.Count - 1))
                return false;

            navigator.MoveToPosition((TextPosition)breakPositions[index]);
            return true;
        }

        /// <summary>
        /// Tries to find a selection boundary LogicalDirection.Backward of
        /// the current position.
        /// </summary>
        public bool MoveToPreviousWordBreak(TextNavigator navigator)
        {
            int index;

            // If the array of break positions hasn't been created and
            // populated yet, do that work now.
            if (null == breakPositions)
            {
                if (!BreakText(navigator, false, out breakPositions))
                    return false;
            }

            index = breakPositions.BinarySearch(navigator);
            if (index < 0)
                index = (~index);
            index -= 1;

            if ((index < 0)
                && (0 != navigator.TextContainer.Start.CompareTo((TextPosition)breakPositions[0])))
            {
                breakPositions = null;
                if (!BreakText(navigator, false, out breakPositions))
                    return false;
                index = breakPositions.BinarySearch(navigator);
                if (index < 0)
                    index = (~index);
                index -= 1;
            }

            if ((index < 0) || (index > breakPositions.Count - 1))
                return false;

            navigator.MoveToPosition((TextPosition)breakPositions[index]);
            return true;
        }

        /// <summary>
        /// Text Changed Handler
        /// </summary>
        /// <param name="target"></param>
        /// <param name="args"></param>
        private void OnTextChanged(object target, TextContainerChangedEventArgs args)
        {
            // Invalidate cache
            breakPositions = null;
            // spellPositions = null;

            // Actually do the refresh the next time they call Break, rather
            // than us doing it now.
            // BreakText(navigator.TextContainer.Start, navigator.TextContainer.End, false, out breakPositions);

            // At some point, it might be good to do something with
            // args.Start, args.SymbolsAdded, and args.SymbolsRemoved to only
            // update the cache as needed rather than recomputing all
            // breaks.
        }

        public bool IncludeWhiteSpaceBefore
        {
            get { return includeWhiteSpaceBefore; }
            set
            {
                includeWhiteSpaceBefore = value;
                Sync();
            }
        }

        public bool IncludeWhiteSpaceAfter
        {
            get { return includeWhiteSpaceAfter; }
            set
            {
                includeWhiteSpaceAfter = value;
                Sync();
            }
        }

        #endregion

        #region Enums

        private void Sync()
        {
        }

        #endregion

    }
}
