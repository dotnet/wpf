// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(PrintDialog))]
    class PrintDialogFactory : DiscoverableFactory<PrintDialog>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MinPage { get; set; }

        public override PrintDialog Create(DeterministicRandom random)
        {
            PrintDialog printDialog = new PrintDialog();
            printDialog.MinPage = (uint)MinPage;
            printDialog.MaxPage = (uint)(MinPage + random.Next(10));
            printDialog.PageRangeSelection = random.NextEnum<PageRangeSelection>();
            printDialog.UserPageRangeEnabled = random.NextBool();
            return printDialog;
        }
    }
}
