// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
All rights reserved.

Module Name:

    ParameterRef.cs

Abstract:

    Definition and implementation of this public feature/parameter related types.

Author:

    Feng Yue (fengy) 7/23/2003

--*/

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;

using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Custom scaling ScaleWidth capability.
    /// </summary>
    internal sealed class ScalingScaleWidthCapability : NonNegativeIntParameterDefinition
    {
        #region Constructors

        internal ScalingScaleWidthCapability() : base()
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static ParameterDefinition NewParamDefCallback(InternalPrintCapabilities printCap)
        {
            ScalingScaleWidthCapability cap = new ScalingScaleWidthCapability();

            return cap;
        }

        #endregion Internal Methods
    }

    /// <summary>
    /// Custom scaling ScaleHeight capability.
    /// </summary>
    internal sealed class ScalingScaleHeightCapability : NonNegativeIntParameterDefinition
    {
        #region Constructors

        internal ScalingScaleHeightCapability() : base()
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static ParameterDefinition NewParamDefCallback(InternalPrintCapabilities printCap)
        {
            ScalingScaleHeightCapability cap = new ScalingScaleHeightCapability();

            return cap;
        }

        #endregion Internal Methods
    }

    /// <summary>
    /// Custom square scaling Scale capability.
    /// </summary>
    internal sealed class ScalingSquareScaleCapability : NonNegativeIntParameterDefinition
    {
        #region Constructors

        internal ScalingSquareScaleCapability() : base()
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static ParameterDefinition NewParamDefCallback(InternalPrintCapabilities printCap)
        {
            ScalingSquareScaleCapability cap = new ScalingSquareScaleCapability();

            return cap;
        }

        #endregion Internal Methods
    }
}