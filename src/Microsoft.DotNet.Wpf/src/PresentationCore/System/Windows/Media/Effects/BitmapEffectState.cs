// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of BitmapEffectState.
//              This is the base class for holding the bitmap effect state.
//
//

using System;
using System.Windows.Threading;

using MS.Win32;
using System.Security;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using MS.Internal;
using System.Resources;
using MS.Utility;
using System.Runtime.InteropServices;
using MS.Internal.PresentationCore;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Effects
{    
    /// <summary>
    /// This class is used to store the user provided bitmap effect data on Visual. 
    /// It is necessary to implement the emulation layer for some legacy effects on top
    /// of the new pipline. 
    /// </summary>
    internal class BitmapEffectState
    {    
        private BitmapEffect _bitmapEffect;
        private BitmapEffectInput _bitmapEffectInput;

        public BitmapEffectState() {}

        public BitmapEffect BitmapEffect
        {
            get { return _bitmapEffect; }
            set { _bitmapEffect = value; }
        }

        public BitmapEffectInput BitmapEffectInput
        {
            get { return _bitmapEffectInput; }
            set { _bitmapEffectInput = value; }
        }
    }
}
