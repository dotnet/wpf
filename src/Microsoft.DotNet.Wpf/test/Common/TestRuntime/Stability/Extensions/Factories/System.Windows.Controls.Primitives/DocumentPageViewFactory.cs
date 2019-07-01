// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create DocumentPageView.
    /// </summary>
    internal class DocumentPageViewFactory : DiscoverableFactory<DocumentPageView>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a FixedDocumentSequence to set DocumentPageView DocumentPaginator property.
        /// </summary>
        public FixedDocumentSequence FixedDocumentSequence { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a DocumentPageView.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DocumentPageView Create(DeterministicRandom random)
        {
            DocumentPageView documentPageView = new DocumentPageView();

            documentPageView.PageNumber = random.Next();
            documentPageView.Stretch = random.NextEnum<Stretch>();
            documentPageView.StretchDirection = random.NextEnum<StretchDirection>();
            documentPageView.DocumentPaginator = FixedDocumentSequence.DocumentPaginator;

            return documentPageView;
        }

        #endregion
    }
}
