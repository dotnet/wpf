// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(PasswordBox))]
    class PasswordBoxFactory : DiscoverableFactory<PasswordBox>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MaxLength { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Password { get; set; }
        public Char PasswordChar { get; set; }

        public override PasswordBox Create(DeterministicRandom random)
        {
            PasswordBox passwordBox = new PasswordBox();
            passwordBox.MaxLength = MaxLength;
            passwordBox.Password = Password;
            passwordBox.PasswordChar = PasswordChar;
            return passwordBox;
        }
    }
}
