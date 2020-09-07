// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: defining types with Generics in WindowsBase.dll that are only 
//      instantiated in PresentationCore/-Framework assemblies need a dummy
//      instantiation in this assembly to trigger proper ngen when WB.dll
//      is installed.
//      This prevents JIT-ing when WB.dll is loaded.
//

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MS.Internal
{
    internal static class GenericsInstances
    {
        // ObservableCollection / ReadOnlyObservableCollection<T>
        private static ObservableCollection<object> s_OC_Empty = new ObservableCollection<object>();
        private static ReadOnlyObservableCollection<object> s_ROOC_Empty
            = new ReadOnlyObservableCollection<object>(new ObservableCollection<object>());

        // ICollectionView.Filter:
        private static bool PredicateMethod(object item) { return false; }
        private static Predicate<object> s_PM_Empty = new Predicate<object>(PredicateMethod);
    }
}

