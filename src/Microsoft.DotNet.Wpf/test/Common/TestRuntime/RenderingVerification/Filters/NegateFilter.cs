// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// A Negate Filter
    /// </summary>
    public class NegateFilter: Filter
    {
        #region Constants
        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "Return the negative of an image.";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Negate Filter constructor
            /// </summary>
            public NegateFilter()
            {
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    LogicalOperationFilter logicalFilter = new LogicalOperationFilter();
                    logicalFilter.Not = true;
                    return logicalFilter.Process(source);
                }
            #endregion Public Methods
        #endregion Methods
    }
}
