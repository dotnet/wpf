// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System.Windows;
using System;
using MS.Internal.TextFormatting;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Windows.Media.TextFormatting;
using MS.Internal;

namespace MS.Internal.TextFormatting
{
    /// <Remark>
    /// Bidi resolution services
    /// </Remark>
    internal static class Bidi
    {
        static private readonly StateMachineAction [,]  Action;
        static private readonly StateMachineState  [,]  NextState;
        static private readonly byte               [,]  ImplictPush;
        static private readonly byte               [,]  CharProperty;
        static private readonly StateMachineState  []   ClassToState;
        static private readonly byte               []   FastPathClass;

        // Hidden char doesn't affect the relative ordering of surroudning characters. 
        // They are internally assigned to the class types of either the previous or following non-hidden text
        static private char CharHidden = '\xFFFF';

        static Bidi()
        {
            Action = new StateMachineAction[9, 11]
            {
                //          L                             R                             AN                             EN                             AL                            ES                             CS                             ET                            NSM                            BN                           N
                /*S_L*/    {StateMachineAction.ST_ST,     StateMachineAction.ST_ST,     StateMachineAction.ST_ST,      StateMachineAction.EN_L,       StateMachineAction.ST_ST,     StateMachineAction.SEP_ST,     StateMachineAction.SEP_ST,     StateMachineAction.CS_NUM,    StateMachineAction.NSM_ST,     StateMachineAction.BN_ST,    StateMachineAction.N_ST},
                /*S_AL*/   {StateMachineAction.ST_ST,     StateMachineAction.ST_ST,     StateMachineAction.ST_ST,      StateMachineAction.EN_AL,      StateMachineAction.ST_ST,     StateMachineAction.SEP_ST,     StateMachineAction.SEP_ST,     StateMachineAction.CS_NUM,    StateMachineAction.NSM_ST,     StateMachineAction.BN_ST,    StateMachineAction.N_ST},
                /*S_R*/    {StateMachineAction.ST_ST,     StateMachineAction.ST_ST,     StateMachineAction.ST_ST,      StateMachineAction.ST_ST,      StateMachineAction.ST_ST,     StateMachineAction.SEP_ST,     StateMachineAction.SEP_ST,     StateMachineAction.CS_NUM,    StateMachineAction.NSM_ST,     StateMachineAction.BN_ST,    StateMachineAction.N_ST},
                /*S_AN*/   {StateMachineAction.ST_ST,     StateMachineAction.ST_ST,     StateMachineAction.ST_ST,      StateMachineAction.NUM_NUM,    StateMachineAction.ST_ST,     StateMachineAction.ES_AN,      StateMachineAction.CS_NUM,     StateMachineAction.CS_NUM,    StateMachineAction.NSM_ST,     StateMachineAction.BN_ST,    StateMachineAction.N_ST},
                /*S_EN*/   {StateMachineAction.ST_ST,     StateMachineAction.ST_ST,     StateMachineAction.ST_ST,      StateMachineAction.NUM_NUM,    StateMachineAction.ST_ST,     StateMachineAction.CS_NUM,     StateMachineAction.CS_NUM,     StateMachineAction.ET_EN,     StateMachineAction.NSM_ST,     StateMachineAction.BN_ST,    StateMachineAction.N_ST},
                /*S_ET*/   {StateMachineAction.ST_ET,     StateMachineAction.ST_ET,     StateMachineAction.ST_ET,      StateMachineAction.EN_ET,      StateMachineAction.ST_ET,     StateMachineAction.SEP_ET,     StateMachineAction.SEP_ET,     StateMachineAction.ET_ET,     StateMachineAction.NSM_ET,     StateMachineAction.BN_ST,    StateMachineAction.N_ET},
                /*S_ANfCS*/{StateMachineAction.ST_NUMSEP, StateMachineAction.ST_NUMSEP, StateMachineAction.NUM_NUMSEP, StateMachineAction.ST_NUMSEP,  StateMachineAction.ST_NUMSEP, StateMachineAction.SEP_NUMSEP, StateMachineAction.SEP_NUMSEP, StateMachineAction.ET_NUMSEP, StateMachineAction.SEP_NUMSEP, StateMachineAction.BN_ST,    StateMachineAction.N_ST},
                /*S_ENfCS*/{StateMachineAction.ST_NUMSEP, StateMachineAction.ST_NUMSEP, StateMachineAction.ST_NUMSEP,  StateMachineAction.NUM_NUMSEP, StateMachineAction.ST_NUMSEP, StateMachineAction.SEP_NUMSEP, StateMachineAction.SEP_NUMSEP, StateMachineAction.ET_NUMSEP, StateMachineAction.SEP_NUMSEP, StateMachineAction.BN_ST,    StateMachineAction.N_ST},
                /*S_N*/    {StateMachineAction.ST_N,      StateMachineAction.ST_N,      StateMachineAction.ST_N,       StateMachineAction.EN_N,       StateMachineAction.ST_N,      StateMachineAction.SEP_N,      StateMachineAction.SEP_N,      StateMachineAction.ET_N,      StateMachineAction.NSM_ET,     StateMachineAction.BN_ST,    StateMachineAction.N_ET}
            };

            NextState   = new StateMachineState[9, 11]
            {
                //          L                             R                            AN                            EN                            AL                           ES                            CS                            ET                           NSM                           BN                         N
                /*S_L*/     {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_N,        StateMachineState.S_ET,      StateMachineState.S_L,        StateMachineState.S_L,     StateMachineState.S_N},
                /*S_AL*/    {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_AN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_N,        StateMachineState.S_ET,      StateMachineState.S_AL,       StateMachineState.S_AL,    StateMachineState.S_N},
                /*S_R*/     {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_N,        StateMachineState.S_ET,      StateMachineState.S_R,        StateMachineState.S_R,     StateMachineState.S_N},
                /*S_AN*/    {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_ANfCS,    StateMachineState.S_ET,      StateMachineState.S_AN,       StateMachineState.S_AN,    StateMachineState.S_N},
                /*S_EN*/    {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_ENfCS,    StateMachineState.S_ENfCS,    StateMachineState.S_EN,      StateMachineState.S_EN,       StateMachineState.S_EN,    StateMachineState.S_N},
                /*S_ET*/    {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_N,        StateMachineState.S_ET,      StateMachineState.S_ET,       StateMachineState.S_ET,    StateMachineState.S_N},
                /*S_ANfCS*/ {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_N,        StateMachineState.S_ET,      StateMachineState.S_N,        StateMachineState.S_ANfCS, StateMachineState.S_N},
                /*S_ENfCS*/ {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_N,        StateMachineState.S_ET,      StateMachineState.S_N,        StateMachineState.S_ENfCS, StateMachineState.S_N},
                /*S_N*/     {StateMachineState.S_L,       StateMachineState.S_R,       StateMachineState.S_AN,       StateMachineState.S_EN,       StateMachineState.S_AL,      StateMachineState.S_N,        StateMachineState.S_N,        StateMachineState.S_ET,      StateMachineState.S_N,        StateMachineState.S_N,     StateMachineState.S_N}
            };

            ImplictPush = new byte[2,4]
            {
                //        L,  R,  AN, EN
                /*even*/  {0,  1,  2,  2},
                /*odd*/   {1,  0,  1,  1}
};


            CharProperty = new byte[6, (int) DirectionClass.ClassMax - 1]
            {
                                    //L    R    AN   EN   AL   ES   CS   ET   NSM  BN   N    B    LRE  LRO  RLE  RLO  PDF  S    WS   ON
               /*STRONG*/           { 1,   1,   0,   0,   1,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0} ,
               /*STRONG/NUMBER*/    { 1,   1,   1,   1,   1,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0} ,
               /*FIXED*/            { 1,   1,   1,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0} ,
               /*FINAL*/            { 1,   1,   1,   1,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0} ,
               /*NUMBER*/           { 0,   0,   1,   1,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0} ,
               /*VALID INDEX*/      { 1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   0,   0,   0,   0,   0,   0,   0,   0,   0}
            };

            ClassToState = new StateMachineState[(int) DirectionClass.ClassMax]
            {
                /* Left                 */ StateMachineState.S_L,
                /* Right                */ StateMachineState.S_R,
                /* ArabicNumber         */ StateMachineState.S_AN,
                /* EuropeanNumber       */ StateMachineState.S_EN,
                /* ArabicLetter         */ StateMachineState.S_AL,
                /* EuropeanSeparator    */ StateMachineState.S_L,
                /* CommonSeparator      */ StateMachineState.S_L,
                /* EuropeanTerminator   */ StateMachineState.S_L,
                /* NonSpacingMark       */ StateMachineState.S_L,
                /* BoundaryNeutral      */ StateMachineState.S_L,
                /* GenericNeutral       */ StateMachineState.S_L,
                /* ParagraphSeparator   */ StateMachineState.S_L,
                /* LeftToRightEmbedding */ StateMachineState.S_L,
                /* LeftToRightOverride  */ StateMachineState.S_L,
                /* RightToLeftEmbedding */ StateMachineState.S_L,
                /* RightToLeftOverride  */ StateMachineState.S_L,
                /* PopDirectionalFormat */ StateMachineState.S_L,
                /* SegmentSeparator     */ StateMachineState.S_L,
                /* WhiteSpace           */ StateMachineState.S_L,
                /* OtherNeutral         */ StateMachineState.S_L,
                /* ClassInvalid         */ StateMachineState.S_L
            };

            //  FastPathClass
            //  0 means couldn't handle through the fast loop.
            //  1 means treat it as nuetral character.
            //  2 means Left strong character.
            //  3 Right strong character.

            FastPathClass = new byte[(int) DirectionClass.ClassMax]
            {
                /* Left                 */ 2,
                /* Right                */ 3,
                /* ArabicNumber         */ 0,
                /* EuropeanNumber       */ 0,
                /* ArabicLetter         */ 3,
                /* EuropeanSeparator    */ 1,
                /* CommonSeparator      */ 1,
                /* EuropeanTerminator   */ 0,
                /* NonSpacingMark       */ 0,
                /* BoundaryNeutral      */ 0,
                /* GenericNeutral       */ 1,
                /* ParagraphSeparator   */ 0,
                /* LeftToRightEmbedding */ 0,
                /* LeftToRightOverride  */ 0,
                /* RightToLeftEmbedding */ 0,
                /* RightToLeftOverride  */ 0,
                /* PopDirectionalFormat */ 0,
                /* SegmentSeparator     */ 0,
                /* WhiteSpace           */ 1,
                /* OtherNeutral         */ 1,
                /* ClassInvalid         */ 1
            };
}


        //
        //  Helper Class
        //

        private static class Helper
        {
            public static ulong LeftShift(ulong x, byte y)
            {
                return x << y;
            }

            public static ulong LeftShift(ulong x, int y)
            {
                return x << y;
            }

            public static void SetBit(ref ulong x, byte y)
            {
                x |= LeftShift(1,y);
            }

            public static void ResetBit(ref ulong x, int y)
            {
                x &= ~LeftShift(1,y);
            }

            public static bool IsBitSet(ulong x, byte y)
            {
                return ((x & LeftShift(1,y)) != 0);
            }

            public static bool IsBitSet(ulong x, int y)
            {
                return ((x & LeftShift(1,y)) != 0);
            }

            public static bool IsOdd(byte x)
            {
                return ((x & 1) != 0);
            }

            public static bool IsOdd(int x)
            {
                return ((x & 1) != 0);
            }
        }

        //
        // BidiStack class. It represents the level stack in bidi analysis
        // The level stack is internally stored as a ulong. The Nth bit's value 
        // represents whether level N is on stack.
        //
        internal class BidiStack
        {
            private const byte EmbeddingLevelInvalid    = 62;

            public BidiStack()
            {
                currentStackLevel = 0;
            }

            public bool Init (ulong initialStack)
            {
                byte    currentLevel   = GetMaximumLevel(initialStack);
                byte    minimumLevel   = GetMinimumLevel(initialStack);

                if ((currentLevel >= EmbeddingLevelInvalid) || (minimumLevel < 0))
                {
                    return false;
                }
                stack = initialStack;
                currentStackLevel = currentLevel;
                return true;
            }

            public bool Push(bool pushToGreaterEven)
            {
                byte newMaximumLevel; 
                if (!PushCore(ref stack, pushToGreaterEven, currentStackLevel, out newMaximumLevel))
                    return false;
                    
                currentStackLevel = newMaximumLevel;
                return true;
            }

            public bool Pop()
            {
                byte newMaximumLevel;

                if (!PopCore(ref stack, currentStackLevel, out newMaximumLevel))
                    return false;
                
                currentStackLevel = newMaximumLevel;
                return true;
            }

            public byte   GetStackBottom()
            {
                return GetMinimumLevel(stack);
            }

            public byte   GetCurrentLevel()
            {
                return currentStackLevel;
            }

            public ulong GetData()
            {
                return stack;
            }

            /// <summary>
            /// Helper method to push to bidi stack. Bidi stack is a ulong, the value of the Nth bit inidcates whether
            /// level N is on stack. 
            /// </summary>
            internal static bool Push(ref ulong stack, bool pushToGreaterEven, out byte topLevel)
            {
                byte currentLevel = GetMaximumLevel(stack);
                return PushCore(ref stack, pushToGreaterEven, currentLevel, out topLevel);                
            }
            
            /// <summary>
            /// Helper method to pop bidi stack. Bidi stack is a ulong, the value of the Nth bit inidcates whether
            /// level N is on stack. 
            /// </summary>
            internal static bool Pop(ref ulong stack, out byte topLevel)
            {
                byte currentLevel = GetMaximumLevel(stack);
                return PopCore(ref stack, currentLevel, out topLevel);
            }

            /// <summary>
            /// Helper method to get the top-most level of the bidi stack.
            /// </summary>
            internal static byte GetMaximumLevel(ulong inputStack)
            {
                byte maximumLevel = 0;
                for (int counter=MaxLevel; counter>=0; counter--)
                {
                    if (Helper.IsBitSet(inputStack, counter))
                    {
                        maximumLevel = (byte) counter;
                        break;
                    }
                }
                return maximumLevel;
            }            

            private static bool PushCore(
                ref ulong  stack, 
                bool       pushToGreaterEven, 
                byte       currentStackLevel, 
                out byte   newMaximumLevel
                )
            {
                newMaximumLevel =
                     pushToGreaterEven ? GreaterEven(currentStackLevel) : GreaterOdd(currentStackLevel);

                if (newMaximumLevel >= EmbeddingLevelInvalid)
                {
                    newMaximumLevel = currentStackLevel;
                    return false;
                }
                
                Helper.SetBit(ref stack, newMaximumLevel);                                       
                return true;
            }   

            private static bool PopCore(
                ref ulong  stack, 
                byte       currentStackLevel, 
                out byte   newMaximumLevel                
                )
            {
                newMaximumLevel = currentStackLevel;
                if (currentStackLevel == 0 || ((currentStackLevel == 1) && ((stack & 1)==0)))
                {
                    return false;
                }
                newMaximumLevel = Helper.IsBitSet(stack, currentStackLevel - 1) ?
                                  (byte)(currentStackLevel - 1) : (byte)(currentStackLevel - 2);

                Helper.ResetBit(ref stack, currentStackLevel);            
                return true;
            }

            private static byte GetMinimumLevel(ulong inputStack)
            {
                byte minimumLevel = 0xFF;
                for (byte counter =0; counter<=MaxLevel; counter++)
                {
                    if (Helper.IsBitSet(inputStack, counter))
                    {
                        minimumLevel = counter;
                        break;
                    }
                }
                return minimumLevel;
            }

            private static byte GreaterEven(byte level)
            {
                return Helper.IsOdd(level) ? (byte) (level + 1) : (byte) (level + 2);
            }

            private static byte GreaterOdd(byte level)
            {
                return Helper.IsOdd(level) ? (byte) (level + 2) : (byte) (level + 1);
            }

            private ulong stack;
            private byte  currentStackLevel;
        };

        #region Enumerations & Const
        /// <Remark>
        /// Bidi control flags
        /// </Remark>
        public enum Flags : uint
        {
            /// <Remark>
            /// Paragraph direction defaults to left to right.
            /// Ignored if ContinueAnalysis flag is set.
            /// </Remark>
            DirectionLeftToRight              = 0x00000000,

            /// <Remark>
            /// Paragraph direction defaults to right to left.
            /// Ignored if ContinueAnalysis flag is set.
            /// </Remark>
            DirectionRightToLeft              = 0x00000001,

            /// <Remark>
            /// Paragragraph direction determined by scanning for the first strongly
            /// directed character. If no strong character is found, defaults to
            /// setting of DirectionRtl flag.
            /// Ignored if ContinueAnalysis flag is set.
            /// </Remark>
            FirstStrongAsBaseDirection        = 0x00000002,

            /// <Remark>
            /// Parse numbers as if the paragraph were preceeded by an Arabic letter.
            /// Ignored if ContinueAnalysis flag is set.
            /// </Remark>
            PreviousStrongIsArabic            = 0x00000004,

            /// <Remark>
            /// This analysis is a continuation. The 'state' parameter provides the
            /// last state of the previously analyzed block. This flag causes
            /// DirectionRtl, FirstStrongAsBaseDirection, and PreviousStrongIsArabic
            /// flags to be ignored.
            /// </Remark>
            ContinueAnalysis                  = 0x00000008,   // require state-in

            /// <Remark>
            /// Indicates that the input text may not end at a paragraph boundary,
            /// and that futher calls to the API may be made for subsequent text to
            /// come and thereby resolve trailing neutral characters. If this flag
            /// is set, the 'state' and 'cchResolved' parameters are required.
            /// </Remark>
            IncompleteText                    = 0x00000010,   // require state-out

            /// <Remark>
            /// The hint is given for the maximum number of character to be analyzed.
            /// This is purely for performance optimization. If this flag is set,
            /// the API will start attempting to stop the process at a proper character
            /// position beginning at the position given by the 'cchTextMaxHint'
            /// parameter.
            /// </Remark>
            MaximumHint                       = 0x00000020,   // hint on upper bound limit

            /// <Remark>
            /// Indicate that direction controls (i.e. LRE, RLE, PDF, LRO and RLO) are not to 
            /// be processed in the analysis. These characters will be treated as neutrals and 
            /// and will not affect the bidi state. 
            /// </Remark>        
            IgnoreDirectionalControls         = 0x00000040,   // ignore all directional controls characters in input

            /// <Remark>
            /// By default Unicode Bidi Algorithm resolves European number based on preceding 
            /// character (Unciode Bidi Rule w2). When this flag is set, the API will ignore this rule and 
            /// use the input DirectionClass explicitly for European numbers (Unicode Bidi Rule HL2). The 
            /// valid DirectionClass for European number is either EuropeanNumber or ArabicNumber. This flag 
            /// doesn't affect non European number. 
            /// </Remark>
            OverrideEuropeanNumberResolution  = 0x00000080,
        }


        private enum OverrideClass
        {
            OverrideClassNeutral,
            OverrideClassLeft,
            OverrideClassRight
        };

        private enum StateMachineState
        {
            S_L,        // Left character
            S_AL,       // Arabic letter
            S_R,        // Right character
            S_AN,       // Arabic number
            S_EN,       // European number
            S_ET,       // Europen terminator
            S_ANfCS,    // Arabic number followed by common sperator
            S_ENfCS,    // European number followed by common sperator
            S_N         // Neutral character
        };


        private enum StateMachineAction
        {
            ST_ST,      // Strong followed by strong
            ST_ET,      // ET followed by Strong
            ST_NUMSEP,  // Number followed by sperator follwed by strong
            ST_N,       // Neutral followed by strong
            SEP_ST,     // Strong followed by sperator
            CS_NUM,     // Number followed by CS
            SEP_ET,     // ET followed by sperator
            SEP_NUMSEP, // Number follwed by sperator follwed by number
            SEP_N,      // Neutral followed by sperator
            ES_AN,      // Arabic Number followed by European sperator
            ET_ET,      // European terminator follwed by a sperator
            ET_NUMSEP,  // Number followed by sperator followed by ET
            ET_EN,      // European number follwed by European terminator
            ET_N,       // Neutral followed by European Terminator
            NUM_NUMSEP, // Number followed by sperator followed by number
            NUM_NUM,    // Number followed by number
            EN_L,       // Left followed by EN
            EN_AL,      // AL followed by EN
            EN_ET,      // ET followed by EN
            EN_N,       // Neutral followed by EN
            BN_ST,      // ST followed by BN
            NSM_ST,     // ST followed by NSM
            NSM_ET,     // ET followed by NSM
            N_ST,       // ST followed by neutral
            N_ET        // ET followed by neutral
        };
        #endregion

        #region Public API
        /// <summary>
        /// GetLastStongAndNumberClass is used to get the last strong character class and last number
        /// class. if numberClass is not equal DirectionClass.ClassInvalid then we are interested in
        /// last strong class only otherwise we are interested in both last strong and number class.
        /// this method is useful while implementing the derived Bidi.State properties Bidi.LastNumberClass
        /// and Bidi.LastStrongClass.
        /// </summary>
        static internal bool GetLastStongAndNumberClass(
            CharacterBufferRange charString,
            ref DirectionClass   strongClass,
            ref DirectionClass   numberClass)
        {
            int             wordCount;
            DirectionClass  currentClass;
            int             i = charString.Length - 1;

            while (i >= 0)
            {
                int intChar = charString[i];
                wordCount = 1;

                if (((charString[i] & 0xFC00) == 0xDC00) && (i > 0) &&  ((charString[i-1] & 0xFC00) == 0xD800))
                {
                    intChar = (((charString[i-1] & 0x03ff) << 10) | (charString[i] & 0x3ff)) + 0x10000;
                    wordCount = 2;
                }

                currentClass = Classification.CharAttributeOf((int) Classification.GetUnicodeClass(intChar)).BiDi;

                // Stop scaning backwards in this character buffer once and ParagraphSeperator is encountered. 
                // Bidi algorithm works strictly within a paragraph. 
                if (currentClass == DirectionClass.ParagraphSeparator)
                {                    
                    return false; // stop scaning as paragraph separator is encountered before seeing last strong/number.
                }                
            
                if (CharProperty[1, (int) currentClass] == 1)
                {
                    if (numberClass == DirectionClass.ClassInvalid )
                    {   
                        numberClass = currentClass;                       
                    }
                    
                    if (currentClass != DirectionClass.EuropeanNumber)
                    {
                        strongClass = currentClass;
                        break;
                    }
                }
                i -= wordCount;
            }

            return true; // Finish scanning all the input characters 
        }

        /// <Remark>
        /// Bidi state
        /// </Remark>
        internal class State
        {
            /// <Remark>
            /// Constructor
            /// </Remark>
            public State(bool isRightToLeft)
            {
                OverrideLevels          = 0;
                Overflow                = 0;
                NumberClass             = DirectionClass.Left;
                StrongCharClass         = DirectionClass.Left;
                LevelStack = isRightToLeft ? Bidi.StackRtl : Bidi.StackLtr;
            }

            #region Overridable methods
            /// <Remark>
            /// This method should return one of the following values:
            /// Left, Right, or ArabicLetter.
            /// </Remark>
            public virtual DirectionClass LastStrongClass
            {
                get { return StrongCharClass; }
                set { StrongCharClass = value;}
            }

            /// <Remark>
            /// Last number character
            /// </Remark>
            public virtual DirectionClass LastNumberClass
            {
                get { return NumberClass; }
                set { NumberClass = value; }
            }
            #endregion

            /// <Remark>
            /// Bidi level stack
            /// </Remark>
            public ulong LevelStack
            {
                get { return m_levelStack; }
                set { m_levelStack = value; }
            }

            /// <Remark>
            /// Bidi override status
            /// </Remark>
            public ulong OverrideLevels
            {
                get { return m_overrideLevels; }
                set { m_overrideLevels = value; }
            }

            /// <Remark>
            /// Overflow counter
            /// </Remark>
            public ushort Overflow
            {
                get { return m_overflow; }
                set { m_overflow = value; }
            }

            ulong                       m_levelStack;
            ulong                       m_overrideLevels;

            /// <Remark>
            /// holding the last number class from the analysis
            /// </Remark>
            protected DirectionClass    NumberClass;
            /// <Remark>
            /// holding the last strong class from the analysis
            /// </Remark>
            protected DirectionClass    StrongCharClass;

            ushort                      m_overflow;
        }
        #endregion

        //
        //  Bidi class constants
        //

        private  const byte ParagraphTerminatorLevel = 0xFF;
        private  const int  PositionInvalid          = -1;
        private  const byte BaseLevelLeft            = 0;
        private  const byte BaseLevelRight           = 1;
        private  const uint EmptyStack               = 0;          // no stack
        private  const uint StackLtr                 = 1;          // left to right
        private  const uint StackRtl                 = 2;          // right to left
        private  const int  MaxLevel                 = 63;         // right to left

        //
        //  Start BiDi class implementation
        //
        static private bool GetFirstStrongCharacter(
            CharacterBuffer     charBuffer,
            int                 ichText,
            int                 cchText,
            ref DirectionClass  strongClass)
        {
            DirectionClass currentClass = DirectionClass.ClassInvalid;

            int  counter = 0;
            int wordCount;

            while (counter<cchText)
            {
                int intChar = charBuffer[ichText + counter];
                wordCount = 1;
                if ((intChar & 0xFC00) == 0xD800)
                {
                    intChar = DoubleWideChar.GetChar(charBuffer, ichText, cchText, counter, out wordCount);
                }

                currentClass = Classification.CharAttributeOf((int) Classification.GetUnicodeClass(intChar)).BiDi;

                if (CharProperty[0, (int) currentClass]==1 || currentClass == DirectionClass.ParagraphSeparator)
                {
                    break;
                }
                counter +=  wordCount;
            }

            if (CharProperty[0, (int) currentClass]==1)
            {
                strongClass = currentClass;
                return true;
            }
            return false;
        }

        static private void ResolveNeutrals(
            IList<DirectionClass>   characterClass,   // [IN / OUT]
            int                     classIndex,       // [IN]
            int                     count,            // [IN]
            DirectionClass          startClass,       // [IN]
            DirectionClass          endClass,         // [IN]
            byte                    runLevel          // [IN]
        )
        {
            DirectionClass        startType;
            DirectionClass        endType;
            DirectionClass        resolutionType;

            if ((characterClass == null) || (count == 0))
            {
                return;
            }



            Debug.Assert(CharProperty[1, (int) startClass]==1 || (startClass == DirectionClass.ArabicLetter),
                     ("Cannot use non strong type to resolve neutrals"));

            Debug.Assert(CharProperty[1, (int) endClass]==1, ("Cannot use non strong type to resolve neutrals"));

            startType =  ((startClass == DirectionClass.EuropeanNumber) ||
                          (startClass == DirectionClass.ArabicNumber)   ||
                          (startClass == DirectionClass.ArabicLetter)) ? DirectionClass.Right : startClass;

            endType =  ((endClass == DirectionClass.EuropeanNumber) ||
                        (endClass == DirectionClass.ArabicNumber)   ||
                        (endClass == DirectionClass.ArabicLetter)) ? DirectionClass.Right : endClass;

            if (startType == endType)
            {
                resolutionType = startType;
            }
            else
            {
                resolutionType = Helper.IsOdd(runLevel) ? DirectionClass.Right : DirectionClass.Left;
            }

            for (int counter = 0; counter < count; counter++)
            {
                // We should never be changing a fixed type here

                Debug.Assert(CharProperty[2, (int) characterClass[counter + classIndex]]==0,
                         "Resolving fixed class as being neutral: " +
                         characterClass[counter + classIndex].ToString());

                characterClass[counter + classIndex] = resolutionType;
            }
        }


        static private void ChangeType(
            IList<DirectionClass> characterClass,   // [IN / OUT]
            int                   classIndex,
            int                   count,            // [IN]
            DirectionClass        newClass          // [IN]
        )
        {
            if ((characterClass == null) || (count == 0))
            {
                return;
            }

            for (int counter = 0; counter < count; counter++)
            {
                // We should never be changing a fixed type here

                Debug.Assert(CharProperty[2, (int) characterClass[counter + classIndex]]==0, "Changing class of a fixed class");
                characterClass[counter + classIndex] = newClass;
            }
        }


        static private int ResolveNeutralAndWeak(
            IList<DirectionClass>   characterClass,        // [IN / OUT]
            int                     classIndex,            // [IN]
            int                     runLength,             // [IN]
            DirectionClass          sor,                   // [IN]
            DirectionClass          eor,                   // [IN]
            byte                    runLevel,              // [IN]
            State                   stateIn,               // [IN], [OPTIONAL]
            State                   stateOut,              // [OUT],[OPTIONAL]
            bool                    previousStrongIsArabic,// [IN], OPTIONAL
            Flags                   flags                  // [IN]
        )
        {
            int                      startOfNeutrals    = PositionInvalid;
            int                      startOfDelayed     = PositionInvalid;
            DirectionClass           lastClass          = DirectionClass.ClassInvalid;
            DirectionClass           lastStrongClass    = DirectionClass.ClassInvalid;
            DirectionClass           lastNumericClass   = DirectionClass.ClassInvalid;
            DirectionClass           startingClass      = DirectionClass.ClassInvalid;
            DirectionClass           currentClass       = DirectionClass.ClassInvalid;
            StateMachineState        state;
            bool                     previousClassIsArabic = false;
            bool                     ArabicNumberAfterLeft = false;
            int                      lengthResolved = 0;

            if (runLength == 0)
            {
                return 0;
            }

            if (stateIn != null)
            {
                lastStrongClass = stateIn.LastStrongClass;

                if (stateIn.LastNumberClass != DirectionClass.ClassInvalid)
                {
                    lastNumericClass = startingClass =
                                       lastClass =
                                       stateIn.LastNumberClass;
                }
                else
                {
                    startingClass = lastClass = lastStrongClass;
                }
}
            else if (previousStrongIsArabic)
            {
                startingClass = DirectionClass.ArabicLetter;
                lastClass = lastStrongClass = sor;
                previousClassIsArabic = true;
            }
            else
            {
                startingClass = lastClass = lastStrongClass = sor;
            }

            state = ClassToState[(int) startingClass];

            // We have two types of classes that needs delayed resolution:
            // Neutrals and other classes such as CS, ES, ET, BN, NSM that needs look ahead.
            // We keep a separate pointer for the start of neutrals and another pointer
            // for the those other classes (if needed since its resolution might be delayed).
            // Also, we need the last strong class for neutral resolution and the last
            // general class (that is not BN or MSM) for NSM resolution.

            // The simple idea of all actions is that we always resolve neutrals starting
            // from 'startOfNeutrals' and when we are sure about delayed weak type
            // resolution, we resolve it starting from 'startOfDelayed' else we point by
            // 'startOfNeutrals' as resolve it as neutral.
            int counter = 0;
            for (counter = 0; counter < runLength; counter++)
            {
                currentClass = characterClass[counter + classIndex];

                // We index action and next state table by class.
                // If we got a calss that should have been resolved already or a bogus
                // value, return what we were able to resolve so far.

                if (CharProperty[5, (int) currentClass]==0)
                {
                    return lengthResolved;
                }
                StateMachineAction action = Action[(int) state, (int)currentClass];

                // Need to record last numeric type so that when
                // we continue from a previous call, we can correctly resolve something
                // like L AN at the end of the first call and EN at the start of the
                // next call.

                if (CharProperty[4, (int) currentClass]==1)
                {
                    lastNumericClass = currentClass;
                }

                // If we have previousClassIsArabic flag set, we need its efect to
                // last only till the first strong character in the run.

                if(CharProperty[0, (int) currentClass]==1)
                {
                    previousClassIsArabic = false;
                }

                switch (action)
                {
                case StateMachineAction.ST_ST:
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                              "Cannot have unresolved neutrals. State: " +
                              state.ToString() +
                              ", Class: " + currentClass.ToString());

                    if (currentClass == DirectionClass.ArabicLetter)
                    {
                        characterClass[counter + classIndex] = DirectionClass.Right;
                    }

                    if (startOfDelayed != PositionInvalid)
                    {
                        startOfNeutrals = startOfDelayed;

                        ResolveNeutrals(characterClass,
                                        classIndex + startOfNeutrals,
                                        counter    - startOfNeutrals,
                                        ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                        characterClass[counter + classIndex],
                                        runLevel);

                        startOfNeutrals = startOfDelayed = PositionInvalid;
                    }

                    if ((currentClass != DirectionClass.ArabicNumber)  ||
                        ((currentClass == DirectionClass.ArabicNumber) &&
                         (lastStrongClass == DirectionClass.Right)))
                    {
                        lastStrongClass = currentClass;
                    }

                    if ((currentClass == DirectionClass.ArabicNumber) &&
                        (lastStrongClass == DirectionClass.Left))
                    {
                        ArabicNumberAfterLeft = true;
                    }
                    else
                    {
                        ArabicNumberAfterLeft = false;
                    }

                    lastClass = currentClass;
                    break;

                case StateMachineAction.ST_ET:
                    Debug.Assert(startOfDelayed != PositionInvalid,
                             "Must have delayed weak classes. State: " +
                             state.ToString() +
                             ", Class: "+ currentClass.ToString());

                    if (startOfNeutrals == PositionInvalid)
                    {
                       startOfNeutrals =  startOfDelayed;
                    }

                    if (currentClass == DirectionClass.ArabicLetter)
                    {
                        characterClass[counter + classIndex] = DirectionClass.Right;
                    }

                    ResolveNeutrals(characterClass,
                                    classIndex + startOfNeutrals,
                                    counter    - startOfNeutrals,
                                    ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                    characterClass[counter + classIndex],
                                    runLevel);

                    startOfNeutrals = startOfDelayed = PositionInvalid;

                    if ((currentClass != DirectionClass.ArabicNumber) ||
                        ((currentClass == DirectionClass.ArabicNumber) &&
                         (lastStrongClass == DirectionClass.Right)))
                    {
                        lastStrongClass = currentClass;
                    }

                    if ((currentClass == DirectionClass.ArabicNumber) &&
                        (lastStrongClass == DirectionClass.Left))
                    {
                        ArabicNumberAfterLeft = true;
                    }
                    else
                    {
                        ArabicNumberAfterLeft = false;
                    }
                    lastClass = currentClass;
                    break;

                case StateMachineAction.ST_NUMSEP:
                    {
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() +
                             ", Class: "+ currentClass.ToString());

                    Debug.Assert(startOfDelayed != PositionInvalid,
                             "Must have delayed weak classes. State: " +
                             state.ToString() +
                             " Class: "+ currentClass.ToString());
                    bool processed = false;

                    if (currentClass == DirectionClass.ArabicLetter)
                    {
                        // Rule W3, change all AL to R. 
                        characterClass[counter + classIndex] = DirectionClass.Right;
                    }

                    if (((lastStrongClass == DirectionClass.ArabicLetter) || previousClassIsArabic) &&
                        ((currentClass == DirectionClass.EuropeanNumber && (flags & Flags.OverrideEuropeanNumberResolution) == 0) ||
                         (currentClass == DirectionClass.ArabicNumber)))
                    {
                        // Rule W2: Change EN to AN if it follows AL.
                        characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                        bool commonSeparator = true;
                        int  commonSeparatorCount = 0;

                        for (int i = startOfDelayed; i < counter; i++)
                        {
                            if (characterClass[i + classIndex] != DirectionClass.CommonSeparator &&
                                characterClass[i + classIndex] != DirectionClass.BoundaryNeutral)
                            {
                                commonSeparator = false;
                                break;
                            }

                            if (characterClass[i + classIndex] == DirectionClass.CommonSeparator )
                            {
                                commonSeparatorCount++;
                            }
}

                        if (commonSeparator && (commonSeparatorCount == 1))
                        {
                            // Rule W4: In sequence of AN CS AN, change CS to AN.
                            ChangeType(characterClass,
                                       classIndex + startOfDelayed,
                                       counter    -  startOfDelayed,
                                       characterClass[counter + classIndex]);

                            processed = true;
                        }
                    }
                    else if ((lastStrongClass == DirectionClass.Left) &&
                             (currentClass    == DirectionClass.EuropeanNumber))
                    {
                        // Rule W7: Change EN to L if it follows L.
                        characterClass[counter + classIndex] = DirectionClass.Left;
                    }

                    if (!processed)
                    {
                        startOfNeutrals =  startOfDelayed;

                        ResolveNeutrals(characterClass,
                                        classIndex + startOfNeutrals,
                                        counter    - startOfNeutrals,
                                        ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                        characterClass[counter + classIndex],
                                        runLevel);
                    }

                    startOfNeutrals = startOfDelayed = PositionInvalid;

                    if ((currentClass != DirectionClass.ArabicNumber)  ||
                        ((currentClass == DirectionClass.ArabicNumber) &&
                         (lastStrongClass == DirectionClass.Right)))
                    {
                        if (!(((lastStrongClass == DirectionClass.Left) ||
                                (lastStrongClass == DirectionClass.ArabicLetter)) &&
                                (currentClass == DirectionClass.EuropeanNumber)))
                        {
                            lastStrongClass = currentClass;
                        }
                    }

                    if ((currentClass == DirectionClass.ArabicNumber) &&
                        (lastStrongClass == DirectionClass.Left))
                    {
                        ArabicNumberAfterLeft = true;
                    }
                    else
                    {
                        ArabicNumberAfterLeft = false;
                    }

                    lastClass = currentClass;

                    if (characterClass[counter + classIndex] == DirectionClass.ArabicNumber)
                    {
                        currentClass = DirectionClass.ArabicNumber;
                    }
                    }
                    break;

                case StateMachineAction.ST_N:
                    Debug.Assert(startOfNeutrals != PositionInvalid,
                             "Must have unresolved neutrals. State: " +
                             state.ToString() +", Class: "+
                             currentClass.ToString());

                    if (currentClass == DirectionClass.ArabicLetter)
                    {
                        characterClass[counter + classIndex] = DirectionClass.Right;
                    }

                    ResolveNeutrals(characterClass,
                                    classIndex + startOfNeutrals,
                                    counter    - startOfNeutrals,
                                    ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                    characterClass[counter + classIndex],
                                    runLevel);

                    startOfNeutrals = startOfDelayed = PositionInvalid;

                    if ((currentClass != DirectionClass.ArabicNumber) ||
                        ((currentClass == DirectionClass.ArabicNumber) &&
                         (lastStrongClass == DirectionClass.Right)))
                    {
                        lastStrongClass = currentClass;
                    }

                    if ((currentClass == DirectionClass.ArabicNumber) &&
                        (lastStrongClass == DirectionClass.Left))
                    {
                        ArabicNumberAfterLeft = true;
                    }
                    else
                    {
                        ArabicNumberAfterLeft = false;
                    }
                    lastClass = currentClass;
                    break;

                case StateMachineAction.EN_N:
                    Debug.Assert(startOfNeutrals != PositionInvalid,
                             "Must have unresolved neutrals. State: " +
                             state.ToString() + ", Class: "+
                             currentClass.ToString());

                    if ((flags & Flags.OverrideEuropeanNumberResolution) == 0 &&
                            ((lastStrongClass == DirectionClass.ArabicLetter) ||
                             previousClassIsArabic)
                        )
                    {
                        // Rule W2: EN changes to AN if it follows AL.
                        characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                        currentClass            = DirectionClass.ArabicNumber;
                    }
                    else if (lastStrongClass == DirectionClass.Left)
                    {
                        // Rule W7: EN changes to L if it follows L.
                        characterClass[counter + classIndex] = DirectionClass.Left;
                    }

                    ResolveNeutrals(characterClass,
                                    classIndex + startOfNeutrals,
                                    counter    - startOfNeutrals,
                                    ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                    characterClass[counter + classIndex],
                                    runLevel);

                    startOfNeutrals = startOfDelayed = PositionInvalid;
                    ArabicNumberAfterLeft = false;
                    lastClass = currentClass;
                    break;

                case StateMachineAction.SEP_ST:
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    if (startOfDelayed != PositionInvalid)
                    {
                        startOfNeutrals = startOfDelayed;
                        startOfDelayed = PositionInvalid;
                    }
                    else
                    {
                        startOfNeutrals = counter;
                    }
                    lastClass = currentClass;
                    break;

                case StateMachineAction.CS_NUM:
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    if (startOfDelayed == PositionInvalid)
                    {
                        startOfDelayed = counter;
                    }
                    lastClass = currentClass;
                    break;

                case StateMachineAction.SEP_ET:
                    Debug.Assert(startOfDelayed != PositionInvalid,
                             "Must have delayed weak classes. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    if (startOfNeutrals == PositionInvalid)
                    {
                        startOfNeutrals = startOfDelayed;
                    }
                    startOfDelayed = PositionInvalid;
                    lastClass = DirectionClass.GenericNeutral;
                    break;

                case StateMachineAction.SEP_NUMSEP:
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    Debug.Assert(startOfDelayed != PositionInvalid,
                             "Must have delayed weak classes. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    startOfNeutrals = startOfDelayed;
                    startOfDelayed = PositionInvalid;
                    lastClass = DirectionClass.GenericNeutral;
                    break;

                case StateMachineAction.SEP_N:
                    Debug.Assert(startOfNeutrals != PositionInvalid,
                             "Must have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    startOfDelayed = PositionInvalid;
                    break;

                case StateMachineAction.ES_AN:
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    if (startOfDelayed != PositionInvalid)
                    {
                        startOfNeutrals = startOfDelayed;
                        startOfDelayed = PositionInvalid;
                    }
                    else
                    {
                        startOfNeutrals = counter;
                    }
                    lastClass = DirectionClass.GenericNeutral;
                    break;

                case StateMachineAction.ET_ET:
                    Debug.Assert(startOfDelayed != PositionInvalid,
                             "Must have delayed weak classes. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());
                    Debug.Assert(lastClass == DirectionClass.EuropeanTerminator,
                             "Last class must be ET. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());
                    break;

                case StateMachineAction.ET_NUMSEP:
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    Debug.Assert(startOfDelayed != PositionInvalid,
                             "Must have delayed weak classes. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    startOfNeutrals = startOfDelayed;
                    startOfDelayed = counter;
                    lastClass = currentClass;
                    break;

                case StateMachineAction.ET_EN:
                    if (startOfDelayed == PositionInvalid)
                    {
                        startOfDelayed = counter;
                    }

                    if (!((lastStrongClass == DirectionClass.ArabicLetter) || previousClassIsArabic))
                    {
                        if (lastStrongClass == DirectionClass.Left)
                        {
                            characterClass[counter + classIndex] = DirectionClass.Left;
                        }
                        else
                        {
                            characterClass[counter + classIndex] = DirectionClass.EuropeanNumber;
                        }

                        ChangeType(characterClass,
                                   classIndex + startOfDelayed,
                                   counter    -  startOfDelayed,
                                   characterClass[counter + classIndex]);

                    startOfDelayed = PositionInvalid;
                    }
                    lastClass = DirectionClass.EuropeanNumber;

                    // According to the rules W4, W5, and W6 If we have a sequence EN ET ES EN
                    // we should treat ES as ON

                    if ( counter<runLength-1        &&
                        (characterClass[counter + 1 + classIndex] == DirectionClass.EuropeanSeparator||
                         characterClass[counter + 1 + classIndex] == DirectionClass.CommonSeparator))
                    {
                        characterClass[counter + 1 + classIndex]  = DirectionClass.GenericNeutral;
                    }

                    break;

                case StateMachineAction.ET_N:
                    Debug.Assert(startOfNeutrals != PositionInvalid,
                             "Must have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    if (startOfDelayed == PositionInvalid)
                    {
                        startOfDelayed = counter;
                    }

                    lastClass = currentClass;
                    break;

                case StateMachineAction.NUM_NUMSEP:
                    Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    Debug.Assert(startOfDelayed != PositionInvalid,
                             "Must have delayed weak classes. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                    if ((lastStrongClass == DirectionClass.ArabicLetter) ||
                        previousClassIsArabic || ArabicNumberAfterLeft)
                    {   
                        if ((flags & Flags.OverrideEuropeanNumberResolution) == 0)
                        {
                            characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                        }
                    }
                    else if (lastStrongClass == DirectionClass.Left)
                    {
                        characterClass[counter + classIndex] = DirectionClass.Left;
                    }
                    else
                    {
                        lastStrongClass = currentClass;
                    }

                    ChangeType(characterClass,
                                classIndex + startOfDelayed,
                                counter    -  startOfDelayed,
                                characterClass[counter + classIndex]);

                    startOfDelayed = PositionInvalid;
                    lastClass = currentClass;
                    break;

               case StateMachineAction.EN_L:
                   Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                   if (lastStrongClass == DirectionClass.Left)
                   {
                       characterClass[counter + classIndex] = DirectionClass.Left;
                   }

                   if (startOfDelayed != PositionInvalid)
                   {
                       startOfNeutrals = startOfDelayed;

                       ResolveNeutrals(characterClass,
                                       classIndex + startOfNeutrals,
                                       counter    - startOfNeutrals,
                                       ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                       characterClass[counter + classIndex],
                                       runLevel);

                       startOfNeutrals = startOfDelayed = PositionInvalid;
                   }
                   lastClass = currentClass;
                   break;

               case StateMachineAction.NUM_NUM:
                   Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                   if ((flags & Flags.OverrideEuropeanNumberResolution) == 0 &&
                       (lastStrongClass == DirectionClass.ArabicLetter || previousClassIsArabic)
                      ) 
                   {
                       // W2: EN changes to AN if it follows AL.
                       characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                       currentClass                         = DirectionClass.ArabicNumber;
                   }
                   else if (lastStrongClass == DirectionClass.Left)
                   {
                       characterClass[counter + classIndex] = DirectionClass.Left;
                   }

                   if (startOfDelayed != PositionInvalid)
                   {
                       startOfNeutrals = startOfDelayed;

                       ResolveNeutrals(characterClass,
                                       classIndex + startOfNeutrals,
                                       counter    - startOfNeutrals,
                                       ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                       characterClass[counter + classIndex],
                                       runLevel);

                       startOfNeutrals = startOfDelayed = PositionInvalid;
                   }

                   if ( (currentClass == DirectionClass.ArabicNumber) &&
                        (lastStrongClass == DirectionClass.Left))
                   {
                       ArabicNumberAfterLeft = true;
                   }
                   else
                   {
                       ArabicNumberAfterLeft = false;
                   }
                   lastClass = currentClass;
                   break;

               case StateMachineAction.EN_AL:
                   Debug.Assert(startOfNeutrals == PositionInvalid,
                             "Cannot have unresolved neutrals. State: " +
                             state.ToString() + ", Class: " +
                             currentClass.ToString());

                   if ((flags & Flags.OverrideEuropeanNumberResolution) == 0)
                   {
                       // W2: EN changes to AN if it follows AL.
                       // We will go onto Arabic number state (S_AN). 
                       characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                   }
                   else
                   {
                       // Change the current state such that we will go onto European number state (S_EN) 
                       // instead of Arabic number state (S_AN). As rule W2 is ignored, "EN following AL" 
                       // is the same as "EN following L".
                       state = StateMachineState.S_L;
                   }

                   if (startOfDelayed != PositionInvalid)
                   {
                       startOfNeutrals = startOfDelayed;

                       ResolveNeutrals(characterClass,
                                       classIndex + startOfNeutrals,
                                       counter    - startOfNeutrals,
                                       ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                       characterClass[counter + classIndex],
                                       runLevel);

                       startOfNeutrals = startOfDelayed = PositionInvalid;
                   }
                   lastClass = characterClass[counter + classIndex];
                   break;

               case StateMachineAction.EN_ET:
                   Debug.Assert(startOfDelayed != PositionInvalid,
                            "Must have delayed weak classes. State: " +
                            state.ToString() + ", Class: " +
                            currentClass.ToString());

                   if ((lastStrongClass == DirectionClass.ArabicLetter) ||
                        previousClassIsArabic)
                   {
                       if ((flags & Flags.OverrideEuropeanNumberResolution) == 0)
                       {
                           // W2: EN changes to AN if it follows AL
                           characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                           currentClass = DirectionClass.ArabicNumber;
                       }

                       if (startOfNeutrals == PositionInvalid)
                       {
                           ResolveNeutrals(characterClass,
                                           classIndex + startOfDelayed,
                                           counter    - startOfDelayed,
                                           ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                           characterClass[counter + classIndex],
                                           runLevel);
                       }
                       else
                       {
                           ResolveNeutrals(characterClass,
                                           classIndex + startOfNeutrals,
                                           counter    - startOfNeutrals,
                                           ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                           characterClass[counter + classIndex],
                                           runLevel);
                       }
                   }
                   else if (lastStrongClass == DirectionClass.Left)
                   {
                       characterClass[counter + classIndex] = DirectionClass.Left;

                       ChangeType(characterClass,
                                  classIndex + startOfDelayed,
                                  counter -  startOfDelayed,
                                  characterClass[counter + classIndex]);

                       if (startOfNeutrals != PositionInvalid)
                       {
                           ResolveNeutrals(characterClass,
                                           classIndex + startOfNeutrals,
                                           startOfDelayed -  startOfNeutrals,
                                           ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                           characterClass[counter + classIndex],
                                           runLevel);
                       }
                       ArabicNumberAfterLeft = false;
                   }
                   else
                   {
                       ChangeType(characterClass,
                                  classIndex + startOfDelayed,
                                  counter -  startOfDelayed,
                                  DirectionClass.EuropeanNumber);

                       if (startOfNeutrals != PositionInvalid)
                       {
                           ResolveNeutrals(characterClass,
                                           classIndex + startOfNeutrals,
                                           startOfDelayed -  startOfNeutrals,
                                           ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                           currentClass,
                                           runLevel);
                       }
                   }
                   startOfNeutrals = startOfDelayed = PositionInvalid;
                   lastClass = currentClass;
                   break;

               case StateMachineAction.BN_ST:
                   if (startOfDelayed == PositionInvalid)
                   {
                       startOfDelayed = counter;
                   }
                   break;

               case StateMachineAction.NSM_ST:
                    // Here is an NSM (non-space-mark) followed by a Strong.
                    // We can always resolve the NSM to its final class
                    if ((lastStrongClass == DirectionClass.ArabicLetter))
                    {
                        if (lastClass == DirectionClass.EuropeanNumber)
                        {
                            if ((flags & Flags.OverrideEuropeanNumberResolution) == 0)
                            {
                                // Applying rule W1 & W2
                                // W1: NSM changes to the class of previous char
                                // W2: EN following AL changes to AN
                                characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                            }
                            else
                            {
                                // Just apply rule W1.
                                characterClass[counter + classIndex] = DirectionClass.EuropeanNumber;
                            }
                        }
                        else if (lastClass != DirectionClass.ArabicNumber)
                        {
                            // Rule W3: AL is considered as R.
                            characterClass[counter + classIndex] = DirectionClass.Right;
                        }
                        else
                        {
                            // last char is an AN.
                            characterClass[counter + classIndex] = DirectionClass.ArabicNumber;
                        }
                    }
                    else
                    {
                        characterClass[counter + classIndex] = ArabicNumberAfterLeft
                                                    || lastClass == DirectionClass.ArabicNumber ?
                                                    DirectionClass.ArabicNumber :
                                                        lastClass==DirectionClass.EuropeanNumber &&
                                                        lastStrongClass != DirectionClass.Left
                                                        ? DirectionClass.EuropeanNumber  : lastStrongClass;
                    }

                    if (startOfDelayed != PositionInvalid)
                    {
                        // Resolve delayed characters. This happens when
                        // there is BN in between of last strong and this NSM
                        ChangeType(characterClass,
                                    classIndex + startOfDelayed,
                                    counter -  startOfDelayed,
                                    characterClass[counter + classIndex]);

                        startOfDelayed = PositionInvalid;
                    }

                    break;

               case StateMachineAction.NSM_ET:
                   characterClass[counter + classIndex] = lastClass;
                   break;

               case StateMachineAction.N_ST:
                   Debug.Assert(startOfNeutrals == PositionInvalid,
                            "Cannot have unresolved neutrals. State: " +
                            state.ToString() + ", Class: " +
                            currentClass.ToString());

                   if (startOfDelayed != PositionInvalid)
                   {
                       startOfNeutrals = startOfDelayed;
                       startOfDelayed = PositionInvalid;
                   }
                   else
                   {
                       startOfNeutrals = counter;
                   }
                   lastClass = currentClass;
                   break;

               case StateMachineAction.N_ET:

                   // Note that this state is used for N_N as well.

                   if (startOfNeutrals == PositionInvalid)
                   {
                       if (startOfDelayed != PositionInvalid)
                       {
                           startOfNeutrals = startOfDelayed;
                       }
                   }
                   startOfDelayed = PositionInvalid;
                   lastClass = currentClass;
                   break;
                };

                // Fetch next state.

                state = NextState[(int)state, (int)currentClass];

                lengthResolved = Math.Max(startOfNeutrals, startOfDelayed) == PositionInvalid ?
                                 counter + 1 :
                                 ((Math.Min(startOfNeutrals, startOfDelayed) == PositionInvalid) ?
                                 (Math.Max(startOfNeutrals, startOfDelayed)) :
                                 (Math.Min(startOfNeutrals, startOfDelayed)));
            }


            // If the caller flagged this run as incomplete
            // return the maximun that we could resolve so far and the last strong (fixed)
            // class saved

            if (stateOut != null)
            {
                stateOut.LastStrongClass = lastStrongClass;
                stateOut.LastNumberClass = lastNumericClass;
                return lengthResolved;
            }

            // Else, resolve remaining neutrals or delayed classes.
            // Resolve as neutrals based on eor.

            else
            {
                if (lengthResolved != counter)

                ResolveNeutrals(characterClass,
                                classIndex + lengthResolved,
                                counter -  lengthResolved,
                                ArabicNumberAfterLeft ? DirectionClass.ArabicNumber : lastStrongClass,
                                eor,
                                runLevel);

                return counter;
            }
        }

        static private void ResolveImplictLevels(
            IList<DirectionClass>   characterClass,     // [IN / OUT]
            CharacterBuffer         charBuffer,         // [IN]
            int                     ichText,            // [IN]
            int                     runLength,          // [IN]
            IList<byte>             levels,             // [IN / OUT]
            int                     index, 
            byte                    paragraphEmbeddingLevel // [IN] Paragraph base level
        )
        {
            if (runLength == 0)
            {
                return;
            }

            int counter = 0;

            for (counter = runLength -1; counter >= 0; counter--)
            {
                 Invariant.Assert(CharProperty[3, (int) characterClass[counter+index]]==1,
                ("Cannot have unresolved classes during implict levels resolution"));

                int intChar = charBuffer[ichText + index+counter];
                int wordCount = 1;
                if (((intChar & 0xFC00) == 0xDC00) && counter > 0)
                {
                    if ((charBuffer[ichText + index+counter-1] & 0xFC00) == 0xD800)
                    {
                        intChar = ((((charBuffer[ichText + index+counter-1] & 0x03ff) << 10) | (charBuffer[ichText + index+counter] & 0x3ff)) + 0x10000);
                        wordCount = 2;
                    }
                }

                DirectionClass directionClass;
                directionClass = Classification.CharAttributeOf((int) Classification.GetUnicodeClass(intChar)).BiDi;

                if (directionClass == DirectionClass.ParagraphSeparator ||
                    directionClass == DirectionClass.SegmentSeparator)
                {
                    // Rule L1: segment separator and paragraph separator should use paragraph embedding level
                    levels[counter+index] = paragraphEmbeddingLevel;
                }
                else
                {
                    levels[counter+index] =
                    (byte)((ImplictPush[Helper.IsOdd(levels[counter+index]) ? 1 : 0,
                                        (int)characterClass[index+counter]]) + levels[counter+index]);
                }

                if (wordCount > 1)
                {
                    levels[counter+index-1] = levels[counter+index];
                    counter--;
                }
            }
        }


        /// <Remark>
        /// Analyze() is created to serve the testing only. it is protected by security attribute to insure
        /// that BidiTest application only can call this method. the public key in the security attribute is
        /// generated from the BidiTest assembly by the command "sn -Tp biditest.exe"
        /// </Remark>
        static public bool Analyze(
            char []         chars,              // input text to be analyzed
            int             cchText,            // number of input char
            int             cchTextMaxHint,     // hint maximum number of char processed
            Flags           flags,              // control flags
            State           state,              // bidi state in, out or both
            out byte[]      levels,             // resolved level per char
            out int         cchResolved         // number of char resolved
            )
        {
            DirectionClass[] characterClass = new DirectionClass[cchText];
            levels = new byte[cchText];
            

            return Bidi.BidiAnalyzeInternal(
                new CharArrayCharacterBuffer(chars),
                0,
                cchText,
                cchTextMaxHint,
                flags,
                state,
                levels,
                new PartialArray<DirectionClass>(characterClass),
                out cchResolved
                );
        }

        static internal bool BidiAnalyzeInternal(
            CharacterBuffer         charBuffer,         // character buffer
            int                     ichText,            // offset to first char in the buffer
            int                     cchText,            // number of input char
            int                     cchTextMaxHint,     // hint maximum number of char processed
            Flags                   flags,              // control flags
            State                   state,              // bidi state in, out or both
            IList<byte>             levels,             // [IN/OUT] resolved level per char
            IList<DirectionClass>   characterClass,     // [IN/OUT] direction class of each char
            out int                 cchResolved         // number of char resolved
            )
        {
            DirectionClass          tempClass;
            int                 []  runLimits;
            State                   stateIn = null, stateOut = null; // both can point into state parameter
            ulong                   overrideStatus;
            OverrideClass           overrideClass;
            ushort                  stackOverflow;
            byte                    baseLevel;
            byte                    lastRunLevel;
            byte                    lastNonBnLevel;
            int                     counter;
            int                     codePoint;
            int                     lengthUnresolved = 0;
            int                     controlStack     = 0;
            int                     runCount         = 0;
            int                     wordCount;

            Invariant.Assert(levels != null && levels.Count >= cchText);
            Invariant.Assert(characterClass != null && characterClass.Count >= cchText);

            cchResolved = 0;

            // Verifying input parameters.
            if(charBuffer == null || (cchText <= 0) || (charBuffer.Count < cchText) ||
                ((((flags & Flags.ContinueAnalysis)!=0) || ((flags & Flags.IncompleteText)!=0)) && (state == null)))                          
            {
                return false;
            }

            // try to be smart to get the maximum we need to process.
            if ((flags & Flags.MaximumHint) != 0 && cchTextMaxHint>0 && cchTextMaxHint < cchText)
            {
                if (cchTextMaxHint>1 && (charBuffer[ichText + cchTextMaxHint-2] & 0xFC00) == 0xD800)
                {
                    // it might be surrogate pair
                    cchTextMaxHint--;
                }

                int  index = cchTextMaxHint-1;
                int  intChar = charBuffer[ichText + index];
                wordCount = 1;
                if ((intChar & 0xFC00) == 0xD800)
                {
                    intChar = DoubleWideChar.GetChar(charBuffer, ichText, cchText, index, out wordCount);
                }

                tempClass  = Classification.CharAttributeOf((int) Classification.GetUnicodeClass(intChar)).BiDi;

                index +=  wordCount;

                if (CharProperty[1, (int) tempClass]==1)
                {
                    // if we got more than 20 same strong charcaters class, we give up. we might
                    // get this case with Thai script.

                    while (index<cchText && index-cchTextMaxHint<20)
                    {
                        intChar = charBuffer[ichText + index];
                        wordCount = 1;

                        if ((intChar & 0xFC00) == 0xD800)
                        {
                            intChar = DoubleWideChar.GetChar(charBuffer, ichText, cchText, index, out wordCount);
                        }

                        if (tempClass != Classification.CharAttributeOf((int) Classification.GetUnicodeClass(intChar)).BiDi)
                        {
                            break;
                        }
                        else
                        {
                           index +=  wordCount;
                        }
                    }
                }
                else
                {
                    // we got neutral try to get first strong character.
                    while (index<cchText)
                    {
                        intChar = charBuffer[ichText + index];
                        wordCount = 1;
                        if ((intChar & 0xFC00) == 0xD800)
                        {
                            intChar = DoubleWideChar.GetChar(charBuffer, ichText, cchText, index, out wordCount);
                        }

                        if (CharProperty[1,
                                (int) Classification.CharAttributeOf((int) Classification.GetUnicodeClass(intChar)).BiDi] == 1)
                        break;

                        index +=  wordCount;
                    }
                    index++; // include the first strong character to be able to resolve the neutrals
                }

                cchText = Math.Min(cchText, index);
            }

            // If the last character in the string is a paragraph terminator,
            // we can analyze the whole string, No need to use state parameter
            // for output
            BidiStack               levelsStack = new BidiStack();

            if ((flags & Flags.IncompleteText) != 0)
            {
                codePoint = charBuffer[ichText + cchText -1];

                if((cchText > 1) && ((charBuffer[ichText + cchText -2] & 0xFC00 ) == 0xD800) &&  ((charBuffer[ichText + cchText - 1] & 0xFC00) == 0xDC00))
                {
                   codePoint = 0x10000 + (((charBuffer[ichText + cchText -2] & 0x3ff) << 10) | (charBuffer[ichText + cchText - 1] & 0x3ff));
                }

                if (DirectionClass.ParagraphSeparator != Classification.CharAttributeOf((int) Classification.GetUnicodeClass(codePoint)).BiDi)
                {
                    stateOut = state;
                }
}

            if ((flags & Flags.ContinueAnalysis) != 0)
            {
                // try to see if we have enough information to start the analysis or we need to get more.
                codePoint = charBuffer[ichText + 0];
                if((cchText > 1) && ((charBuffer[ichText + 0] & 0xFC00 ) == 0xD800) && ((charBuffer[ichText + 1] & 0xFC00) == 0xDC00))
                {
                   codePoint = 0x10000 + (((charBuffer[ichText + 0] & 0x3ff) << 10) | (charBuffer[ichText + 1] & 0x3ff));
                }

                tempClass = Classification.CharAttributeOf((int) Classification.GetUnicodeClass(codePoint)).BiDi;

                // state can be used as in/out parameter
                stateIn = state;

                // Note: use the state instant to call LastStrongClass or LastStrongOrNumberClass
                // which should be overrided by the caller.

                switch (tempClass)
                {
                    case DirectionClass.Left:
                    case DirectionClass.Right:
                    case DirectionClass.ArabicNumber:
                    case DirectionClass.ArabicLetter:
                        stateIn.LastNumberClass     = tempClass;
                        stateIn.LastStrongClass     = tempClass;
                        break;

                    case DirectionClass.EuropeanNumber:
                        stateIn.LastNumberClass     = tempClass;
                        break;
                }
            }

            // Done with the state


            if (stateIn != null)
            {
                if (!levelsStack.Init(stateIn.LevelStack))
                {
                    cchResolved = 0;
                    return false;
                }

                baseLevel = levelsStack.GetCurrentLevel();
                stackOverflow = stateIn.Overflow;
                overrideStatus = stateIn.OverrideLevels;

                overrideClass = (Helper.IsBitSet(overrideStatus, baseLevel)) ?
                                    (Helper.IsOdd(baseLevel) ?
                                    OverrideClass.OverrideClassRight :
                                    OverrideClass.OverrideClassLeft):
                                 OverrideClass.OverrideClassNeutral;
            }
            else
            {
                baseLevel = BaseLevelLeft;

                if ((flags & Flags.FirstStrongAsBaseDirection) != 0)
                {
                    // Find strong character in the first paragraph
                    // This might cause a complete pass over the input string
                    // but we must get it before we start.

                    DirectionClass firstStrong = DirectionClass.ClassInvalid;

                    if (GetFirstStrongCharacter(charBuffer, ichText, cchText, ref firstStrong))
                    {
                        if (firstStrong != DirectionClass.Left)
                        {
                            baseLevel = BaseLevelRight;
                        }
                    }
                }
                else if ((flags & Flags.DirectionRightToLeft) != 0)
                {
                    baseLevel = BaseLevelRight;
                }

                levelsStack.Init((ulong) baseLevel + 1);
                stackOverflow = 0;
                // Initialize to neutral
                overrideStatus = 0;
                overrideClass = OverrideClass.OverrideClassNeutral;
            }

            byte paragraphEmbeddingLevel = levelsStack.GetStackBottom();

            //
            // try to optimize through a fast path.
            //

            int                     neutralIndex = -1;
            byte                    bidiLevel, nonBidiLevel;
            byte                    lastPathClass;
            byte                    basePathClass;
            byte                    neutralLevel;
            DirectionClass          lastStrongClass;

            if (Helper.IsOdd(baseLevel))
            {
                bidiLevel       = baseLevel;
                nonBidiLevel    = (byte) (baseLevel + 1);
                lastPathClass   = basePathClass = 3;
                if (stateIn != null )
                    lastStrongClass = stateIn.LastStrongClass;
                else
                    lastStrongClass = DirectionClass.Right;
            }
            else
            {
                nonBidiLevel    = baseLevel;
                bidiLevel       = (byte) (baseLevel + 1);
                lastPathClass   = basePathClass = 2;
                if (stateIn != null )
                    lastStrongClass = stateIn.LastStrongClass;
                else
                    lastStrongClass = DirectionClass.Left;
            }

            if (stateIn != null )
            {
                if ((FastPathClass[(int) lastStrongClass] & 0x02) == 0x02) // Strong Left or Right
                {
                    lastPathClass = FastPathClass[(int) lastStrongClass];
                }
            }

            //
            // Hidden text do not affect the relative order of surrounding text. We do that by
            // assigning them to the class type of either the preceding or following non-hidden text 
            // so that they won't cause additional transitions of Bidi levels.
            // 

            DirectionClass hiddenCharClass = DirectionClass.GenericNeutral;             
            counter = 0;            
            wordCount = 1;

            // In case the input starts with hidden characters, we will assign them to the class of the following 
            // non-hidden cp. The for-loop scans forward till the 1st non-hidden cp and remembers its bidi class 
            // to be used in case there are hidden cp at the beginning of the input. 
            for (int i = counter; i < cchText; i += wordCount)
            {
                int intChar = charBuffer[ichText + i];
                if ((intChar & 0xFC00) == 0xD800)
                {
                    intChar = DoubleWideChar.GetChar(charBuffer, ichText, cchText, counter, out wordCount);
                }                

                if (intChar != CharHidden)
                {
                    hiddenCharClass = Classification.CharAttributeOf((int)Classification.GetUnicodeClass(intChar)).BiDi;
                    if (  hiddenCharClass == DirectionClass.EuropeanNumber
                       && (flags & Flags.OverrideEuropeanNumberResolution) != 0)
                    {
                        hiddenCharClass = characterClass[i]; // In case EN resolution is overridden. 
                    }
                    break;
                }
            }            

            while (counter < cchText)
            {
                // Account for surrogate characters
                wordCount = 1;
                codePoint = DoubleWideChar.GetChar(charBuffer, ichText, cchText, counter, out wordCount);                

                tempClass = Classification.CharAttributeOf((int) Classification.GetUnicodeClass(codePoint)).BiDi;

                if (codePoint == CharHidden)
                {
                    tempClass = hiddenCharClass;
                }

                if (FastPathClass[(int) tempClass] == 0)
                    break;

                // The directional class can be processed in fast path. It will not be EN or AN and hence not 
                // overridable.
                characterClass[counter] = tempClass;   
                hiddenCharClass = tempClass; 

                if (FastPathClass[(int) tempClass] == 1)  // Neutral
                {
                    if (tempClass != DirectionClass.EuropeanSeparator && tempClass != DirectionClass.CommonSeparator)
                        characterClass[counter] = DirectionClass.GenericNeutral;
                    if (neutralIndex == -1)
                        neutralIndex = counter;
                }
                else // strong class (2, 3, or 4)
                {
                    if (neutralIndex != -1) // resolve the neutral
                    {
                        if (lastPathClass != FastPathClass[(int) tempClass])
                        {
                            neutralLevel = baseLevel;
                        }
                        else
                        {
                            neutralLevel = lastPathClass == 2 ? nonBidiLevel : bidiLevel;
                        }

                        while (neutralIndex < counter)
                        {
                            levels[neutralIndex] = neutralLevel;
                            neutralIndex++;
                        }
                        neutralIndex = -1;
                    }

                    lastPathClass = FastPathClass[(int) tempClass];

                    levels[counter] = lastPathClass == 2 ? nonBidiLevel : bidiLevel;

                    if (wordCount == 2)
                    {
                        // Higher and Lower surrogate should have the same bidi level.
                        levels[counter + 1] = levels[counter];
                    }
                    
                    lastStrongClass = tempClass;
                }

                counter = counter + wordCount;
            }

            if (counter < cchText)  // couldn't optimize.
            {
                // reset the levels
                for (int j=0; j<counter; j++)
                    levels[j] = baseLevel;
            }
            else
            {
                cchResolved = cchText;

                if (state != null)
                {
                    state.LastStrongClass = lastStrongClass;
                }

                if (neutralIndex != -1) // resolve the neutral
                {
                    if ((flags & Flags.IncompleteText) == 0)
                    {
                        if (lastPathClass != basePathClass)
                        {
                            neutralLevel = baseLevel;
                        }
                        else
                        {
                            neutralLevel = lastPathClass == 2 ? nonBidiLevel : bidiLevel;
                        }

                        while (neutralIndex < cchText)
                        {
                            levels[neutralIndex] = neutralLevel;
                            neutralIndex++;
                        }
                    }
                    else
                    {
                        cchResolved = neutralIndex;
                    }
                }

                return true;
}

            //
            // end fast path
            //

            // Get character classifications.
            // Resolve explicit embedding levels.
            // Record run limits (either due to a level change or due to new paragraph)

            lastNonBnLevel = baseLevel;

            // for the worst case of all paragraph terminators string.
            runLimits = new int[cchText];

            // counter is already initialized in the fast path
            while (counter < cchText)
            {
                int intChar = charBuffer[ichText + counter];
                wordCount = 1;
                if ((intChar & 0xFC00) == 0xD800)
                {
                    intChar = DoubleWideChar.GetChar(charBuffer, ichText, cchText, counter, out wordCount);
                }

                DirectionClass currentClass;

                currentClass = Classification.CharAttributeOf((int) Classification.GetUnicodeClass(intChar)).BiDi;

                levels[counter] = levelsStack.GetCurrentLevel();

                if (intChar == CharHidden)
                {
                    currentClass = hiddenCharClass;
                }

                switch(currentClass)
                {
                case DirectionClass.ParagraphSeparator:
                    // mark output level array with a special mark
                    // to seperate between paragraphs

                    levels[counter] = ParagraphTerminatorLevel;
                    runLimits[runCount] = counter;
                    if (counter != cchText-1)
                    {
                        runCount++;
                    }
                    levelsStack.Init((ulong) baseLevel + 1);
                    overrideStatus = 0;
                    overrideClass =  OverrideClass.OverrideClassNeutral;
                    stackOverflow = 0;
                    controlStack = 0;
                    goto case DirectionClass.OtherNeutral;
                    // Fall through

                // We keep our Unicode classification table stictly following Unicode
                // regarding neutral types (B, S, WS, ON), change all to generic N.

                case DirectionClass.SegmentSeparator:
                case DirectionClass.WhiteSpace:
                case DirectionClass.OtherNeutral:
                    characterClass[counter] = DirectionClass.GenericNeutral;

                    if (counter>0 && characterClass[counter-1] == DirectionClass.BoundaryNeutral)
                    {
                        if (levels[counter-1] < levels[counter] && levels[counter] != ParagraphTerminatorLevel)
                        {
                            levels[counter-1] = levels[counter];
                        }
                    }
                    controlStack = 0;

                    break;

                case DirectionClass.LeftToRightEmbedding:
                case DirectionClass.RightToLeftEmbedding:
                    characterClass[counter] = DirectionClass.BoundaryNeutral;

                    if ((flags & Flags.IgnoreDirectionalControls) != 0)
                        break;  // Ignore directional controls. They won't affect bidi state

                    // If we overflowed the stack, keep track of this in order to know when you hit
                    // a PDF if you should pop or not.

                    if(!levelsStack.Push(currentClass == DirectionClass.LeftToRightEmbedding ? true : false))
                    {
                      stackOverflow++;
                    }
                    else
                    {
                        runLimits[runCount] = counter;
                        if (counter != cchText-1)
                        {
                            runCount++;
                        }
                        controlStack++;
                    }
                    overrideClass =  OverrideClass.OverrideClassNeutral;

                    levels[counter] = lastNonBnLevel;

                    break;

                case DirectionClass.LeftToRightOverride:
                case DirectionClass.RightToLeftOverride:                
                    characterClass[counter] = DirectionClass.BoundaryNeutral;
                    
                    if ((flags & Flags.IgnoreDirectionalControls) != 0)
                        break;  // Ignore directional controls. They won't affect bidi state
                    
                    if(!levelsStack.Push(currentClass == DirectionClass.LeftToRightOverride ? true : false))
                    {
                      stackOverflow++;
                    }
                    else
                    {
                        // Set the matching bit of 'overrideStatus' to one
                        // in order to know when you pop if you're in override state or not.

                        Helper.ResetBit(ref overrideStatus, levelsStack.GetCurrentLevel());
                        overrideClass = (currentClass == DirectionClass.LeftToRightOverride) ?
                                         OverrideClass.OverrideClassLeft : OverrideClass.OverrideClassRight;
                        runLimits[runCount] = counter;
                        if (counter != cchText-1)
                        {
                            runCount++;
                        }
                        controlStack++;
                    }

                    levels[counter] = lastNonBnLevel;
                    break;

                case DirectionClass.PopDirectionalFormat:
                    characterClass[counter] = DirectionClass.BoundaryNeutral;

                    if ((flags & Flags.IgnoreDirectionalControls) != 0)
                        break;  // Ignore directional controls. They won't affect bidi state
                    
                    if (stackOverflow != 0)
                    {
                        stackOverflow--;
                    }
                    else
                    {
                        if (levelsStack.Pop())
                        {
                            int newLevel = levelsStack.GetCurrentLevel();

                            // Override state being left or right is determined
                            // from the new level being even or odd.

                            overrideClass = (Helper.IsBitSet(overrideStatus, newLevel)) ? (Helper.IsOdd(newLevel) ?
                                            OverrideClass.OverrideClassRight : OverrideClass.OverrideClassLeft):
                                            OverrideClass.OverrideClassNeutral;

                            if (controlStack > 0)
                            {
                                runCount--;
                                controlStack--;
                            }
                            else
                            {
                                runLimits[runCount] = counter;
                                if (counter != cchText-1)
                                {
                                    runCount++;
                                }
                            }
                        }
                    }

                    levels[counter] = lastNonBnLevel;
                    break;
                    
                default:
                    controlStack = 0;
                    
                    if (   currentClass == DirectionClass.EuropeanNumber 
                        && (flags & Flags.OverrideEuropeanNumberResolution) != 0)
                    {
                        // Use the input DirectionClass explictly for EN. We don't 
                        // need to copy the the Unicode classification data into it.
                        // However, assert that the input DirectionClass must be either be AN or EN
                        Invariant.Assert(characterClass[counter] == DirectionClass.ArabicNumber || characterClass[counter] == DirectionClass.EuropeanNumber);
                    }
                    else
                    {
                        // Non EuropeanNumber is not affected by the input DirectionClass.
                        characterClass[counter] = currentClass;
                    }

                    if(overrideClass != OverrideClass.OverrideClassNeutral)
                    {
                        characterClass[counter] = (overrideClass == OverrideClass.OverrideClassLeft) ?
                                                  DirectionClass.Left : DirectionClass.Right;
                    }

                    if (counter>0 && characterClass[counter-1]==DirectionClass.BoundaryNeutral)
                    {
                        if (levels[counter-1] < levels[counter])
                        {
                            levels[counter-1] = levels[counter];
                        }
                    }
                    break;
                }

                lastNonBnLevel = levels[counter];

                if (wordCount > 1)
                {
                    levels[counter+1]           = levels[counter];
                    characterClass[counter+1]   = characterClass[counter];
                }

                hiddenCharClass = characterClass[counter];
                counter += wordCount;
            }

            runCount++;

            if (stateOut != null)
            {
                stateOut.LevelStack     = levelsStack.GetData();
                stateOut.OverrideLevels = overrideStatus;
                stateOut.Overflow       = stackOverflow;
            }

            // Resolve neutral and weak types.
            // Resolve implict levels.


            // The lastRunLevel will hold the level of last processed run to be used
            // to determine the sor of the next run. we can't depend on the level array
            // because it can be changed in case of numerics. so level of the numerics
            // will be increased by one or two.

            lastRunLevel = baseLevel;

            bool currenLimitIsParagraphTerminator;
            bool previousLimitIsParagraphTerminator = false;

            for(counter = 0; counter < runCount; counter++)
            {
                DirectionClass   sor;
                DirectionClass   eor;

                currenLimitIsParagraphTerminator = (levels[runLimits[counter]] == ParagraphTerminatorLevel);
                if (currenLimitIsParagraphTerminator)
                    levels[runLimits[counter]] = baseLevel;

                int runStart =  (counter == 0) ? 0 : runLimits[counter - 1] + 1;

                // If the level transition was due to a new paragraph
                // we don't want pass the paragraph terminator position.

                int offset = (counter != (runCount - 1)) ? (currenLimitIsParagraphTerminator ? 1 : 0) : 0;
                int runLength = (counter == (runCount - 1)) ?
                                (int) ((cchText - runStart) - offset):
                                (int) (runLimits[counter] - runStart) + 1 - offset;

                // See if we need to provide state information from a previous call
                // or need to save it for a possible next call

                bool incompleteRun = ((runCount - 1) == counter) && ((flags & Flags.IncompleteText) != 0)
                                     && (stateOut != null);
                bool continuingAnalysis = (counter == 0) && (stateIn != null);

                int runLengthResolved;

                // First run or a run after paragraph terminator.

                if ((counter == 0) || previousLimitIsParagraphTerminator)
                {
                    sor = Helper.IsOdd(Math.Max(baseLevel, levels[runStart])) ?
                            DirectionClass.Right : DirectionClass.Left;
                }
                else
                {
                    sor = Helper.IsOdd(Math.Max(lastRunLevel, levels[runStart])) ?
                          DirectionClass.Right : DirectionClass.Left;
                }

                lastRunLevel = levels[runStart];

                // Last run or a run just before paragraph terminator.

                if( ((runCount - 1) == counter) || currenLimitIsParagraphTerminator)
                {
                    eor = Helper.IsOdd(Math.Max(levels[runStart], baseLevel)) ?
                          DirectionClass.Right : DirectionClass.Left;
                }
                else
                {
                    // we will try to get first run which doesn't have just one
                    // control char like LRE,RLE,... and so on
                    int runNumber = counter+1;
                    while ( runNumber<runCount - 1 &&
                            runLimits[runNumber]-runLimits[runNumber-1]==1 &&
                            characterClass[runLimits[runNumber]] == DirectionClass.BoundaryNeutral)
                    {
                        runNumber++;
                    }

                    eor = Helper.IsOdd(Math.Max(levels[runStart], levels[runLimits[runNumber-1] + 1])) ?
                          DirectionClass.Right : DirectionClass.Left;
                }

                // If it is a continuation from a previous call, set sor
                // to the last stron type saved in the input state parameter.

                runLengthResolved = ResolveNeutralAndWeak(characterClass,
                                                          runStart,
                                                          runLength,
                                                          sor,
                                                          eor,
                                                          levels[runStart],
                                                          continuingAnalysis ? stateIn: null,
                                                          incompleteRun ? stateOut: null,
                                                          ((counter == 0) && (stateIn == null)) ?
                                                          ((flags & Flags.PreviousStrongIsArabic)!=0):
                                                          false,
                                                          flags);
                if (!incompleteRun)
                {
                    // If we in a complete run, we should be able to resolve everything
                    // unless we passed a corrupted data

                    Debug.Assert(runLengthResolved == runLength,
                                    "Failed to resolve neutrals and weaks. Run#:" +
                                    counter.ToString(CultureInfo.InvariantCulture));
}
                else
                {
                    lengthUnresolved =  (runLength - runLengthResolved);
                }

                // Resolve implict levels.
                // Also, takes care of Rule L1 (segment separators, paragraph separator,
                // white spaces at the end of the line.

                ResolveImplictLevels(characterClass,
                                     charBuffer,
                                     ichText,
                                     runLength - lengthUnresolved,
                                     levels,
                                     runStart,
                                     paragraphEmbeddingLevel);

                previousLimitIsParagraphTerminator = currenLimitIsParagraphTerminator;
            }

            cchResolved = cchText - lengthUnresolved;

            // if the charBuffer ended with paragraph seperator then we need to reset the Bidi state
            if (((flags & Flags.IncompleteText) != 0) && (stateOut == null))
            {
                state.OverrideLevels        = 0;
                state.Overflow              = 0;

                if ((paragraphEmbeddingLevel & 1) != 0)
                {
                    state.LastStrongClass   = DirectionClass.Right;
                    state.LastNumberClass   = DirectionClass.Right;
                    state.LevelStack        = Bidi.StackRtl;
                }
                else
                {
                    state.LastStrongClass   = DirectionClass.Left;
                    state.LastNumberClass   = DirectionClass.Left;
                    state.LevelStack        = Bidi.StackLtr;
                }
            }

            return true;
        }
    }


    /// <summary>
    /// DoubleWideChar convert word char into int char (handle Surrogate).
    /// </summary>
    internal static class DoubleWideChar
    {
        static internal int GetChar(
            CharacterBuffer charBuffer,
            int             ichText,
            int             cchText,
            int             charNumber,
            out int         wordCount)
        {
            if (charNumber < cchText-1 &&
                ((charBuffer[ichText + charNumber] & 0xFC00) == 0xD800) &&
                ((charBuffer[ichText + charNumber+1] & 0xFC00) == 0xDC00))
            {
                wordCount = 2;
                return ((((charBuffer[ichText + charNumber] & 0x03ff) << 10) | (charBuffer[ichText + charNumber+1] & 0x3ff)) + 0x10000);
            }
            wordCount = 1;
            return ((int) charBuffer[ichText + charNumber]);
        }
    }
}
