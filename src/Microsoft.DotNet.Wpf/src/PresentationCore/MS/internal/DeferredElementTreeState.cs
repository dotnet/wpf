// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MS.Internal
{
    internal class DeferredElementTreeState
    {
        public void SetCoreParent(DependencyObject element, DependencyObject parent)
        {
            if(!_oldCoreParents.ContainsKey(element))
            {
                _oldCoreParents[element] = parent;
            }
        }

        public static DependencyObject GetCoreParent(DependencyObject element, DeferredElementTreeState treeState)
        {
            DependencyObject parent = null;
            
            if(treeState != null && treeState._oldCoreParents.ContainsKey(element))
            {
                parent = treeState._oldCoreParents[element];
            }
            else
            {
                Visual v = element as Visual;
                if(v != null)
                {
                    parent = VisualTreeHelper.GetParent(v);
                }
                else
                {
                    ContentElement ce = element as ContentElement;
                    if(ce != null)
                    {
                        parent = ContentOperations.GetParent(ce);
                    }
                    else
                    {
                        Visual3D v3D = element as Visual3D;
                        if (v3D != null)
                        {
                            parent = VisualTreeHelper.GetParent(v3D);
                        }
                    }
}
            }

            return parent;
        }

        public static DependencyObject GetInputElementParent(DependencyObject element, DeferredElementTreeState treeState)
        {
            DependencyObject parent = element;
            
            while (true)
            {
                parent = GetCoreParent(parent, treeState);

                if (parent == null || InputElement.IsValid(parent))
                {
                    break;
                }
            }

            return parent;
        }

        public void SetLogicalParent(DependencyObject element, DependencyObject parent)
        {
            if(!_oldLogicalParents.ContainsKey(element))
            {
                _oldLogicalParents[element] = parent;
            }
        }

        public static DependencyObject GetLogicalParent(DependencyObject element, DeferredElementTreeState treeState)
        {
            DependencyObject parent = null;
            
            if(treeState != null && treeState._oldLogicalParents.ContainsKey(element))
            {
                parent = treeState._oldLogicalParents[element];
            }
            else
            {
                UIElement e = element as UIElement;
                if(e != null)
                {
                    parent = e.GetUIParentCore();  // Overriden by FrameworkElement.
                }

                ContentElement ce = element as ContentElement;
                if(ce != null)
                {
                    parent = ce.GetUIParentCore(); // Overriden by FrameworkContentElement.
                }
            }

            return parent;
        }

        public void Clear()
        {
            _oldCoreParents.Clear();
            _oldLogicalParents.Clear();
        }

        public bool IsEmpty
        {
            get
            {
                return _oldCoreParents.Count == 0 && _oldLogicalParents.Count == 0;
            }
        }
        
        private Dictionary<DependencyObject, DependencyObject> _oldCoreParents = new Dictionary<DependencyObject, DependencyObject>();
        private Dictionary<DependencyObject, DependencyObject> _oldLogicalParents = new Dictionary<DependencyObject, DependencyObject>();
    }
}

