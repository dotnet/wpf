// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Used to store mapping information for names occuring
*  within the template content section.
*
*
\***************************************************************************/
using System;
using System.Diagnostics;
using MS.Internal;
using MS.Utility;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Windows.Markup;

namespace System.Windows
{
    //+-----------------------------------------------------------------------------
    //
    //  Class TemplateNameScope
    //
    //  This class implements the name scope that is used for template content
    //  (i.e. that which is applied to each templated parent, not the template itself).
    //  During apply (except for FEF-based templates), this is the INameScope to which the BamlRecordReader
    //  calls.  We take those opportunities to hook up FEs and FCEs (e.g.
    //  with the TemplatedParent pointer).  After apply, this services
    //  FindName requests.
    //
    //+-----------------------------------------------------------------------------

    internal class TemplateNameScope : INameScope
    {
        #region Constructors

        // This constructor builds a name scope that just supports lookup.
        // This is used for FEF-based templates.

        internal TemplateNameScope(DependencyObject templatedParent)
                        : this( templatedParent, null, null )
        {
        }


        internal TemplateNameScope(
                                    DependencyObject templatedParent,
                                    List<DependencyObject> affectedChildren,
                                    FrameworkTemplate frameworkTemplate )
        {
            Debug.Assert(templatedParent == null || (templatedParent is FrameworkElement || templatedParent is FrameworkContentElement),
                         "Templates are only supported on FE/FCE containers.");

            _affectedChildren = affectedChildren;

            _frameworkTemplate = frameworkTemplate;

            _templatedParent = templatedParent;

            _isTemplatedParentAnFE = true;

        }

        #endregion Constructors

        #region INameScope

        /// <summary>
        /// Registers the name - element combination
        /// </summary>
        /// <param name="name">Name of the element</param>
        /// <param name="scopedElement">Element where name is defined</param>
        void INameScope.RegisterName(string name, object scopedElement)
        {
            // We register FrameworkElements and FrameworkContentElements in FrameworkTemplate.cs
            // Since we register them during object creation, we don't need to process it the second 
            // time it shows up.
            if (!(scopedElement is FrameworkContentElement || scopedElement is FrameworkElement))
            {
                RegisterNameInternal(name, scopedElement);
            }
        }
        internal void RegisterNameInternal(string name, object scopedElement)
        {
            FrameworkElement fe;
            FrameworkContentElement fce;

            Helper.DowncastToFEorFCE( scopedElement as DependencyObject,
                                      out fe, out fce,
                                      false /*throwIfNeither*/ );

            int childIndex;


            // First, though, do we actually have a templated parent?  If not,
            // then we'll just set the properties directly on the element
            // (this is the serialization scenario).

            if( _templatedParent == null )
            {
                if (_nameMap == null)
                {
                    _nameMap = new HybridDictionary();
                }

                _nameMap[name] = scopedElement;

                // No, we don't have a templated parent.  Loop through
                // the shared values (assuming this is an FE/FCE), and set them
                // directly onto the element.

                if( fe != null || fce != null )
                {
                    SetTemplateParentValues( name, scopedElement );
                }

            }

            // We have a templated parent, but is this not a FE/FCE?

            else if (fe == null && fce == null)
            {
                // All we need to do is update the _templatedNonFeChildren list

                Hashtable nonFeChildren = _templatedNonFeChildrenField.GetValue(_templatedParent);
                if (nonFeChildren == null)
                {
                    nonFeChildren = new Hashtable(1);
                    _templatedNonFeChildrenField.SetValue(_templatedParent, nonFeChildren);
                }

                nonFeChildren[name] = scopedElement;

            }

            // Otherwise, we need to hook this FE/FCE up to the template.

            else
            {

                // Update the list on the templated parent of the named FE/FCEs.

                _affectedChildren.Add(scopedElement as DependencyObject);

                // Update the TemplatedParent, IsTemplatedParentAnFE, and TemplateChildIndex.

                if( fe != null )
                {
                    fe._templatedParent = _templatedParent;
                    fe.IsTemplatedParentAnFE = _isTemplatedParentAnFE;

                    childIndex = fe.TemplateChildIndex = (int)_frameworkTemplate.ChildIndexFromChildName[name];
                }
                else
                {
                    fce._templatedParent = _templatedParent;
                    fce.IsTemplatedParentAnFE = _isTemplatedParentAnFE;
                    childIndex = fce.TemplateChildIndex = (int)_frameworkTemplate.ChildIndexFromChildName[name];
                }

                // Entries into the NameScope MUST match the location in the AffectedChildren list
                Debug.Assert(_affectedChildren.Count == childIndex);

                // Make updates for the Loaded/Unloaded event listeners (if they're set).

                HybridDictionary templateChildLoadedDictionary = _frameworkTemplate._TemplateChildLoadedDictionary;

                FrameworkTemplate.TemplateChildLoadedFlags templateChildLoadedFlags
                        = templateChildLoadedDictionary[ childIndex ] as FrameworkTemplate.TemplateChildLoadedFlags;

                if( templateChildLoadedFlags != null )
                {
                    if( templateChildLoadedFlags.HasLoadedChangedHandler || templateChildLoadedFlags.HasUnloadedChangedHandler )
                    {
                        BroadcastEventHelper.AddHasLoadedChangeHandlerFlagInAncestry((fe != null) ? (DependencyObject)fe : (DependencyObject)fce);
                    }
                }


                // Establish databinding instance data.

                StyleHelper.CreateInstanceDataForChild(
                                StyleHelper.TemplateDataField,
                                _templatedParent,
                                (fe!=null) ? (DependencyObject)fe : (DependencyObject)fce,
                                childIndex,
                                _frameworkTemplate.HasInstanceValues,
                                ref _frameworkTemplate.ChildRecordFromChildIndex);
            }

        }

        /// <summary>
        /// Unregisters the name - element combination
        /// </summary>
        /// <param name="name">Name of the element</param>
        void INameScope.UnregisterName(string name)
        {
            Debug.Assert(false, "Should never be trying to unregister via this interface for templates");
        }

        /// <summary>
        /// Find the element given name
        /// </summary>
        object INameScope.FindName(string name)
        {
            // _templatedParent is null if template.LoadContent() was responsible
            if (_templatedParent != null)
            {
                FrameworkObject fo = new FrameworkObject(_templatedParent);
                
                Debug.Assert(fo.IsFE);
                if (fo.IsFE)
                {
                    return StyleHelper.FindNameInTemplateContent(fo.FE, name, fo.FE.TemplateInternal);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (_nameMap == null || name == null || name == String.Empty)
                    return null;
                
                return _nameMap[name];
            }
        }

        #endregion INameScope



        //+----------------------------------------------------------------------------------------------------------------
        //
        //  SetTemplateParentValues
        //
        //  This method takes the "template parent values" (those that look like local values in the template), which
        //  are ordinarily shared, and sets them as local values on the FE/FCE that was just created.  This is used
        //  during serialization.
        //
        //+----------------------------------------------------------------------------------------------------------------

        private void SetTemplateParentValues( string name, object element )
        {
            FrameworkTemplate.SetTemplateParentValues( name, element, _frameworkTemplate, ref _provideValueServiceProvider );
        }






        #region Data

        // This is a HybridDictionary of Name->FE(FCE) mappings
        private List<DependencyObject> _affectedChildren;

        // This is the table of Name->NonFE mappings
        private static UncommonField<Hashtable> _templatedNonFeChildrenField = (UncommonField<Hashtable>)StyleHelper.TemplatedNonFeChildrenField;

        // The templated parent we're instantiating for
        private DependencyObject       _templatedParent;

        // The template we're instantiating
        private FrameworkTemplate      _frameworkTemplate;

        // Is templated parent an FE or an FCE?
        private bool                   _isTemplatedParentAnFE;

        ProvideValueServiceProvider    _provideValueServiceProvider;

        // This is a HybridDictionary of Name-Object maps
        private HybridDictionary _nameMap;

        #endregion Data

    }
}

