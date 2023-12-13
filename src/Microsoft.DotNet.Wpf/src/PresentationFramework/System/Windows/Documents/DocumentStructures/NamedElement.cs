// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal.Documents;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Markup;

[assembly: XmlnsDefinition(
    "http://schemas.microsoft.com/xps/2005/06/documentstructure",
    "System.Windows.Documents.DocumentStructures")]

namespace System.Windows.Documents.DocumentStructures
{
    /// <summary>
    ///
    /// </summary>
    public class BlockElement
    {
        internal FixedElement.ElementType ElementType
        {
            get { return _elementType;}
        }

        internal  FixedElement.ElementType _elementType;
    }

    /// <summary>
    ///
    /// </summary>
    public class StoryBreak : BlockElement
    {
    }

    /// <summary>
    ///
    /// </summary>
    public class NamedElement : BlockElement
    {
        /// <summary>
        ///
        /// </summary>
        public NamedElement()
        {
        }
        /// <summary>
        /// The element name
        /// </summary>
        public string NameReference
        {
            get
            {
                return _reference;
            }
            set
            {
                _reference = value;
            }
        }

        private string _reference;
    }
  }

