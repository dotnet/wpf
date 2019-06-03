// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: An implementation of XmlReader that filters out certain bits
//  of underlying XML that we don't want exposed to our caller.  
// 
// Additional remarks: This class is originally created so the 
//  XamlSerializer implementers won't have to worry about things like the 
//  x:Uid attribute that may have been applied everywhere.  We skip them 
//  during the attribute access/traversal methods.  If somebody gets content 
//  some other way, like with "GetInnerXml", "GetOuterXml", or "GetRemainder", 
//  we don't modify that data. The caller will get all the x:Uid attributes.
//
//  The initial implementation disallows index-based attribute access because 
//  that involves a lot more logic - not only does the Uid attribute need to
//  be cloaked, we'll need to determine whether or not the 
//  "xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" attribute needs to be cloaked too.  (There
//  may be a perfectly valid "def" attribute alongside the Uid.)
//
// The challenge with doing a complete filter is the fact that we can't
//  "seek ahead" and rewind.  XmlReader is a forward-only interface.  So it
//  there are situations where it is possible for a x:Uid to slip by.  The 
//  example is the following:
//
//  <Tag defoo:Uid="2" xmlns:defoo="http://schemas.microsoft.com/winfx/2006/xaml"/>
//
// When we get to this element, CheckForUidAttribute will return an erroneous
//  result because it is looking for the wrong prefix.  It can't look for the
//  correct prefix because it doesn't know what to look for... until *after*
//  we've passed it and the definition prefix is updated.  XmlReader internally
//  knows this info ahead of time, but we can't get to it from a subclass.
//
// Rather than give inconsistent results, we'll throw an exception to tell the
//  user that they can't have content like this - defining the Definition
//  prefix within this chunk of XML is not allowed.
//

using System;       // InvalidOperationException
using System.IO;    // TextReader
using System.Xml;   // XmlTextReader

using MS.Utility;   // ExceptionStringTable

namespace System.Windows.Markup
{
internal class FilteredXmlReader : XmlTextReader
{
    //------------------------------------------------------
    //
    //  Public Constructors
    //
    //------------------------------------------------------
    
    // None
    
    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    #region Public Properties

    public override int AttributeCount
    {
        get
        {
            int baseCount = base.AttributeCount;

            // If there's an UID, we're going to hide it from the user.
            if( haveUid )
                return baseCount - 1;
            else
                return baseCount;
        }
    }

    public override bool HasAttributes
    {
        get
        {
            // This should be a simple yes/no, but unfortunately we need the
            //  total count and whether one of them is UID to tell.
            return (AttributeCount != 0);
        }
    }

    public override string this[ int attributeIndex ]
    {
        get
        {
            return GetAttribute( attributeIndex ); // Defined elsewhere in this file.
        }
    }

    public override string this[ string attributeName ]
    {
        get
        {
            return GetAttribute( attributeName ); // Defined elsewhere in this file.
        }
    }

    public override string this[ string localName, string namespaceUri ]
    {
        get
        {
            return GetAttribute( localName, namespaceUri ); // Defined elsewhere in this file.
        }
    }

    #endregion Public Properties
    
    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    #region Public Methods

    public override string GetAttribute( int attributeIndex )
    {
        // Index-based acccess are not allowed.  See remark at top of this file.
        throw new InvalidOperationException(
            SR.Get(SRID.ParserFilterXmlReaderNoIndexAttributeAccess));
    }

    public override string GetAttribute( string attributeName )
    {
        if( attributeName == uidQualifiedName )
        {
            return null;  // ...these aren't the attributes you're looking for...
        }
        else
        {
            return base.GetAttribute( attributeName );
        }
    }
    
    public override string GetAttribute( string localName, string namespaceUri )
    {
        if( localName == uidLocalName && 
            namespaceUri == uidNamespace)
        {
            return null;  // ...these aren't the attributes you're looking for...
        }
        else
        {
            return base.GetAttribute( localName, namespaceUri );
        }
    }

    public override void MoveToAttribute( int attributeIndex )
    {
        // Index-based acccess are not allowed.  See remark at top of this file.
        throw new InvalidOperationException(
            SR.Get(SRID.ParserFilterXmlReaderNoIndexAttributeAccess));
    }

    public override bool MoveToAttribute( string attributeName )
    {
        if( attributeName == uidQualifiedName )
        {
            return false;  // ...these aren't the attributes you're looking for...
        }
        else
        {
            return base.MoveToAttribute( attributeName );
        }
    }

    public override bool MoveToAttribute( string localName, string namespaceUri )
    {
        if( localName == uidLocalName &&
            namespaceUri == uidNamespace)
        {
            return false;  // ...these aren't the attributes you're looking for...
        }
        else
        {
            return base.MoveToAttribute( localName, namespaceUri );
        }
    }

    public override bool MoveToFirstAttribute()
    {
        bool success = base.MoveToFirstAttribute();

        success = CheckForUidOrNamespaceRedef(success);
        
        return success;
    }

    public override bool MoveToNextAttribute()
    {
        bool success = base.MoveToNextAttribute();

        success = CheckForUidOrNamespaceRedef(success);

        return success;
    }

    public override bool Read()
    {
        bool success = base.Read();

        if( success )
        {
            CheckForUidAttribute();
        }

        return success;
    }

    #endregion Public Methods
    
    //------------------------------------------------------
    //
    //  Public Events
    //
    //------------------------------------------------------

    // None
    
    //------------------------------------------------------
    //
    //  Internal Constructors
    //
    //------------------------------------------------------

    #region Internal Constructors

    internal FilteredXmlReader( string xmlFragment, XmlNodeType fragmentType, XmlParserContext context ) : 
        base( xmlFragment, fragmentType, context )
    {
        haveUid = false;
        uidPrefix = defaultPrefix;  
        uidQualifiedName = uidPrefix + ":" + uidLocalName; 
    }

    #endregion Internal Constructors

    //------------------------------------------------------
    //
    //  Internal Properties
    //
    //------------------------------------------------------

    // None
    
    //------------------------------------------------------
    //
    //  Internal Methods
    //
    //------------------------------------------------------

    // None
    
    //------------------------------------------------------
    //
    //  Internal Events
    //
    //------------------------------------------------------

    // None

    //------------------------------------------------------
    //
    //  Private Methods
    //
    //------------------------------------------------------

    #region Private Methods

    // Using our best known information on the fully qualified name for the Uid
    //  attribute, look for it on the current node.
    private void CheckForUidAttribute()
    {
        if( base.GetAttribute(uidQualifiedName) != null ) // Do NOT use base[uidQualifiedName], that just comes right back to us.
            haveUid = true;
        else
            haveUid = false;
    }       

    // We've just moved the attribute "cursor" to the next node.  This may
    //  be an Uid node - which we want to skip.  Or we may end up at a
    //  xmlns:???="http://schemas.microsoft.com/winfx/2006/xaml" where we need to pick up a new prefix.
    private bool CheckForUidOrNamespaceRedef(bool previousSuccessValue)
    {
        bool success = previousSuccessValue;
        
        if( success && 
            base.LocalName == uidLocalName &&
            base.NamespaceURI == uidNamespace)
        {
            // We've found an Uid tag, based on URI Namespace and "Uid" text
            //  value.  However, the prefix before the "Uid" may or may not be 
            //  what we think it is.  Check and update as needed.
            CheckForPrefixUpdate();

            // Move again because we want to skip this.
            success = base.MoveToNextAttribute();
        }

        // We may be handing a xmlns definition back to the user at this point.
        //  this may be the "Definition" re-definition.  (It's nasty that we
        //  might run into its *use* above, before we get the *definition* here.)
        CheckForNamespaceRedef();

        return success;
    }

    // This is called whenever we have a match based on LocalName and NamespaceURI.
    //  If the prefix has changed, we update our best known information on prefix
    //  and the fully qualified name.
    private void CheckForPrefixUpdate()
    {
        if( base.Prefix != uidPrefix )
        {
            uidPrefix = base.Prefix;
            uidQualifiedName = uidPrefix + ":" + uidLocalName;

            // Prefix updated - run a check again for Uid.
            CheckForUidAttribute();
        }
    }

    // This is called whenever a node is about to be returned to the user.  We
    //  check to see if it's a "xmlns" node that re-defines the "Definition"
    //  namespace.
    private void CheckForNamespaceRedef()
    {
        // xmlns node example: < [...] xmlns:someprefix="http://schemas.microsoft.com/winfx/2006/xaml" [...}/>
        //  base.Name = "xmlns:someprefix"
        //  base.LocalName = "someprefix"
        //  base.Prefix = "xmlns"
        //  base.Value ="http://schemas.microsoft.com/winfx/2006/xaml" = uidNamespace
        
        if( base.Prefix == "xmlns" &&         // Fixed via XML spec
            base.LocalName != uidPrefix &&    // Hey, this prefix isn't the one we think it is.
            base.Value == uidNamespace)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.ParserFilterXmlReaderNoDefinitionPrefixChangeAllowed));
        }
    }

    #endregion Private Methods

    //------------------------------------------------------
    //
    //  Private Fields
    //
    //------------------------------------------------------

    #region Private Fields

    // These are fixed, by definition of the Uid feature.
    const string uidLocalName = "Uid";
    const string uidNamespace = XamlReaderHelper.DefinitionNamespaceURI;
    const string defaultPrefix = "def";

    // Best known information on the Definition prefix, updated as we know more.
          string uidPrefix;  
    // Best known information on the fully qualified name, updated as we know more.  
    //  (Updated at same time as uidPrefix.)    
          string uidQualifiedName; 

    // Every time we move to another XML element, we try to see if there is
    //  a "x:UID" on the node.
    bool  haveUid;

    #endregion Private Fields
}
}
