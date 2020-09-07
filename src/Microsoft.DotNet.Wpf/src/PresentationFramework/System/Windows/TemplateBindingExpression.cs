// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Expression to represent a TemplateBindingExtension during editing of a
*  template.
*
*
\***************************************************************************/
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace System.Windows
{

    /// <summary>
    /// A TemplateBinding is used in templates (e.g. ControlTemplate).  When the content of
    /// a template is instantiated for inspection, the template bindings are represented with
    ///  a TemplateBindingExpression.  (In this case, the expression returns the property's default
    /// value.)
    /// </summary>

    [TypeConverter(typeof(TemplateBindingExpressionConverter))]    
    public class TemplateBindingExpression : Expression
    {
        private TemplateBindingExtension _templateBindingExtension;

        internal TemplateBindingExpression( TemplateBindingExtension templateBindingExtension )
        {
            _templateBindingExtension = templateBindingExtension;
        }


        /// <summary>
        /// Constructor for TemplateBindingExpression
        /// </summary>
        public TemplateBindingExtension TemplateBindingExtension
        {
            get { return _templateBindingExtension; }
        }


        /// <summary>
        ///     Called to evaluate the Expression value
        /// </summary>
        /// <param name="d">DependencyObject being queried</param>
        /// <param name="dp">Property being queried</param>
        /// <returns>Computed value. Default (of the target) if unavailable.</returns>
        internal override object GetValue(DependencyObject d, DependencyProperty dp)
        {
            return dp.GetDefaultValue(d.DependencyObjectType);
        }


    }


}

