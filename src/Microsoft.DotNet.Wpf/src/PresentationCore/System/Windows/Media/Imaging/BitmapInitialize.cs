// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//


using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32;
using System.Security;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using SR=MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region BitmapInitialize
    /// <summary>
    /// Utility class providing support for ISupportInitialize
    /// </summary>
    internal class BitmapInitialize : ISupportInitialize
    {
        public BitmapInitialize()
        {
        }

        public void BeginInit()
        {
            if (IsInitAtLeastOnce)
                throw new InvalidOperationException(SR.Format(SR.Image_OnlyOneInit, null));

            if (IsInInit)
                throw new InvalidOperationException(SR.Format(SR.Image_InInitialize, null));

            _inInit = true;
        }

        public void EndInit()
        {
            if (!IsInInit)
                throw new InvalidOperationException(SR.Format(SR.Image_EndInitWithoutBeginInit, null));

            _inInit = false;
            _isInitialized = true;
        }

        public void SetPrologue()
        {
            if (!IsInInit)
            {
                throw new InvalidOperationException(SR.Format(SR.Image_SetPropertyOutsideBeginEndInit, null));
            }
        }

        public bool IsInInit
        {
            get
            {
                return _inInit;
            }
        }

        public bool IsInitAtLeastOnce
        {
            get
            {
                return _isInitialized;
            }
        }

        public void EnsureInitializedComplete()
        {
            if (IsInInit)
                throw new InvalidOperationException(SR.Format(SR.Image_InitializationIncomplete, null));

            if (!IsInitAtLeastOnce)
                throw new InvalidOperationException(SR.Format(SR.Image_NotInitialized, null));
        }

        public void Reset()
        {
            _inInit = false;
            _isInitialized = false;
        }

        private bool _inInit = false;
        private bool _isInitialized = false;
    }

    #endregion
}
