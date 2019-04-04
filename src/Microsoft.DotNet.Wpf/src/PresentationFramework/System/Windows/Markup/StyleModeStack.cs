// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    internal enum StyleMode : byte
    {
        Base,                    // Style/Template tag, simple and top level complex properties
        TargetTypeProperty,      // Target type complex property under a Style/Template
        BasedOnProperty,         // BasedOn complex property under a Style/Template
        DataTypeProperty,        // Data type complex property under a Template
        ComplexProperty,         // Reading an allowed complex property under a Template
        Resources,               // Resources complex property under a Style
        Setters,                 // Style.Setters IList complex property and subtree
        Key,                     // x:Key subtree when Style is used in a dictionary
        TriggerBase,             // Style.TriggerBase complex property and subtree
        TriggerActions,          // When in the middle of parsing EventTrigger.TriggerActions
        TriggerSetters,          // When in the middle of parsing property trigger Setters collection
        TriggerEnterExitActions, // Trigger.EnterActions or Trigger.ExitActions
        VisualTree,              // FrameworkTemplate.VisualTree's subtree
    }

    internal class StyleModeStack
    {
        internal StyleModeStack()
        {
            Push(StyleMode.Base);
        }
        
        internal int Depth
        {
            get { return _stack.Count - 1; }
        }
        
        internal StyleMode Mode
        {
            get
            {
                Debug.Assert(Depth >= 0, "StyleModeStack's depth was " + Depth + ", should be >= 0");
                return _stack.Peek();
            }
        }

        internal void Push (StyleMode mode)
        {
            _stack.Push(mode);
        }

        internal void Push ()
        {
            Push(Mode);
        }
        
        internal StyleMode Pop()
        {
            Debug.Assert(Depth >= 0, "StyleMode::Pop() with depth of " + Depth + ", should be >= 0");
            return _stack.Pop();
        }
        
        private Stack<StyleMode> _stack = new Stack<StyleMode>(64);
    }
}
