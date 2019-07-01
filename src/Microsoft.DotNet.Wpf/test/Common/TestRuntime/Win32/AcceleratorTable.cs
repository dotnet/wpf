// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Runtime.InteropServices;


namespace Microsoft.Test.Win32
{

    ///<summary>
    /// This is the implementation for creating a AcceleratorTable for Win32
    ///</summary>
    public class AccelaratorTable : IDisposable
    {

        ///<summary>
        /// Passing an array with all the ACCEL that will be on the Table
        ///</summary>
        public AccelaratorTable(NativeStructs.ACCEL[] accelArray)
        {
            if (accelArray.Length <= 0)
                throw new ArgumentException("Empty Array");

            _accelArray = accelArray;
        }


        ///<summary>
        /// Pointer to the accel Table
        ///</summary>
        public IntPtr ACCELTable 
        {
            get
            {
                return _accelTable;
            }
        }

        ///<summary>
        /// Destroy the Accelerator Table
        ///</summary>
        public void Dispose()
        {
            Dispose(true);
        }


        ///<summary>
        /// Destructor
        ///</summary>
        ~AccelaratorTable()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
                
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            if (_accelTable != IntPtr.Zero)
            {
                NativeMethods.DestroyAcceleratorTable(new HandleRef(null, _accelTable));
                _accelTable = IntPtr.Zero;
            }

        }


        ///<summary>
        /// Create the Accelarator Table
        ///</summary>
        public void Create()
        {
            // We don't create another table
            if (_accelTable != IntPtr.Zero)
                return;
           
            int accelCount = _accelArray.Length;

            int accelSize = Marshal.SizeOf(typeof(NativeStructs.ACCEL));

            _accelPtrMemory = Marshal.AllocHGlobal(accelSize * accelCount);

            try
            {

                for (int i=0; i< _accelArray.Length; i++)
                {
                    IntPtr address = (IntPtr)((long)_accelPtrMemory + accelSize *i);

                    Marshal.StructureToPtr(_accelArray[i],address,false);
                }

                _accelTable = NativeMethods.CreateAcceleratorTable(new HandleRef(null, _accelPtrMemory), _accelArray.Length);

            }
            finally
            {

                if (_accelPtrMemory != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_accelPtrMemory);
                    _accelPtrMemory = IntPtr.Zero;
                }
            }


            

        }

        IntPtr _accelTable = IntPtr.Zero;
        IntPtr _accelPtrMemory = IntPtr.Zero;
        NativeStructs.ACCEL[] _accelArray;
    }
    
}
