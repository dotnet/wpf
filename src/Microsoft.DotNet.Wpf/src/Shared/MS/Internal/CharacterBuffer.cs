// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//

// 
//
// Description: Definition of readonly character memory buffer
// 
//
//  
//
//
//
//---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;

namespace MS.Internal
{
    /// <summary>
    /// Abstraction of a readonly character buffer
    /// </summary>
    internal abstract class CharacterBuffer : IList<char>
    {
        /// <summary>
        /// Get fixed address of the character buffer
        /// </summary>
        public abstract unsafe char* GetCharacterPointer();

        /// <summary>
        /// Get fixed address of the character buffer and Pin if necessary.
        /// Note: This call should only be used when we know that we are not pinning 
        /// memory for a long time so as not to fragment the heap.
        /// </summary>
        public abstract IntPtr PinAndGetCharacterPointer(int offset, out GCHandle gcHandle);

        /// <summary>
        /// This is a matching call for PinAndGetCharacterPointer to unpin the memory.
        /// </summary>
        public abstract void UnpinCharacterPointer(GCHandle gcHandle);


        /// <summary>
        /// Add character buffer's content to a StringBuilder
        /// </summary>
        /// <param name="stringBuilder">string builder to add content to</param>
        /// <param name="characterOffset">character offset to first character to append</param>
        /// <param name="length">number of character appending</param>
        /// <returns>string builder</returns>
        public abstract void AppendToStringBuilder(
            StringBuilder   stringBuilder,
            int             characterOffset,
            int             length
            );

        #region IList<char> Members

        public int IndexOf(char item)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (item == this[i])
                    return i;
            }
            return -1;
        }

        public void Insert(int index, char item)
        {
            // CharacterBuffer is read only.
            throw new NotSupportedException();
        }

        public abstract char this[int index]
        {
            get;
            set;
        }

        public void RemoveAt(int index)
        {
            // CharacterBuffer is read only.
            throw new NotSupportedException();
        }

        #endregion

        #region ICollection<char> Members

        public void Add(char item)
        {
            // CharacterBuffer is read only.
            throw new NotSupportedException();
        }

        public void Clear()
        {
            // CharacterBuffer is read only.
            throw new NotSupportedException();
        }

        public bool Contains(char item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(char[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; ++i)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public abstract int Count
        {
            get;
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(char item)
        {
            // CharacterBuffer is read only.
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<char> Members

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<char>)this).GetEnumerator();
        }

        #endregion
    }



    /// <summary>
    /// Character memory buffer implemented by managed character array
    /// </summary>
    internal sealed class CharArrayCharacterBuffer : CharacterBuffer
    {
        private char[]      _characterArray;


        /// <summary>
        /// Creating a character memory buffer from character array
        /// </summary>
        /// <param name="characterArray">character array</param>
        public CharArrayCharacterBuffer(
            char[]  characterArray
            )
        {
            if (characterArray == null) 
            {
                throw new ArgumentNullException("characterArray");
            }
            
            _characterArray = characterArray;
        }


        /// <summary>
        /// Read a character from buffer at the specified character index
        /// </summary>
        public override char this[int characterOffset]
        {
            get { return _characterArray[characterOffset]; }
            set { throw new NotSupportedException(); }
        }


        /// <summary>
        /// Buffer character length
        /// </summary>
        public override int Count
        {
            get { return _characterArray.Length; }
        }


        /// <summary>
        /// Get fixed address of the character buffer
        /// </summary>
        public override unsafe char* GetCharacterPointer()
        {
            // Even though we could allocate GCHandle for this purpose, we would need
            // to manage how to release them appropriately. It is even worse if we
            // consider performance implication of doing so. In typical UI scenario,
            // there are so many string objects running around in GC heap. Getting 
            // GCHandle for every one of them is very expensive and demote GC's ability
            // to compact its heap. 
            return null;
        }

        /// <summary>
        /// Get fixed address of the character buffer and Pin if necessary.
        /// Note: This call should only be used when we know that we are not pinning 
        /// memory for a long time so as not to fragment the heap.
        public unsafe override IntPtr PinAndGetCharacterPointer(int offset, out GCHandle gcHandle)
        {
            gcHandle = GCHandle.Alloc(_characterArray, GCHandleType.Pinned);
            return new IntPtr(((char*)gcHandle.AddrOfPinnedObject().ToPointer()) + offset);
        }
        
        /// <summary>
        /// This is a matching call for PinAndGetCharacterPointer to unpin the memory.
        /// </summary>
        public override void UnpinCharacterPointer(GCHandle gcHandle)
        {
            gcHandle.Free();
        }

        /// <summary>
        /// Add character buffer's content to a StringBuilder
        /// </summary>
        /// <param name="stringBuilder">string builder to add content to</param>
        /// <param name="characterOffset">index to first character in the buffer to append</param>
        /// <param name="characterLength">number of character appending</param>
        public override void AppendToStringBuilder(
            StringBuilder   stringBuilder,
            int             characterOffset,
            int             characterLength
            )
        {
            Debug.Assert(characterOffset >= 0 && characterOffset < _characterArray.Length, "Invalid character index");

            if (    characterLength < 0 
                ||  characterOffset + characterLength > _characterArray.Length)
            {
                characterLength = _characterArray.Length - characterOffset;
            }

            stringBuilder.Append(_characterArray, characterOffset, characterLength);
        }
    }



    /// <summary>
    /// Character buffer implemented by string
    /// </summary>
    internal sealed class StringCharacterBuffer : CharacterBuffer
    {
        private string      _string;


        /// <summary>
        /// Creating a character buffer from string
        /// </summary>
        /// <param name="characterString">character string</param>
        public StringCharacterBuffer(
            string  characterString
            )
        {
            if (characterString == null)
            {
                throw new ArgumentNullException("characterString");
            }
            
            _string = characterString;
        }


        /// <summary>
        /// Read a character from buffer at the specified character index
        /// </summary>
        public override char this[int characterOffset]
        {
            get { return _string[characterOffset]; }
            set { throw new NotSupportedException(); }
        }


        /// <summary>
        /// Buffer character length
        /// </summary>
        public override int Count
        {
            get { return _string.Length; }
        }


        /// <summary>
        /// Get fixed address of the character buffer
        /// </summary>
        public override unsafe char* GetCharacterPointer()
        {
            // Even though we could allocate GCHandle for this purpose, we would need
            // to manage how to release them appropriately. It is even worse if we
            // consider performance implication of doing so. In typical UI scenario,
            // there are so many string objects running around in GC heap. Getting 
            // GCHandle for every one of them is very expensive and demote GC's ability
            // to compact its heap. 
            return null;
        }

        /// <summary>
        /// Get fixed address of the character buffer and Pin if necessary.
        /// Note: This call should only be used when we know that we are not pinning 
        /// memory for a long time so as not to fragment the heap.
        public unsafe override IntPtr PinAndGetCharacterPointer(int offset, out GCHandle gcHandle)
        {
            gcHandle = GCHandle.Alloc(_string, GCHandleType.Pinned);
            return new IntPtr(((char*)gcHandle.AddrOfPinnedObject().ToPointer()) + offset);
        }

        /// <summary>
        /// This is a matching call for PinAndGetCharacterPointer to unpin the memory.
        /// </summary>
        public override void UnpinCharacterPointer(GCHandle gcHandle)
        {
            gcHandle.Free();
        }


        /// <summary>
        /// Add character buffer's content to a StringBuilder
        /// </summary>
        /// <param name="stringBuilder">string builder to add content to</param>
        /// <param name="characterOffset">index to first character in the buffer to append</param>
        /// <param name="characterLength">number of character appending</param>
        public override void AppendToStringBuilder(
            StringBuilder   stringBuilder,
            int             characterOffset,
            int             characterLength
            )
        {
            Debug.Assert(characterOffset >= 0 && characterOffset < _string.Length, "Invalid character index");

            if (    characterLength < 0 
                ||  characterOffset + characterLength > _string.Length)
            {
                characterLength = _string.Length - characterOffset;
            }

            stringBuilder.Append(_string, characterOffset, characterLength);
        }
    }



    /// <summary>
    /// Character buffer implemented as unsafe pointer to character string
    /// </summary>
    internal sealed unsafe class UnsafeStringCharacterBuffer : CharacterBuffer
    {
        private char*   _unsafeString;

        private int     _length;


        /// <summary>
        /// Creating a character buffer from an unsafe pointer to character string
        /// </summary>
        /// <param name="characterString">unsafe pointer to character string</param>
        /// <param name="length">number of valid characters referenced by the unsafe pointer</param>
        public UnsafeStringCharacterBuffer(
            char*   characterString,
            int     length
            )
        {
            if (characterString == null)
            {
                throw new ArgumentNullException("characterString");
            }

            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length", SR.ParameterValueMustBeGreaterThanZero);
            }
            
            _unsafeString = characterString;
            _length = length;
        }


        /// <summary>
        /// Read a character from buffer at the specified character index
        /// </summary>
        public override char this[int characterOffset]
        {
            get {
                if (characterOffset >= _length || characterOffset < 0)
                    throw new ArgumentOutOfRangeException("characterOffset", SR.Format(SR.ParameterMustBeBetween,0,_length));
                return _unsafeString[characterOffset];
            }
            set { throw new NotSupportedException(); }
        }


        /// <summary>
        /// Buffer character length
        /// </summary>
        public override int Count
        {
            get { return _length; }
        }


        /// <summary>
        /// Get fixed address of the character buffer
        /// </summary>
        public override unsafe char* GetCharacterPointer()
        {
            return _unsafeString;
        }

        /// <summary>
        /// Get fixed address of the character buffer and Pin if necessary.
        /// Note: This call should only be used when we know that we are not pinning 
        /// memory for a long time so as not to fragment the heap.
        public override IntPtr PinAndGetCharacterPointer(int offset, out GCHandle gcHandle)
        {
            gcHandle = new GCHandle();
            return new IntPtr(_unsafeString + offset);
        }

        /// <summary>
        /// This is a matching call for PinAndGetCharacterPointer to unpin the memory.
        /// </summary>
        public override void UnpinCharacterPointer(GCHandle gcHandle)
        {
        }


        /// <summary>
        /// Add character buffer's content to a StringBuilder
        /// </summary>
        /// <param name="stringBuilder">string builder to add content to</param>
        /// <param name="characterOffset">index to first character in the buffer to append</param>
        /// <param name="characterLength">number of character appending</param>
        public override  void AppendToStringBuilder(
            StringBuilder   stringBuilder,
            int             characterOffset,
            int             characterLength
            )
        {

            if (characterOffset >= _length || characterOffset < 0)
            {
                throw new ArgumentOutOfRangeException("characterOffset", SR.Format(SR.ParameterMustBeBetween,0,_length));
            }

            if (characterLength < 0 || characterOffset + characterLength > _length)
            {
                throw new ArgumentOutOfRangeException("characterLength", SR.Format(SR.ParameterMustBeBetween,0, _length - characterOffset));
            }

            stringBuilder.Append(new string(_unsafeString, characterOffset, characterLength));
        }
    }
}

