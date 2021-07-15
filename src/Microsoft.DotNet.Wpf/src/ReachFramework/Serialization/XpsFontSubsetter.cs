// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                                                                                                            
    Abstract:
        This file implements the XpsFontSubsetter used by
        the Xps Serialization APIs for serializing fonts
        to a Xps package.                                                                            
--*/
using System;
using System.Collections;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Windows.Xps.Packaging;
using System.Security;
using MS.Internal.Utility;
using MS.Internal.IO.Packaging;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Flags indicating which parts are to be exluded
    /// from a digital signature
    /// May not be or'ed together
    /// </summary>
    [FlagsAttribute]
    public enum FontSubsetterCommitPolicies
    {
        /// <summary>
        /// No font subsettint, the entire font is copied
        /// fonts are assumed shared accross all documents
        /// </summary>
        None                        = 0x00000000,
        /// <summary>
        /// Font parts will be generted for the fonts 
        /// used in a single page
        /// </summary>
        CommitPerPage               = 0x00000001,
        /// <summary>
        /// Font parts will be generted for the fonts 
        /// used in a single document
        /// </summary>
        CommitPerDocument           = 0x00000002,
        /// <summary>
        /// Font parts will be generted for all fonts 
        /// used in accros the entire sequence
        /// </summary> 
        CommitEntireSequence       =  0x00000003,
    };

    /// <summary>
    /// Actions to be taken to embed font
    /// based on type flag
    /// </summary>
    [FlagsAttribute]
    internal enum FontEmbeddingAction
    {
        /// <summary>
        /// No action
        /// </summary>
        None                       = 0x00000000,
        /// <summary>
        /// Obfuscate and subset the font
        /// </summary>
        ObfuscateSubsetFont        = 0x00000001,
    
        /// <summary>
        /// Obfuscate but copy the data 
        /// directly from the font stream
        /// </summary>
        ObfuscateOnlyFont         = 0x00000002,

        /// <summary>
        /// Generate Images of glyps using Image Brush
        /// directly from the font stream
        /// </summary>
        ImageOnlyFont             = 0x00000004,

    }

    
    /// <summary>
    /// Implements the functionality to generate font subsets
    /// based on glyph runs obtained.  This class uses the
    /// serialization manager to write data to the Xps
    /// package.
    /// </summary>
    internal class XpsFontSubsetter
    {
        #region Constructors

        /// <summary>
        /// Constructs a XpsFontSubsetter instance.
        /// </summary>
        /// <param name="packagingPolicy">
        /// The serialization manager to serialize to.
        /// </param>
        public
        XpsFontSubsetter(
            BasePackagingPolicy packagingPolicy
            )
        {
            if (null == packagingPolicy)
            {
                throw new ArgumentNullException("packagingPolicy");
            }

            _packagingPolicy = packagingPolicy;
            _fontEmbeddingManagerCache = new Dictionary<Uri, FEMCacheItem>(3, MS.Internal.UriComparer.Default);
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Adds a specified Glyph run to an existing or new
        /// cache item and returns the part Uri where font
        /// will be committed.
        /// </summary>
        /// <param name="glyphRun">
        /// The GlyphRun to add.
        /// </param>
        /// <returns>
        /// A Uri to locate the font within the Xps package.
        /// </returns>
        public
        Uri
        ComputeFontSubset(
            GlyphRun        glyphRun
            )
        {
            if (null == glyphRun)
            {
                throw new ArgumentNullException("glyphRun");
            }

            FontEmbeddingRight embeddingRights = glyphRun.GlyphTypeface.EmbeddingRights;

            Uri fontUri = null;

            if (DetermineEmbeddingAction(embeddingRights) ==
                    FontEmbeddingAction.ImageOnlyFont)
            {
                //
                // Aquire a bit map stream to embedd the Glyph Run image
                //
                fontUri = _packagingPolicy.AcquireResourceStreamForXpsImage(XpsS0Markup.ImageUriPlaceHolder).Uri;
            }
            else
            {
                 FEMCacheItem cacheItem = AcquireCacheItem(
                                            glyphRun.GlyphTypeface
                                            );
                if (cacheItem != null)
                {
                    cacheItem.CurrentPageReferences = true;
                    if( _commitPolicy == FontSubsetterCommitPolicies.None &&
                        !cacheItem.IsStreamWritten )
                    {
                        fontUri = cacheItem.CopyFontStream();
                    }
                    else
                    {
                        fontUri = cacheItem.AddGlyphRunUsage(glyphRun);
                    }
                }
            }

            return fontUri;
        }


        /// <summary>
        /// Commits all fonts that are currently in the cache
        /// and clears the cache out.
        /// </summary>
        /// <param name="signal">
        /// Signal indicating the font should be subsetted 
        /// according to which policy.
        /// </param>
        /// <return>
        /// true if the signal completed a subset.
        /// </return>
        public
        bool
        CommitFontSubsetsSignal(
            FontSubsetterCommitPolicies signal
            )
        {
            //
            // flag indicating if this signal completed a subset
            //
            bool completedSubset = false;
            if( signal == FontSubsetterCommitPolicies.CommitPerPage )
            {
                foreach (FEMCacheItem item in _fontEmbeddingManagerCache.Values)
                {
                    if (item != null)
                    {
                        item.AddRelationship();
                        item.CurrentPageReferences = false;
                    }
                }

            }
            else
            if( signal == FontSubsetterCommitPolicies.CommitPerDocument &&
                _commitPolicy == FontSubsetterCommitPolicies.CommitEntireSequence )
            {
                foreach (FEMCacheItem item in _fontEmbeddingManagerCache.Values)
                {
                    if (item != null)
                    {
                        item.AddRestrictedRelationship();
                    }
                }

            }      
            //
            // If we recieve a signal to commit a superset of what we are commiting on
            // i.e. Document Signal with commit Policy is per page
            // we must commit
            //
            if( signal >= _commitPolicy )
            {
                if (signal == _commitPolicy)
                {
                    _currentCommitCnt++;
                }
                if( signal > _commitPolicy || _currentCommitCnt%_commitCountPolicy == 0 )
                {
                    foreach (FEMCacheItem item in _fontEmbeddingManagerCache.Values)
                    {
                        if (item != null && _commitPolicy != FontSubsetterCommitPolicies.None)
                        {
                            item.Commit();
                            //
                            // We will be removing this FEMCacheItem from the cache 
                            // after this loop so we must add its document relationship
                            // if restricted.  If the policy is CommitPerDocumentSequence
                            // we do not need to add since it will have been added at the document signal
                            //
                            if (signal != FontSubsetterCommitPolicies.CommitEntireSequence)
                            {
                                item.AddRestrictedRelationship();
                            }
                        }
                    }
                    _fontEmbeddingManagerCache.Clear();
                    _currentCommitCnt = 0;
                    completedSubset = true;
                }
            }
            return completedSubset;
        }

        
        /// <summery>
        /// Determines Embedding action based
        /// on flags in the fsType field
        /// </summery>
        public
        static
        FontEmbeddingAction
        DetermineEmbeddingAction(GlyphTypeface glyphTypeface)
        {
            return DetermineEmbeddingAction(glyphTypeface.EmbeddingRights);
        }
        /// <summary> 
        /// Determines Embedding action based
        /// on flags in the fsType field
        ///</summary> 
        public
        static
        FontEmbeddingAction
        DetermineEmbeddingAction(
           FontEmbeddingRight fsType
            )
        {
            FontEmbeddingAction action = FontEmbeddingAction.ObfuscateSubsetFont;

            switch( fsType )
            {
                case FontEmbeddingRight.RestrictedLicense:
                case FontEmbeddingRight.PreviewAndPrintButWithBitmapsOnly:
                case FontEmbeddingRight.PreviewAndPrintButNoSubsettingAndWithBitmapsOnly:
                case FontEmbeddingRight.EditableButWithBitmapsOnly:
                case FontEmbeddingRight.EditableButNoSubsettingAndWithBitmapsOnly:
                case FontEmbeddingRight.InstallableButWithBitmapsOnly:
                case FontEmbeddingRight.InstallableButNoSubsettingAndWithBitmapsOnly:
                    action = FontEmbeddingAction.ImageOnlyFont;
                    break;

                case FontEmbeddingRight.PreviewAndPrint:
                case FontEmbeddingRight.Editable:
                case FontEmbeddingRight.Installable:
                    action = FontEmbeddingAction.ObfuscateSubsetFont;
                    break;

                case FontEmbeddingRight.EditableButNoSubsetting:                    
                case FontEmbeddingRight.PreviewAndPrintButNoSubsetting:
                case FontEmbeddingRight.InstallableButNoSubsetting:
                    action = FontEmbeddingAction.ObfuscateOnlyFont;
                    break;           
            }
            return action;            
        }

        /// <summery>
        /// Determines Embedding action based
        /// on flags in the fsType field
        /// </summery>
        public
        static
        bool
        IsRestrictedFont(GlyphTypeface glyphTypeface)
        {
            return IsRestrictedFont(glyphTypeface.EmbeddingRights);
        }
        /// <summary> 
        /// Determines Embedding action based
        /// on flags in the fsType field
        ///</summary> 
        public
        static
        bool
        IsRestrictedFont(
           FontEmbeddingRight fsType
            )
        {
            bool isRestrictedFont = false;

            switch( fsType )
            {                    
                case FontEmbeddingRight.PreviewAndPrintButNoSubsetting:
                case FontEmbeddingRight.PreviewAndPrint:
                    isRestrictedFont = true;
                    break;
           }
           return isRestrictedFont;            
        }
        
        /// <summary> 
        /// Determines on what signal subsets will be commited
        ///</summary> 
        public
        void
        SetSubsetCommitPolicy( FontSubsetterCommitPolicies policy )
        {
           if( policy == FontSubsetterCommitPolicies.CommitEntireSequence &&
                _commitCountPolicy != 1 )
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.ReachPackaging_SequenceCntMustBe1));
            }
          _commitPolicy = policy;
        }

        /// <summary> 
        /// Determines the number of signals subsets will be commited on
        ///</summary> 
        public
        void
        SetSubsetCommitCountPolicy( int commitCount )
        {
            if( _commitPolicy == FontSubsetterCommitPolicies.CommitEntireSequence &&
                commitCount != 1 )
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.ReachPackaging_SequenceCntMustBe1));
            }
            else
            if( commitCount < 1 )
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.ReachPackaging_CommitCountPolicyLessThan1));
            }
            _commitCountPolicy = commitCount;
        }
        #endregion Public methods
        
        #region Private data

        private IDictionary<Uri, FEMCacheItem>  _fontEmbeddingManagerCache;
        private BasePackagingPolicy             _packagingPolicy;
        private FontSubsetterCommitPolicies     _commitPolicy = FontSubsetterCommitPolicies.CommitEntireSequence;
        private int                             _commitCountPolicy = 1;
        private int                             _currentCommitCnt = 0;
        #endregion Private data

        #region Private methods

        /// <summary>
        /// Acquires a cache item for the specified font.  If one
        /// does not exist a new one is created.
        /// </summary>
        /// <param name="glyphTypeface">
        /// glyphTypeface of font to acquire.
        /// </param>
        /// <returns>
        /// A reference to a FEMCacheItem.
        /// </returns>
        private
        FEMCacheItem
        AcquireCacheItem(
            GlyphTypeface         glyphTypeface
            )
        {
            FEMCacheItem manager = null;
            Uri fontUri = glyphTypeface.FontUri;

            if (!_fontEmbeddingManagerCache.TryGetValue(fontUri, out manager))
            {
                manager = new FEMCacheItem(glyphTypeface, _packagingPolicy);
                _fontEmbeddingManagerCache.Add(fontUri, manager);
            }

            return manager;
        }


        #endregion Private methods
    }

    /// <summary>
    /// This class represents a single cache item for
    /// a FontEmbeddingManager.  Each manager represents
    /// one font.
    /// </summary>
    internal class FEMCacheItem
    {
        #region Constructors

        /// <summary>
        /// Constructs a new FEMCacheItem.
        /// </summary>
        /// <param name="glyphTypeface">
        /// The glyphTypeface of the font for this cache item.
        /// </param>
        /// <param name="packagingPolicy">
        /// The BasePackagingPolicy to write to.
        /// </param>
        public
        FEMCacheItem(
            GlyphTypeface                   glyphTypeface,
            BasePackagingPolicy             packagingPolicy
            )
        {
            if (null == packagingPolicy)
            {
                throw new ArgumentNullException("packagingPolicy");
            }
            if (null == glyphTypeface)
            {
                throw new ArgumentNullException("glyphTypeface");
            }

            _packagingPolicy = packagingPolicy;
            _streamWritten = false;

            //
            // Acquire the stream that will be used to save the font.
            // if the font is Image only we do not need a font stream 
            // since each glyph run will be saved in its own image stream
            //
            _fontEmbeddingManager = new FontEmbeddingManager();
            _glyphTypeface = glyphTypeface;
            _fontUri = glyphTypeface.FontUri;
            
            Uri fontUri = new Uri(_fontUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped), UriKind.RelativeOrAbsolute);
            string fontUriAsString = fontUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            _fontResourceStream = packagingPolicy.AcquireResourceStreamForXpsFont(fontUriAsString);
            
            //
            // Aquiring the Resource stream will instantiate the font
            // and add the require resource relationship
            //
            _curPageRelAdded = true; 
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Adds a specified glyph run to the ongoing subset.
        /// </summary>
        /// <param name="glyphRun">
        /// The GlyphRun to add to subset.
        /// </param>
        /// <returns>
        /// A reference to a Uri where font is stored
        /// within the package.
        /// </returns>
        public
        Uri
        AddGlyphRunUsage(
            GlyphRun        glyphRun
            )
        {
            Uri fontUri = null;
            FontEmbeddingAction action = XpsFontSubsetter.DetermineEmbeddingAction( _glyphTypeface );
            switch( action )
            {
                //
                // Provide an empty image stream.  The Glyphs serializer 
                // will check content type and render adn serialize an 
                // image into this stream based on content type
                //
                case FontEmbeddingAction.ImageOnlyFont:
                    break;
                    
                case FontEmbeddingAction.ObfuscateOnlyFont:
                    fontUri = _fontResourceStream.Uri;
                    // nothing to do here since the entire font will be added
                    break;
                    
                case FontEmbeddingAction.ObfuscateSubsetFont:
                    _fontEmbeddingManager.RecordUsage(glyphRun);
                    fontUri = _fontResourceStream.Uri;
                    break;
            }
            return fontUri;
        }

        /// <summary>
        /// Computes the font subset of all GlyphRuns seen
        /// and commits the font to the Xps package.
        /// This ends the lifetime of this cache item.
        /// </summary>
        public
        void
        Commit(
            )
        {
            FontEmbeddingAction action = XpsFontSubsetter.DetermineEmbeddingAction( _glyphTypeface );
            switch( action )
            {
                case FontEmbeddingAction.ImageOnlyFont:
                    // nothing to do here since the glyph runs have already been converted
                    break;
                    
                case FontEmbeddingAction.ObfuscateOnlyFont:
                    CopyFontStream();
                    break;
                    
                case FontEmbeddingAction.ObfuscateSubsetFont:
                    SubSetFont(
                        _fontEmbeddingManager.GetUsedGlyphs(_fontUri),
                        _fontResourceStream.Stream
                        );
                    break;

            }
        }

        public
        void
        AddRestrictedRelationship(
            )
        {
            FontEmbeddingAction action = XpsFontSubsetter.DetermineEmbeddingAction( _glyphTypeface );
            if( action != FontEmbeddingAction.ImageOnlyFont &&             
                XpsFontSubsetter.IsRestrictedFont(_glyphTypeface) )
            {
                _packagingPolicy.
                   RelateRestrictedFontToCurrentDocument(
                    _fontResourceStream.Uri
                    );
            }        }
        public
        void
        AddRelationship(
            )
        {
            if( !_curPageRelAdded && CurrentPageReferences)
            {
                FontEmbeddingAction action = XpsFontSubsetter.DetermineEmbeddingAction( _glyphTypeface );
                switch( action )
                {
                    case FontEmbeddingAction.ImageOnlyFont:
                        break;
                        
                    case FontEmbeddingAction.ObfuscateOnlyFont:
                    case FontEmbeddingAction.ObfuscateSubsetFont:
                        _packagingPolicy.
                            RelateResourceToCurrentPage(
                            _fontResourceStream.Uri, 
                            XpsS0Markup.ResourceRelationshipName); 
                         break;
                }
            }

            // Now we are finished with the current page
            // and need flag the need for relationship on the next
            _curPageRelAdded = false;
        }
        #endregion Public methods

        #region Private static methods

        private
        void
        SubSetFont(
            ICollection<ushort> glyphs,
            Stream stream
            )
        {
            byte[] fontData = _glyphTypeface.ComputeSubset(glyphs);
            
            Guid guid = ParseGuidFromUri(_fontResourceStream.Uri);
            ObfuscateData(fontData, guid);
 
            stream.Write(fontData, 0, fontData.Length);

            Uri fontUri = new Uri(_fontUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped), UriKind.RelativeOrAbsolute);
            string fontUriAsString = fontUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            _packagingPolicy.ReleaseResourceStreamForXpsFont(fontUriAsString);
            
            _streamWritten = true;
        }

        internal
        Uri
        CopyFontStream()
        {
            Uri sourceUri =     _fontUri;
            Uri destUri =       _fontResourceStream.Uri;
            Stream destStream = _fontResourceStream.Stream;
            Stream sourceStream = null;
            byte [] memoryFont;
            GlyphTypeface glyphTypeface = new GlyphTypeface(sourceUri);

            sourceStream = glyphTypeface.GetFontStream();
            
            memoryFont = new byte[_readBlockSize];
            Guid guid = ParseGuidFromUri(destUri);
            
            int bytesRead = PackagingUtilities.ReliableRead(sourceStream, memoryFont,0,_readBlockSize);
            if (bytesRead > 0)
            {
                 // Obfuscate the first block
                ObfuscateData(memoryFont, guid );
            }
            
            while( bytesRead > 0 )
            {
                destStream.Write(memoryFont, 0, bytesRead);
                bytesRead = PackagingUtilities.ReliableRead( sourceStream, memoryFont, 0, _readBlockSize);  
            }

            Uri fontUri = new Uri(_fontUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped), UriKind.RelativeOrAbsolute);
            string fontUriAsString = fontUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            _packagingPolicy.ReleaseResourceStreamForXpsFont(fontUriAsString);
            
            _streamWritten = true;
            return destUri;
        }

        /// <summary>
        /// Parses a Guid from the file name 
        /// in accordence with 6.2.7.3  Embedded Font Obfuscation
        /// of the metro spec
        /// </summary>
        /// <param name="uri">
        /// Uri to parse
        /// </param>
        /// <returns>
        /// Guid parsed out of Uri
        /// </returns>
        private
        static
        Guid
        ParseGuidFromUri( Uri uri )
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension( 
                                BindUriHelper.UriToString(uri) 
                              );
            return new Guid( fileName );
        }
        
        /// <summary>
        /// Obfuscate font data  
        /// in accordence with 6.2.7.3  Embedded Font Obfuscation
        /// of the metro spec
        /// </summary>
        /// <param name="fontData">
        ///  Data to obfuscate
        /// </param>
        /// <param name="guid">
        /// Guid to be used in XORing the header
        /// </param>
        public
        static
        void
        ObfuscateData( byte[] fontData, Guid guid )
        {
            byte[] guidByteArray = new byte[16];
          // Convert the GUID into string in 32 digits format (xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx)
            string guidString = guid.ToString("N");

  
            for (int i = 0; i < guidByteArray.Length; i++)
            {

                guidByteArray[i] = Convert.ToByte(guidString.Substring(i * 2, 2), 16);

            }
 

 
            for( int j = 0; j < 2; j++ )
            {
                for( int i = 0; i < 16; i ++ )                {
                    fontData[i+j*16] ^= guidByteArray[15-i];
                }
            }
        }
        #endregion Private static methods
        
        #region Public Properties
        public
        bool
        IsStreamWritten
        {
         get
         {
            return _streamWritten;
         }
        }

        public
        bool
        CurrentPageReferences
        {
            get
            {
                return _currentPageReferences;
            }
            set
            {
                _currentPageReferences = value;
            }
        }
        #endregion Public Properties

        #region Private data

        private bool                    _currentPageReferences;
        private bool                    _curPageRelAdded;
        private FontEmbeddingManager    _fontEmbeddingManager;
        private BasePackagingPolicy     _packagingPolicy;
        private XpsResourceStream       _fontResourceStream;
        private GlyphTypeface           _glyphTypeface;
        private bool                    _streamWritten; 
        private Uri _fontUri;
        private static readonly int _readBlockSize = 1048576; //1MB

        #endregion Private data
    }
}
