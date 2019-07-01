// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Hyperlink))]
    internal class HyperlinkFactory : InlineFactory<Hyperlink>
    {
        #region Public Members

        public Object CommandParameter { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String TargetName { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri NavigateUri { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string Content { get; set; }

        #endregion

        #region Override Members

        public override Hyperlink Create(DeterministicRandom random)
        {
            Hyperlink hyperlink = new Hyperlink();

            ApplyInlineProperties(hyperlink, random);
            hyperlink.Command = random.NextStaticProperty<RoutedUICommand>(typeof(ApplicationCommands));
            hyperlink.CommandParameter = CommandParameter;
            hyperlink.NavigateUri = NavigateUri;
            hyperlink.TargetName = TargetName;
            hyperlink.Inlines.Add(Content);

            return hyperlink;
        }

        #endregion
    }
}
