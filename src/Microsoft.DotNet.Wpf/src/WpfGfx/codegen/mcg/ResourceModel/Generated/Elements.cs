﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 
namespace MS.Internal.MilCodeGen.ResourceModel {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class CG {
        
        private CGElement[] elementsField;
        
        private CGEvent[] eventsField;
        
        private CGProperty[] propertiesField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlArrayItemAttribute("Element", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=false)]
        public CGElement[] Elements {
            get {
                return this.elementsField;
            }
            set {
                this.elementsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlArrayItemAttribute("Event", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=false)]
        public CGEvent[] Events {
            get {
                return this.eventsField;
            }
            set {
                this.eventsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlArrayItemAttribute("Property", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=false)]
        public CGProperty[] Properties {
            get {
                return this.propertiesField;
            }
            set {
                this.propertiesField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    public partial class CGElement {
        
        private string nameField;
        
        private string namespaceField;
        
        private string managedDestinationDirField;
        
        private bool implementsIAnimatableField;
        
        private bool implementsIAnimatableFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Namespace {
            get {
                return this.namespaceField;
            }
            set {
                this.namespaceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ManagedDestinationDir {
            get {
                return this.managedDestinationDirField;
            }
            set {
                this.managedDestinationDirField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool ImplementsIAnimatable {
            get {
                return this.implementsIAnimatableField;
            }
            set {
                this.implementsIAnimatableField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ImplementsIAnimatableSpecified {
            get {
                return this.implementsIAnimatableFieldSpecified;
            }
            set {
                this.implementsIAnimatableFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    public partial class CGEvent {
        
        private string nameField;
        
        private string handlerTypeField;
        
        private bool handledTooField;
        
        private bool handledTooFieldSpecified;
        
        private bool translateInputField;
        
        private bool translateInputFieldSpecified;
        
        private bool commandingField;
        
        private bool commandingFieldSpecified;
        
        private string commentField;
        
        private string categoryIDField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string HandlerType {
            get {
                return this.handlerTypeField;
            }
            set {
                this.handlerTypeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool HandledToo {
            get {
                return this.handledTooField;
            }
            set {
                this.handledTooField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool HandledTooSpecified {
            get {
                return this.handledTooFieldSpecified;
            }
            set {
                this.handledTooFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool TranslateInput {
            get {
                return this.translateInputField;
            }
            set {
                this.translateInputField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TranslateInputSpecified {
            get {
                return this.translateInputFieldSpecified;
            }
            set {
                this.translateInputFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool Commanding {
            get {
                return this.commandingField;
            }
            set {
                this.commandingField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool CommandingSpecified {
            get {
                return this.commandingFieldSpecified;
            }
            set {
                this.commandingFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Comment {
            get {
                return this.commentField;
            }
            set {
                this.commentField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string CategoryID {
            get {
                return this.categoryIDField;
            }
            set {
                this.categoryIDField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    public partial class CGProperty {
        
        private string nameField;
        
        private string typeField;
        
        private string defaultValueField;
        
        private bool isReadOnlyField;
        
        private bool isReadOnlyFieldSpecified;
        
        private bool changedEventField;
        
        private bool changedEventFieldSpecified;
        
        private bool reverseInheritField;
        
        private bool reverseInheritFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Type {
            get {
                return this.typeField;
            }
            set {
                this.typeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DefaultValue {
            get {
                return this.defaultValueField;
            }
            set {
                this.defaultValueField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool IsReadOnly {
            get {
                return this.isReadOnlyField;
            }
            set {
                this.isReadOnlyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IsReadOnlySpecified {
            get {
                return this.isReadOnlyFieldSpecified;
            }
            set {
                this.isReadOnlyFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool ChangedEvent {
            get {
                return this.changedEventField;
            }
            set {
                this.changedEventField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ChangedEventSpecified {
            get {
                return this.changedEventFieldSpecified;
            }
            set {
                this.changedEventFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool ReverseInherit {
            get {
                return this.reverseInheritField;
            }
            set {
                this.reverseInheritField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ReverseInheritSpecified {
            get {
                return this.reverseInheritFieldSpecified;
            }
            set {
                this.reverseInheritFieldSpecified = value;
            }
        }
    }
}
