// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: 
//              AdornerHitTestResult class, used to return the result from
//              a call to AdornerLayer.AdornerHitTest().
//              See spec at: AdornerLayer Spec.htm
// 

using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Collections;
using MS.Internal;
using System.Windows.Documents;

namespace System.Windows.Media
{
	/// <summary>
	/// Data provided as a result of calling AdornerLayer.AdornerHitTest().
	/// In addition to the visual and point information provided by the base
	/// class PointHitTestResult, also returns the Adorner that was hit (since
	/// there may be multiple Visuals in a single Adorner).
	/// </summary>
	public class AdornerHitTestResult : PointHitTestResult
	{
		private readonly Adorner _adorner;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="visual">Visual that was hit</param>
		/// <param name="pt">Point that was hit, in visual's coordinate space</param>
		/// <param name="adorner">Adorner that was hit</param>
		internal AdornerHitTestResult(Visual visual, Point pt, Adorner adorner) : base(visual, pt)
		{
			_adorner = adorner;
		}

		/// <summary>
		/// Returns the visual that was hit.
		/// </summary>
		public Adorner Adorner
		{
			get
			{
				return _adorner;
			}
		}
	}
}
