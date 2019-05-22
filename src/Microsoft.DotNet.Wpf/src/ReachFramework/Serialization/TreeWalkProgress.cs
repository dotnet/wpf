// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file implements the TreeWalkProgress
        used by the Xps Serialization APIs for tracking cycles in a visual tree.
--*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class  is used by the Xps Serialization APIs for tracking cycles in a visual tree.
    /// </summary>
    internal class TreeWalkProgress
    {        
        public bool EnterTreeWalk(ICyclicBrush brush)
        {
            if(this._cyclicBrushes.ContainsKey(brush))
            {
                return false;
            }
            
            this._cyclicBrushes.Add(brush, EmptyStruct.Default);
            return true;
        }
            
        public void ExitTreeWalk(ICyclicBrush brush)
        {
            this._cyclicBrushes.Remove(brush);
        }
            
        public bool IsTreeWalkInProgress(ICyclicBrush brush)
        {
            return this._cyclicBrushes.ContainsKey(brush);
        }
        
        // We use the keys of this dictionary to simulate a set
        // We do not use HashSet<K,V> to avoid a perf regression by loading System.Core.dll
        // It also makes the fix easier to backport to pre .net 3.5 releases
        private IDictionary<ICyclicBrush, EmptyStruct> _cyclicBrushes = new Dictionary<ICyclicBrush, EmptyStruct>();
        
        // A struct that when optimized does not consume per instance heap\stack space 
        private struct EmptyStruct
        {
            public static EmptyStruct Default = new EmptyStruct();
        }
    }
}
