// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Gets PropertyValue of StylusPoint.
    /// </summary>
    public class StylusPointGetPropertyValueAction : SimpleDiscoverableAction
    {
        public InkCanvas InkCanvas { get; set; }

        public StylusPointDescription StylusPointDescription { get; set; }

        public StylusPointCollection StylusPointCollection { get; set; }

        public int StrokeIndex { get; set; }

        public double XFact { get; set; }

        public double YFact { get; set; }

        public int GetValue { get; set; }

        public StylusPointPropertyInfo StylusPointPropertyInfo { get; set; }

        public override void Perform()
        {
            if (InkCanvas.Strokes.Count == 0)
            {
                for (int i = 0; i < StylusPointCollection.Count; i++)
                {
                    if (StylusPointCollection[i].HasProperty(StylusPointPropertyInfo))
                    {
                        if (!StylusPointPropertyInfo.IsButton)
                        {
                            StylusPoint stylusPoint = StylusPointCollection[i];
                            int value = stylusPoint.GetPropertyValue(StylusPointPropertyInfo);
                            value++;
                            stylusPoint.SetPropertyValue(StylusPointPropertyInfo, value);
                            StylusPointCollection[i] = stylusPoint;
                        }
                    }
                }
                //add the stroke and manipulate the points
                Stroke addStroke = new Stroke(StylusPointCollection);
                InkCanvas.Strokes.Add(addStroke);
            }
            else //randomly grab a stroke and tweak the data.
            {
                Stroke stroke = InkCanvas.Strokes[StrokeIndex % InkCanvas.Strokes.Count];
                StylusPointDescription StylusPointDescription = stroke.StylusPoints.Description;
                ReadOnlyCollection<StylusPointPropertyInfo> StylusPointPropertyInfo = StylusPointDescription.GetStylusPointProperties();
                
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                {
                    StylusPoint StylusPoint = stroke.StylusPoints[i];
                    for (int j = 0; j < StylusPointPropertyInfo.Count; j++)
                    {
                        if (stroke.StylusPoints[i].HasProperty(StylusPointPropertyInfo[j]))
                        {
                            if (!StylusPointPropertyInfo[j].IsButton)
                            {
                                if (StylusPointProperties.X.Id == StylusPointPropertyInfo[j].Id)
                                {
                                    StylusPoint.X = StylusPoint.X * XFact;
                                }
                                else if (StylusPointProperties.Y.Id == StylusPointPropertyInfo[j].Id)
                                {
                                    StylusPoint.Y = StylusPoint.Y * YFact;
                                }
                                else
                                {
                                    int value = StylusPoint.GetPropertyValue(StylusPointPropertyInfo[j]);
                                    //pick a random number and set the value
                                    value = Math.Min(StylusPointPropertyInfo[j].Minimum + GetValue % StylusPointPropertyInfo[j].Maximum, StylusPointPropertyInfo[j].Maximum);
                                    StylusPoint.SetPropertyValue(StylusPointPropertyInfo[j], value);
                                }
                            }
                        }
                    }
                    stroke.StylusPoints[i] = StylusPoint;
                    int hash = StylusPoint.GetHashCode();
                }
            }
        }
    }
}
