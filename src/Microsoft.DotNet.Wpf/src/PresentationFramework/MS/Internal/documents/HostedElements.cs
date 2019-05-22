// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Enumerator class for returning descendants of TextBlock and FlowDocumentPage
//

using System.Collections;
using MS.Internal.Documents;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Diagnostics;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Documents
{
    /// <summary>
    ///  Enumerator class for implementation of IContentHost on TextBlock and FlowDocumentPage.
    ///  Used to iterate through descendants of the content host
    /// </summary>
    internal class HostedElements : IEnumerator<IInputElement>
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        internal HostedElements(ReadOnlyCollection<TextSegment> textSegments)
        {
            _textSegments = textSegments;
            _currentPosition = null;
            _currentTextSegment = 0;
        }

        #endregion Constructors


        //-------------------------------------------------------------------
        //
        // IDisposable Methods
        //
        //-------------------------------------------------------------------

        #region IDisposable Methods

        void IDisposable.Dispose()
        {
            _textSegments = null;
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Methods

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        public bool MoveNext()
        {
            // If _textSegments has been disposed, throw exception
            if (_textSegments == null)
            {
                throw new ObjectDisposedException("HostedElements");
            }

            if (_textSegments.Count == 0)
                return false;

            // Verify that current position matches with current text segment, and set it if it's null.
            if (_currentPosition == null)
            {
                // We should be at the start
                Debug.Assert(_currentTextSegment == 0);
                // Type check that we can create _currentPosition here
                if (_textSegments[0].Start is TextPointer)
                {
                    _currentPosition = new TextPointer(_textSegments[0].Start as TextPointer);
                }
                else
                {
                    // We cannot search, return false
                    _currentPosition = null;
                    return false;
                }
            }
            else if (_currentTextSegment < _textSegments.Count)
            {
                // We have not yet reached the end of the container. Assert that our position matches with
                // our current text segment
                Debug.Assert(((ITextPointer)_currentPosition).CompareTo(_textSegments[_currentTextSegment].Start) >= 0 &&
                             ((ITextPointer)_currentPosition).CompareTo(_textSegments[_currentTextSegment].End) < 0);

                // This means that we are in the middle of content, so the previous search would have returned 
                // something. We want to now move beyond that position
                _currentPosition.MoveToNextContextPosition(LogicalDirection.Forward);
            }

            // Search strarting from the position in the current segment
            while (_currentTextSegment < _textSegments.Count)
            {
                Debug.Assert(((ITextPointer)_currentPosition).CompareTo(_textSegments[_currentTextSegment].Start) >= 0);
                while (((ITextPointer)_currentPosition).CompareTo(_textSegments[_currentTextSegment].End) < 0)
                {
                    if (_currentPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart ||
                        _currentPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.EmbeddedElement)
                    {
                        return true;
                    }
                    else
                    {
                        _currentPosition.MoveToNextContextPosition(LogicalDirection.Forward);
                    }
                }

                // We have reached the end of a segement. Update counters
                _currentTextSegment++;
                if (_currentTextSegment < _textSegments.Count)
                {
                    // Move to the next segment
                    if (_textSegments[_currentTextSegment].Start is TextPointer)
                    {
                        _currentPosition = new TextPointer(_textSegments[_currentTextSegment].Start as TextPointer);
                    }
                    else
                    {
                        // This state is invalid, set to null and return false
                        _currentPosition = null;
                        return false;
                    }
                }
            }
            // We have reached the end without finding an element. Return false.
            return false;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        // Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        object IEnumerator.Current
        {
            get { return this.Current; }
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }

        public IInputElement Current
        {
            get
            {
                // Disable PRESharp warning 6503: "Property get methods should not throw exceptions".
                // HostedElements must throw exception if Current property is incorrectly accessed
                if (_textSegments == null)
                {
                    // Collection was modified 
#pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorCollectionDisposed));
                }

                if (_currentPosition == null)
                {
                    // Enumerator not started. Call MoveNext to see if we can move ahead
#pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorNotStarted));
                }

                IInputElement currentElement = null;
                switch (_currentPosition.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.ElementStart:
                        Debug.Assert(_currentPosition.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward) is IInputElement);
                        currentElement = _currentPosition.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);
                        break;
                    case TextPointerContext.EmbeddedElement:
                        Debug.Assert(_currentPosition.GetAdjacentElement(LogicalDirection.Forward) is IInputElement);
                        currentElement = (IInputElement)_currentPosition.GetAdjacentElement(LogicalDirection.Forward);
                        break;
                    default:
                        // Throw exception because this function should only be called after MoveNext, and not 
                        // if MoveNext returns false
                        Debug.Assert(false, "Invalid state in HostedElements.cs");
                        break;
                }
                return currentElement;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        ReadOnlyCollection<TextSegment> _textSegments;
        TextPointer _currentPosition;
        int _currentTextSegment;

        #endregion Private Fields
    }
}
