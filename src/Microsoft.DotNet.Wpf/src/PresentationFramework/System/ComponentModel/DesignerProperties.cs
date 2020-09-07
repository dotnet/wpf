// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: 
//              Provides attached properties used to communicate with a designer.
//              See spec at: Design%20Mode%20Property.doc
// 


namespace System.ComponentModel
{
    using System;
    using System.Windows;
    using MS.Internal.KnownBoxes;

    /// <summary>
    /// The DesignerProperties class provides attached properties that can be used to 
    /// query the state of a control when it is running in a designer.   Designer tools 
    /// will set values for properties on objects that are running in the designer.
    /// </summary>
    public static class DesignerProperties
    {
        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------


        /// <summary>
        /// Identifies the DesignerProperties.IsInDesignMode dependency property.  
        /// This field is read only.    
        /// </summary>
        public static readonly DependencyProperty IsInDesignModeProperty = 
            DependencyProperty.RegisterAttached(
                 "IsInDesignMode",
                 typeof(bool), typeof(DesignerProperties),
                 new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, 
                 FrameworkPropertyMetadataOptions.Inherits | 
                 FrameworkPropertyMetadataOptions.OverridesInheritanceBehavior));        


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------


        /// <summary>
        /// Returns the attached property IsInDesignMode value for the given dependency object.
        ///
        /// This property will return true if the given element is running in the context of a
        /// designer.  Component developers may use this property to perform different logic 
        /// in the context of a designer than they would when running in an application.  For 
        /// example, expensive validation or connecting to an external resource like a server 
        /// may not make sense while an application is being developed.
        ///
        /// Designers may change the value of this property to move a control from design 
        /// mode to run mode and back.  Components that make changes to their state based 
        /// on the value of this property should override the virtual OnPropertyChanged method 
        /// and update their state if the IsInDesignMode property value changes.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static bool GetIsInDesignMode(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException("element");
            return (bool)element.GetValue(IsInDesignModeProperty);
        }        


        /// <summary>
        /// Sets the value of the IsInDesignMode attached property for the given dependency object.
        /// </summary>
        public static void SetIsInDesignMode(DependencyObject element, bool value)
        {
            if (element == null) throw new ArgumentNullException("element");
            element.SetValue(IsInDesignModeProperty, value);
        }        
    }
}
