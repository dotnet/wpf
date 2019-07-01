// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Threading;
using System.Windows.Media;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add a MediaElement in a Panel(if it contains less than 10 children) in logical tree. 
    /// </summary>
    [TargetTypeAttribute(typeof(MediaElement))]
    public class AddMediaElementInPanel : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Panel Panel { get; set; }

        public MediaElement MediaElement { get; set; }

        public override bool CanPerform()
        {
            return Panel.Children.Count < 2;
        }

        public override void Perform()
        {
            Panel.Children.Add(MediaElement);
        }
    }

    /// <summary>
    /// Remove a MediaElement from its Panel parent.
    /// </summary>
    [TargetTypeAttribute(typeof(MediaElement))]
    public class RemoveMediaElementFromPanel : SimpleDiscoverableAction
    {
        public MediaElement Target { get; set; }


        public override void Perform()
        {
            Panel panel = Target.Parent as Panel;
            if (panel != null)
            {
                panel.Children.Remove(Target);
            }
        }
    }


    /// <summary>
    /// Change MediaElement Properties.
    /// </summary>
    [TargetTypeAttribute(typeof(MediaElement))]
    public class MediaElementChangePropertiesAction : SimpleDiscoverableAction
    {
        public MediaElement Target { get; set; }

        public double Volume { get; set; }
        public double SpeedRatio { get; set; }
        public bool ScrubbingEnabled { get; set; }
        public bool isMuted { get; set; }
        public double Balance { get; set; }
        public Stretch Stretch { get; set; }
        public StretchDirection StretchDirection { get; set; }
        public MediaState LoadedBehavior { get; set; }
        public double PositionScale { get; set; }

        public override void Perform()
        {
            Target.Volume = Volume;
            Target.SpeedRatio = SpeedRatio;
            Target.ScrubbingEnabled = ScrubbingEnabled;
            Target.IsMuted = isMuted;
            Target.Balance = Balance;
            Target.Stretch = Stretch;
            Target.LoadedBehavior = LoadedBehavior;            
            Target.StretchDirection = StretchDirection;

            Duration duration = Target.NaturalDuration;
            TimeSpan timeSpan = duration.TimeSpan;
            int milliseconds = timeSpan.Milliseconds;
            Target.Position = TimeSpan.FromMilliseconds(PositionScale * milliseconds);
        }
    }

    [TargetTypeAttribute(typeof(MediaElement))]
    public class PlayMediaElementAction : SimpleDiscoverableAction
    {
        public MediaElement Element { get; set; }

        public override bool CanPerform()
        {
            return Element.Clock == null;
        }
        public override void Perform()
        {
            Element.Play();
        }
    }


    [TargetTypeAttribute(typeof(MediaElement))]
    public class StopMediaElementAction : SimpleDiscoverableAction
    {
        public MediaElement Element { get; set; }

        public override void Perform()
        {
            if (Element.Clock != null)
            {
                Element.Clock.Controller.Stop();
            }
            else
            {
                Element.Stop();
            }
        }
    }

    [TargetTypeAttribute(typeof(MediaElement))]
    public class ResumeMediaElementAction : SimpleDiscoverableAction
    {
        public MediaElement Element { get; set; }

        public override bool CanPerform()
        {
            return Element.Clock != null;
        }
        public override void Perform()
        {
            Element.Clock.Controller.Resume();   
        }
    }

    [TargetTypeAttribute(typeof(MediaElement))]
    public class PauseMediaElementAction : SimpleDiscoverableAction
    {
        public MediaElement Element { get; set; }

        public override void Perform()
        {
            if (Element.Clock != null)
            {
                Element.Clock.Controller.Pause();
            }
            else
            {
                Element.Pause();
            }
        }
    }
}
