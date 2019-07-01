// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Microsoft.Test.Stability.Extensions.Factories;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(SolidColorBrush))]
    public class FreezableAction : SimpleDiscoverableAction
    {        
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Button Button { get; set; }
                
        public SolidColorBrush Brush { get; set; }

        public override void Perform()
        {            
            //freeze and then apply brush to button background
            if (Brush.CanFreeze)
            {
                Brush.Freeze();
            }
            
            Button.Background = Brush;

            //Clone the brush and apply to button again
            SolidColorBrush brushClone = Brush.Clone();
            brushClone.Color = Colors.Blue;
            Button.Background = brushClone;

            //Freeze the clone to avoid memory leak
            if (brushClone.CanFreeze)
            {
                brushClone.Freeze();
            }
        }
    }

    [TargetTypeAttribute(typeof(Border))]
    public class FreezeableTransformAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]        
        public Border Border {get; set;}                

        public override void Perform()
        {            
            MatrixTransform matrixTransform = (MatrixTransform)Border.RenderTransform;
            Matrix matrix = matrixTransform.Matrix;
            if (matrixTransform.CanFreeze)
            {
                matrixTransform.Freeze();                
            }
            MatrixTransform matrixTransformClone = matrixTransform.Clone();
            matrix.Scale(10, 10);
            matrixTransformClone = new MatrixTransform(matrix);
            if (matrixTransformClone.CanFreeze)
            {
                matrixTransformClone.Freeze();
            }
        }        
    }
#endif
}
