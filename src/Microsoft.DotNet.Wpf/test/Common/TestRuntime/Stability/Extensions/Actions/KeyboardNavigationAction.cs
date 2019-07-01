// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40    
    
    [TargetTypeAttribute(typeof(UIElement))]
    public class KeyboardTabAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        protected Int32 numberOfHits { get; set; }        

        public override void  Perform()
        {
            for (int i = 0; i < numberOfHits; i++)
            {                                
                HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Tab);
            }
        }
    }

    [TargetTypeAttribute(typeof(UIElement))]
    public class KeyboardTabSpaceAction : SimpleDiscoverableAction
    {                   
        public override void Perform()
        {          
            HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Space);
        }
    }

    [TargetTypeAttribute(typeof(UIElement))]
    public class KeyboardRandomPressAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        protected Int32 numberOfHits { get; set; }        

        protected Key randomKey { get; set; }
        
        public override void Perform()
        {
            for (int i = 0; i < numberOfHits; i++)
            {
                HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Tab);
                HomelessTestHelpers.KeyPress(randomKey);                
            }
        }
    }
#endif
}
