// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class BindingFactory : DiscoverableFactory<Binding>
    {
        #region Public Members

        public ConverterForBinding Converter { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ValidationRuleForBinding Rule { get; set; }

        #endregion

        #region Override Members

        public override Binding Create(Core.DeterministicRandom random)
        {
            Binding binding = new Binding();
            binding.Mode = random.NextEnum<BindingMode>();
            binding.UpdateSourceTrigger = random.NextEnum<UpdateSourceTrigger>();
            if (random.NextBool())
            {
                binding.Converter = Converter;
            }

            if (random.NextBool())
            {
                int index = random.Next() % GroupNames.Length;
                binding.BindingGroupName = GroupNames[index];
            }

            if (random.NextBool())
            {
                binding.ValidationRules.Add(Rule);
            }

            return binding;
        }

        #endregion

        private string[] GroupNames = { "GroupName1", "GroupName2", "GroupName3", "GroupName4", "GroupName5", "GroupName6", "GroupName7", "GroupName8" };
    }
}
