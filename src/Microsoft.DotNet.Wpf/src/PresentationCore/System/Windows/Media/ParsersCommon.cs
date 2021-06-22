// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Synopsis: Implements class Parsers for internal use of type converters
//
//            This file contains all the code that is shared between PresentationBuildTasks and PresentationCore
//
//            Changes to this file will likely result in a compiler update. 
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using MS.Internal;
using System.ComponentModel;
using System.Globalization;
using System.IO; 


#if PRESENTATION_CORE

using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.Media; 
using TypeConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace MS.Internal

#elif PBTCOMPILER

using MS.Utility ;
using MS.Internal.Markup;
using TypeConverterHelper = MS.Internal.Markup.TypeConverterHelper;

namespace MS.Internal.Markup

#endif
{
    internal static partial class Parsers
    {
#if !PBTCOMPILER
        internal static object DeserializeStreamGeometry( BinaryReader reader )
        {
            StreamGeometry geometry = new StreamGeometry();
            
            using (StreamGeometryContext context = geometry.Open())
            {
                ParserStreamGeometryContext.Deserialize( reader, context, geometry ); 
            }
            geometry.Freeze();

            return geometry; 
        }
#endif

        internal static void PathMinilanguageToBinary( BinaryWriter bw, string stringValue ) 
        {
            ParserStreamGeometryContext context = new ParserStreamGeometryContext( bw ); 
#if PRESENTATION_CORE 
            FillRule fillRule = FillRule.EvenOdd ; 
#else            
            bool fillRule = false  ; 
#endif
            ParseStringToStreamGeometryContext(context, stringValue, TypeConverterHelper.InvariantEnglishUS, ref fillRule);             
            context.SetFillRule( fillRule );                                  
            
            context.MarkEOF(); 
        }

        /// <summary>
        /// Parse a PathGeometry string.
        /// The PathGeometry syntax is the same as the PathFigureCollection syntax except that it
        /// may start with a "wsp*Fwsp*(0|1)" which indicate the winding mode (F0 is EvenOdd while
        /// F1 is NonZero).
        /// </summary>

#if !PBTCOMPILER 
        internal static Geometry ParseGeometry(
            string pathString,
            IFormatProvider formatProvider)
        {
            FillRule fillRule = FillRule.EvenOdd ;             
            StreamGeometry geometry = new StreamGeometry();
            StreamGeometryContext context = geometry.Open(); 

            ParseStringToStreamGeometryContext( context, pathString, formatProvider , ref fillRule ) ; 

            geometry.FillRule = fillRule ;                                          
            geometry.Freeze();

            return geometry;
        }
#endif
        //
        // Given a mini-language representation of a Geometry - write it to the 
        // supplied streamgeometrycontext
        // 

        private static void ParseStringToStreamGeometryContext ( 
            StreamGeometryContext context, 
            string pathString,
            IFormatProvider formatProvider, 
#if PRESENTATION_CORE            
            ref FillRule fillRule 
#else            
            ref bool fillRule 
#endif      
            )
        {
            using ( context )
            {
                // Check to ensure that there's something to parse
                if (pathString != null)
                {
                    int curIndex = 0;

                    // skip any leading space
                    while ((curIndex < pathString.Length) && Char.IsWhiteSpace(pathString, curIndex))
                    {
                        curIndex++;
                    }

                    // Is there anything to look at?
                    if (curIndex < pathString.Length)
                    {
                        // If so, we only care if the first non-WhiteSpace char encountered is 'F'
                        if (pathString[curIndex] == 'F')
                        {
                            curIndex++;

                            // Since we found 'F' the next non-WhiteSpace char must be 0 or 1 - look for it.
                            while ((curIndex < pathString.Length) && Char.IsWhiteSpace(pathString, curIndex))
                            {
                                curIndex++;
                            }

                            // If we ran out of text, this is an error, because 'F' cannot be specified without 0 or 1
                            // Also, if the next token isn't 0 or 1, this too is illegal
                            if ((curIndex == pathString.Length) ||
                                ((pathString[curIndex] != '0') &&
                                 (pathString[curIndex] != '1')))
                            {
                                throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
                            }
                            
#if PRESENTATION_CORE
                            fillRule = pathString[curIndex] == '0' ? FillRule.EvenOdd : FillRule.Nonzero;
#else
                            fillRule = pathString[curIndex] != '0' ; 

#endif

                            // Increment curIndex to point to the next char
                            curIndex++;
                        }
                    }

                    AbbreviatedGeometryParser parser = new AbbreviatedGeometryParser();
            
                    parser.ParseToGeometryContext(context, pathString, curIndex);
                }
            }
        }
    }
    
     /// <summary>
    /// Parser for XAML abbreviated geometry.
    /// SVG path spec is closely followed http://www.w3.org/TR/SVG11/paths.html
    /// 3/23/2006, new parser for performance (fyuan)
    /// </summary>
    sealed internal class AbbreviatedGeometryParser
    {
        const bool      AllowSign    = true;
        const bool      AllowComma   = true;
        const bool      IsFilled     = true;
        const bool      IsClosed     = true;
        const bool      IsStroked    = true;
        const bool      IsSmoothJoin = true;
        
        IFormatProvider _formatProvider;
        
        string          _pathString;        // Input string to be parsed
        int             _pathLength;
        int             _curIndex;          // Location to read next character from 
        bool            _figureStarted;     // StartFigure is effective
        
        Point           _lastStart;         // Last figure starting point
        Point           _lastPoint;         // Last point
        Point           _secondLastPoint;   // The point before last point
        
        char            _token;             // Non whitespace character returned by ReadToken
        
        StreamGeometryContext _context;
        
        /// <summary>
        /// Throw unexpected token exception
        /// </summary>
        private void ThrowBadToken()
        {
            throw new System.FormatException(SR.Get(SRID.Parser_UnexpectedToken, _pathString, _curIndex - 1));
        }

        bool More()
        {
            return _curIndex < _pathLength;
        }
        
        // Skip white space, one comma if allowed
        private bool SkipWhiteSpace(bool allowComma)
        {
            bool commaMet = false;
            
            while (More())
            {
                char ch = _pathString[_curIndex];
                
                switch (ch)
                {
                case ' ' :
                case '\n':
                case '\r':
                case '\t': // SVG whitespace
                    break;
            
                case ',':
                    if (allowComma)
                    {
                        commaMet   = true;
                        allowComma = false; // one comma only
                    }
                    else
                    {
                        ThrowBadToken();
                    }
                    break;
                    
                default:
                    // Avoid calling IsWhiteSpace for ch in (' ' .. 'z']
                    if (((ch >' ') && (ch <= 'z')) || ! Char.IsWhiteSpace(ch))
                    {
                        return commaMet;
                    }                        
                    break;
                }
                
                _curIndex ++;
            }
            
            return commaMet;
        }

        /// <summary>
        /// Read the next non whitespace character
        /// </summary>
        /// <returns>True if not end of string</returns>
        private bool ReadToken()
        {
            SkipWhiteSpace(!AllowComma);

            // Check for end of string
            if (More())
            {
                _token = _pathString[_curIndex ++];

                return true;
            }
            else
            {
                return false;
            }
        }
        
        private bool IsNumber(bool allowComma)
        {
            bool commaMet = SkipWhiteSpace(allowComma);
            
            if (More())
            {
                _token = _pathString[_curIndex];

                // Valid start of a number
                if ((_token == '.') || (_token == '-') || (_token == '+') || ((_token >= '0') && (_token <= '9'))
                    || (_token == 'I')  // Infinity
                    || (_token == 'N')) // NaN
                {
                    return true;
                }                    
            }

            if (commaMet) // Only allowed between numbers
            {
                ThrowBadToken();
            }
            
            return false;
        }
        
        void SkipDigits(bool signAllowed)
        {
            // Allow for a sign
            if (signAllowed && More() && ((_pathString[_curIndex] == '-') || _pathString[_curIndex] == '+'))
            {
                _curIndex++;
            }
        
            while (More() && (_pathString[_curIndex] >= '0') && (_pathString[_curIndex] <= '9'))
            {
                _curIndex ++;
            }
        }
        
//       
//         /// <summary>
//         /// See if the current token matches the string s. If so, advance and
//         /// return true. Else, return false.
//         /// </summary>
//         bool TryAdvance(string s)
//         {
//             Debug.Assert(s.Length != 0);
// 
//             bool match = false;
//             if (More() && _pathString[_currentIndex] == s[0])
//             {
//                 //
//                 // Don't bother reading subsequent characters, as the CLR parser will
//                 // do this for us later.
//                 //
//                 _currentIndex = Math.Min(_currentIndex + s.Length, _pathLength);
// 
//                 match = true;
//             }
// 
//             return match;
//         }
// 
      
        /// <summary>
        /// Read a floating point number
        /// </summary>
        /// <returns></returns>
        double ReadNumber(bool allowComma)
        {
            if (!IsNumber(allowComma))
            {
                ThrowBadToken();
            }                
            
            bool simple = true;
            int start = _curIndex;
            
            //
            // Allow for a sign
            // 
            // There are numbers that cannot be preceded with a sign, for instance, -NaN, but it's
            // fine to ignore that at this point, since the CLR parser will catch this later.
            //
            if (More() && ((_pathString[_curIndex] == '-') || _pathString[_curIndex] == '+'))
            {
                _curIndex ++;
            }

            // Check for Infinity (or -Infinity).
            if (More() && (_pathString[_curIndex] == 'I'))
            {
                //
                // Don't bother reading the characters, as the CLR parser will
                // do this for us later.
                //
                _curIndex = Math.Min(_curIndex+8, _pathLength); // "Infinity" has 8 characters
                simple = false;
            }
            // Check for NaN
            else if (More() && (_pathString[_curIndex] == 'N'))
            {
                //
                // Don't bother reading the characters, as the CLR parser will
                // do this for us later.
                //
                _curIndex = Math.Min(_curIndex+3, _pathLength); // "NaN" has 3 characters
                simple = false;
            }
            else
            {
                SkipDigits(! AllowSign);

                // Optional period, followed by more digits
                if (More() && (_pathString[_curIndex] == '.'))
                {
                    simple = false;
                    _curIndex ++;
                    SkipDigits(! AllowSign);
                }

                // Exponent
                if (More() && ((_pathString[_curIndex] == 'E') || (_pathString[_curIndex] == 'e')))
                {
                    simple = false;
                    _curIndex ++;
                    SkipDigits(AllowSign);
                }
            }

            if (simple && (_curIndex <= (start + 8))) // 32-bit integer
            {
                int sign = 1;
                
                if (_pathString[start] == '+')
                {
                    start ++;
                }
                else if (_pathString[start] == '-')
                {
                    start ++;
                    sign = -1;
                }                                        
                
                int value = 0;
                
                while (start < _curIndex)
                {
                    value = value * 10 + (_pathString[start] - '0');
                    start ++;
                }
                
                return value * sign;
            }
            else
            {
                ReadOnlySpan<char> slice = _pathString.AsSpan(start, _curIndex - start);

                try
                {
                    return double.Parse(slice, provider: _formatProvider);
                }
                catch (FormatException except)
                {
                    throw new System.FormatException(SR.Get(SRID.Parser_UnexpectedToken, _pathString, start), except);
                }
            }
        }
        
        /// <summary>
        /// Read a bool: 1 or 0
        /// </summary>
        /// <returns></returns>
        bool ReadBool()
        {
            SkipWhiteSpace(AllowComma);

            if (More())
            {
                _token = _pathString[_curIndex ++];

                if (_token == '0')
                {
                    return false;
                }
                else if (_token == '1')
                {
                    return true;
                }
            }

            ThrowBadToken();
            
            return false;
        }
        
        /// <summary>
        /// Read a relative point
        /// </summary>
        /// <returns></returns>
        private Point ReadPoint(char cmd, bool allowcomma)
        {
            double x = ReadNumber(allowcomma);
            double y = ReadNumber(AllowComma);

            if (cmd >= 'a') // 'A' < 'a'. lower case for relative
            {
                x += _lastPoint.X;
                y += _lastPoint.Y;
            }                

            return new Point(x, y);
        }
    
        /// <summary>
        /// Reflect _secondLastPoint over _lastPoint to get a new point for smooth curve
        /// </summary>
        /// <returns></returns>
        private Point Reflect()
        {
            return new Point(2 * _lastPoint.X - _secondLastPoint.X,
                             2 * _lastPoint.Y - _secondLastPoint.Y);
        }
        
        private void EnsureFigure()
        {
            if (!_figureStarted)
            {
                _context.BeginFigure(_lastStart, IsFilled, ! IsClosed);
                _figureStarted = true;
            }
        }
        
        /// <summary>
        /// Parse a PathFigureCollection string
        /// </summary>
        internal void ParseToGeometryContext(
            StreamGeometryContext context,
            string pathString,
            int startIndex)
        {
            // We really should throw an ArgumentNullException here for context and pathString.
            
            // From original code
            // This is only used in call to Double.Parse
            _formatProvider = System.Globalization.CultureInfo.InvariantCulture;
            
            _context         = context;
            _pathString      = pathString;
            _pathLength      = pathString.Length;
            _curIndex        = startIndex;
            
            _secondLastPoint = new Point(0, 0);
            _lastPoint       = new Point(0, 0);
            _lastStart       = new Point(0, 0);
            
            _figureStarted = false;
            
            bool  first = true;
            
            char last_cmd = ' ';

            while (ReadToken()) // Empty path is allowed in XAML
            {
                char cmd = _token;

                if (first)
                {
                    if ((cmd != 'M') && (cmd != 'm'))  // Path starts with M|m
                    {
                        ThrowBadToken();
                    }
            
                    first = false;
                }                    
                
                switch (cmd)
                {
                case 'm': case 'M':
                    // XAML allows multiple points after M/m
                    _lastPoint = ReadPoint(cmd, ! AllowComma);
                    
                    context.BeginFigure(_lastPoint, IsFilled, ! IsClosed);
                    _figureStarted = true;
                    _lastStart = _lastPoint;
                    last_cmd = 'M';
                    
                    while (IsNumber(AllowComma))
                    {
                        _lastPoint = ReadPoint(cmd, ! AllowComma);
                        
                        context.LineTo(_lastPoint, IsStroked, ! IsSmoothJoin);
                        last_cmd = 'L';
                    }
                    break;

                case 'l': case 'L':
                case 'h': case 'H':
                case 'v': case 'V':
                    EnsureFigure();

                    do
                    {
                        switch (cmd)
                        {
                        case 'l': _lastPoint    = ReadPoint(cmd, ! AllowComma); break;
                        case 'L': _lastPoint    = ReadPoint(cmd, ! AllowComma); break;
                        case 'h': _lastPoint.X += ReadNumber(! AllowComma); break;
                        case 'H': _lastPoint.X  = ReadNumber(! AllowComma); break; 
                        case 'v': _lastPoint.Y += ReadNumber(! AllowComma); break;
                        case 'V': _lastPoint.Y  = ReadNumber(! AllowComma); break;
                        }

                        context.LineTo(_lastPoint, IsStroked, ! IsSmoothJoin); 
                    }
                    while (IsNumber(AllowComma));

                    last_cmd = 'L';
                    break;

                case 'c': case 'C': // cubic Bezier
                case 's': case 'S': // smooth cublic Bezier
                    EnsureFigure();
                    
                    do
                    {
                        Point p;
                        
                        if ((cmd == 's') || (cmd == 'S'))
                        {
                            if (last_cmd == 'C')
                            {
                                p = Reflect();
                            }
                            else
                            {
                                p = _lastPoint;
                            }

                            _secondLastPoint = ReadPoint(cmd, ! AllowComma);
                        }
                        else
                        {
                            p = ReadPoint(cmd, ! AllowComma);

                            _secondLastPoint = ReadPoint(cmd, AllowComma);
                        }
                            
                        _lastPoint = ReadPoint(cmd, AllowComma);

                        context.BezierTo(p, _secondLastPoint, _lastPoint, IsStroked, ! IsSmoothJoin);
                        
                        last_cmd = 'C';
                    }
                    while (IsNumber(AllowComma));
                    
                    break;
                    
                case 'q': case 'Q': // quadratic Bezier
                case 't': case 'T': // smooth quadratic Bezier
                    EnsureFigure();
                    
                    do
                    {
                        if ((cmd == 't') || (cmd == 'T'))
                        {
                            if (last_cmd == 'Q')
                            {
                                _secondLastPoint = Reflect();
                            }
                            else
                            {
                                _secondLastPoint = _lastPoint;
                            }

                            _lastPoint = ReadPoint(cmd, ! AllowComma);
                        }
                        else
                        {
                            _secondLastPoint = ReadPoint(cmd, ! AllowComma);
                            _lastPoint = ReadPoint(cmd, AllowComma);
                        }

                        context.QuadraticBezierTo(_secondLastPoint, _lastPoint, IsStroked, ! IsSmoothJoin);
                        
                        last_cmd = 'Q';
                    }
                    while (IsNumber(AllowComma));
                    
                    break;
                    
                case 'a': case 'A':
                    EnsureFigure();
                    
                    do
                    {
                        // A 3,4 5, 0, 0, 6,7
                        double w        = ReadNumber(! AllowComma);
                        double h        = ReadNumber(AllowComma);
                        double rotation = ReadNumber(AllowComma);
                        bool large      = ReadBool();
                        bool sweep      = ReadBool();
                        
                        _lastPoint = ReadPoint(cmd, AllowComma);

                        context.ArcTo(
                            _lastPoint,
                            new Size(w, h),
                            rotation,
                            large,
#if PBTCOMPILER
                            sweep,
#else                            
                            sweep ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
#endif                             
                            IsStroked,
                            ! IsSmoothJoin
                            );
                    }
                    while (IsNumber(AllowComma));
                    
                    last_cmd = 'A';
                    break;
                    
                case 'z':
                case 'Z':
                    EnsureFigure();
                    context.SetClosedState(IsClosed);
                    
                    _figureStarted = false;
                    last_cmd = 'Z';
                    
                    _lastPoint = _lastStart; // Set reference point to be first point of current figure
                    break;
                    
                default:
                    ThrowBadToken();
                    break;
                }
            }
        }
    }
}    
