// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

namespace System.Windows.Media
{
    /// <summary>
    /// This base returns the visual that was hit during a hit test pass.
    /// </summary>
    public abstract class HitTestResult
    {
         private DependencyObject _visualHit;

         internal HitTestResult(DependencyObject visualHit)
         {
             _visualHit = visualHit;
         }
    
         /// <summary>
         /// Returns the visual that was hit.  May be a Visual or Visual3D.
         /// </summary>
         public DependencyObject VisualHit
         { 
             get
             {
                 return _visualHit;
             }
         }
    }
}

