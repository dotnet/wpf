// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Test.Threading;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Test.Stability.Extensions.Utilities;
using System.Windows.Markup;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(CallGoToStateAction))]
    class CallGoToStateAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Control Target { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int GroupIndex { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int StateIndex { get; set; }

        public override void Perform()
        {
            var root = GetTemplateRoot(Target);
            if (root != null)
            {
                var groups = System.Windows.VisualStateManager.GetVisualStateGroups(root) as Collection<VisualStateGroup>;
                if (groups != null && groups.Count > 0)
                {
                    var group = groups[GroupIndex % groups.Count];
                    if (group.States.Count > 0)
                    {
                        VisualState state = group.States[StateIndex % group.States.Count] as VisualState;
                        VisualStateManager.GoToState(Target, state.Name, false);

                        DispatcherHelper.DoEvents(DispatcherPriority.ApplicationIdle);
                    }
                }
            }
        }

        private FrameworkElement GetTemplateRoot(Control control)
        {
            UserControl userControl = control as UserControl;
            if (userControl != null)
            {
                // If using a UserControl, the states will be specified on the
                // root of the content instead of the root of the template.
                return userControl.Content as FrameworkElement;
            }
            else
            {
                if (VisualTreeHelper.GetChildrenCount(control) > 0)
                {
                    return VisualTreeHelper.GetChild(control, 0) as FrameworkElement;
                }
            }

            return null;
        }
    }

    [TargetTypeAttribute(typeof(ApplyVsmTemplateAction))]
    class ApplyVsmTemplateAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Control Target { get; set; }

        public override void Perform()
        {
            ResourceDictionary vsmResourceDictionary = null; 
            using (FileStream fs = new FileStream("VSMResourceDictionary.xaml", FileMode.Open, FileAccess.Read))
            {
                vsmResourceDictionary = (ResourceDictionary)XamlReader.Load(fs);
            }

            if (vsmResourceDictionary != null)
            {
                var key = Target.GetType().Name;
                if (vsmResourceDictionary.Contains(key))
                {
                    var style = vsmResourceDictionary[key] as Style;
                    Target.Style = style;
                }
            }
        }
    }

    [TargetTypeAttribute(typeof(AddVsmControlToWindowAction))]
    class AddVsmControlToWindowAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }

        public Control Target { get; set; }

        public override void Perform()
        {
            Window.Content = Target;
            ResourceDictionary vsmResourceDictionary = null;

            // load style from a xaml and apply to the control. 
            using (FileStream fs = new FileStream("VSMResourceDictionary.xaml", FileMode.Open, FileAccess.Read))
            {
                vsmResourceDictionary = (ResourceDictionary)XamlReader.Load(fs);
            }

            if (vsmResourceDictionary != null)
            {
                var key = Target.GetType().Name;
                if (vsmResourceDictionary.Contains(key))
                {
                    var style = vsmResourceDictionary[key] as Style;
                    Target.Style = style;
                }
            }


            DispatcherHelper.DoEvents(DispatcherPriority.ApplicationIdle);
        }
    }

#endif
}
