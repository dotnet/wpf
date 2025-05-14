// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: This is a super simple Matrix3DStack implementation.
//              MatrixStack (2D) is optimized to avoid boxig and copying
//              of structs.  This was written as a stop-gap to address
//              a 

namespace System.Windows.Media.Media3D
{
    // Use Generics to port MatrixStack to Matrix3D
    //
    //  MatrixStack (2D) is much more advanced.  This was written as a stop-gap to address
    //  a bug until we can use CodeGen here.
    //
    internal class Matrix3DStack
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        public void Clear()
        {
            _stack.Clear();
        }

        public Matrix3D Pop()
        {
            Matrix3D top = Top;
            _stack.RemoveAt(_stack.Count - 1);
            return top;
        }

        /// <summary>
        /// Empty => [matrix]
        /// tail | [top] => tail | [top] | [matrix * top]
        /// </summary>
        public void Push(Matrix3D matrix)
        {
            if (_stack.Count > 0)
            {
                matrix.Append(Top);
            }
            
            _stack.Add(matrix);
        }
        
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        public int Count
        {
            get
            {
                return _stack.Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (_stack.Count == 0);
            }
        }

        public Matrix3D Top
        {
            get
            {
                return _stack[_stack.Count - 1];
            }
        }
        
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        
        #region Private Fields
        
        private readonly List<Matrix3D> _stack = new List<Matrix3D>();

        #endregion Private Fields
    }
}

