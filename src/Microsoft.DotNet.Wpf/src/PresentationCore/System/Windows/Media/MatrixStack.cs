// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics;
using System.Collections;
using MS.Internal;
using System.Security;

namespace System.Windows.Media
{
    /// <summary> MatrixStack class implementation</summary>
    internal class MatrixStack
    {
        private Matrix[] _items;
        private int _size; 
        
#if DEBUG
        private static readonly int s_initialSize = 4;
#else
        private static readonly int s_initialSize = 40; // sizeof(Matrix) * 40 =  2240 bytes. Must be > 4
#endif
        private static readonly int s_growFactor = 2; 
        private static readonly int s_shrinkFactor = s_growFactor + 1;

        // The following members are used to lazily manage the memory allocated by the stack.
        private int _highWaterMark;
        private int _observeCount;
        private static readonly int s_trimCount = 10;

        public MatrixStack()
        {
            _items = new Matrix[s_initialSize];
        }

        /// <summary>
        /// Ensures that there is space for at least one more element.
        /// </summary>
        private void EnsureCapacity()
        {
            // If we reached the capacity of the _items array we need to increase
            // the size. We use an exponential growth to keep Push in average O(1).
            if (_size == _items.Length)
            {
                Matrix[] newItems = new Matrix[s_growFactor * _size];
                Array.Copy(_items, newItems, _size);
                _items = newItems;
            }
        }
        
        /// <summary>
        /// Push new matrix on the stack.
        /// </summary>
        /// <param name="matrix">The new matrix to be pushed</param>
        /// <param name="combine">Iff true, the matrix is multiplied with the current
        /// top matrix on the stack and then pushed on the stack. If false the matrix
        /// is pushed as is on top of the stack.</param>
        public void Push(ref Matrix matrix, bool combine)
        {
            EnsureCapacity();

            if (combine && (_size > 0))
            {
                // Combine means that we push the product of the top matrix and the new
                // one on the stack. Note that there isn't a top-matrix if the stack is empty.
                // In this case the top-matrix is assumed to be identity. See else case.
                _items[_size] = matrix;

                // _items[_size] = _items[_size] * _items[_size-1];
                MatrixUtil.MultiplyMatrix(ref _items[_size], ref _items[_size - 1]);
            }
            else
            {
                // If the stack is empty or we are not combining, we just assign the matrix passed
                // in to the next free slot in the array.
                _items[_size] = matrix;
            }

            _size++;
            
            // For memory optimization purposes we track the max usage of the stack here.
            // See the optimze method for more details about this.
            _highWaterMark = Math.Max(_highWaterMark, _size);
        }

        /// <summary>
        /// Pushes a transformation on the stack. The transformation is multiplied with the top element
        /// on the stack and then pushed on the stack.
        /// </summary>
        /// <param name="transform">2D transformation.</param>
        /// <param name="combine">The combine argument indicates if the transform should be pushed multiplied with the
        /// current top transform or as is.</param>
        /// <remark>
        /// The code duplication between Push(ref Matrix, bool) and Push(Transform, bool) is not ideal. 
        /// However, we did see a performance gain by optimizing the Push(Transform, bool) method 
        /// minimizing the copies of data.
        /// </remark>
        public void Push(Transform transform, bool combine)
        {
            EnsureCapacity();
            
            if (combine && (_size > 0))
            {
                // Combine means that we push the product of the top matrix and transform
                // on the stack. Note that there isn't a top-matrix if the stack is empty.
                // In this case the top-matrix is assumed to be identity. See else case.                
                // _items[_size] is the new top of the stack, _items[_size - 1] is the
                // previous top.
                transform.MultiplyValueByMatrix(ref _items[_size], ref _items[_size - 1]);
            }
            else
            {
                // If we don't combine or if the stack is empty.
                _items[_size] = transform.Value;
            }

            _size++;

            // For memory optimization purposes we track the max usage of the stack here.
            // See the optimze method for more details about this.
            _highWaterMark = Math.Max(_highWaterMark, _size);            
        }

        /// <summary>
        /// Pushes the offset on the stack. If the combine flag is true, the offset is applied to the top element before
        /// it is pushed onto the stack.
        /// </summary>
        public void Push(Vector offset, bool combine)
        {
            EnsureCapacity();

            if (combine && (_size > 0))
            {
                // In combine mode copy the top element, but only if the stack is not empty.
                _items[_size] = _items[_size-1];
            }
            else
            {
                // If combine is false, initialize the new top stack element with the identity
                // matrix.
                _items[_size] = Matrix.Identity;
            }

            // Apply the offset to the new top element.
            MatrixUtil.PrependOffset(ref _items[_size], offset.X, offset.Y);

            _size++;

            // For memory optimization purposes we track the max usage of the stack here.
            // See the optimze method for more details about this.
            _highWaterMark = Math.Max(_highWaterMark, _size);
        }
        

        /// <summary>
        /// Pops the top stack element from the stack.
        /// </summary>
        public void Pop()
        {
            Debug.Assert(!IsEmpty);
#if DEBUG
            _items[_size-1] = new Matrix();
#endif
            _size--;
        }

        /// <summary>
        /// Peek returns the matrix on the top of the stack.
        /// </summary>
        public Matrix Peek()
        {
            Debug.Assert(!IsEmpty);
            return _items[_size-1];
        }   

        ///<value>
        /// This is true iff the stack is empty.  
        ///</value>
        public bool IsEmpty { get { return _size == 0; } }

        /// <summary>
        /// Instead of allocating and releasing memory continuously while pushing on 
        /// and popping off the stack, we call the optimize method after each frame 
        /// to readjust the internal stack capacity. 
        /// </summary>
        public void Optimize()
        {
            Debug.Assert(_size == 0); // The stack must be empty before this is called.
            Debug.Assert(_highWaterMark <= _items.Length);
            
            // After s_trimCount calls to this method we check the past usage of the stack.
            if (_observeCount == s_trimCount)
            {
                int newSize = Math.Max(_highWaterMark, s_initialSize);
                if (newSize * (s_shrinkFactor) <= _items.Length)
                {
                    // If the water mark is less or equal to capacity divided by the shrink 
                    // factor, then we shrink the stack. Usually the shrink factor is greater 
                    // than the grow factor. This avoids an oscillation of shrinking and growing 
                    // the stack if the high water mark goes only slightly up and down.

                    // Note that we don't need to copy the array because the stack is empty.            
                    _items = new Matrix[newSize];
                }

                _highWaterMark = 0;
                _observeCount = 0;
            }
            else
            {
                // Keep incrementing our observe count
                _observeCount++;
            }
        }
}
}


