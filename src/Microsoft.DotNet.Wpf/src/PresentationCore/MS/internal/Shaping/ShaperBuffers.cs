// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  base shaping engine buffers
//
//

using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal.PresentationCore;
using MS.Utility;


namespace MS.Internal.Shaping
{
    /// <summary>
    ///     ShaperBuffers encapsulates the shapers non-volatile, non-shareabld buffers 
    /// </summary>
    /// <remarks>
    ///     This class owns several buffers that need to live as long as there's a
    ///     shaping engine that uses them.  Each shaping engine "owns" one of these, but
    ///     the actual mapping/number of ShaperBuffers instances depends
    ///     on the THREAD_SAFE_SHAPERS constant in BaseShape.cs.  If THREAD_SAFE_SHAPERS
    ///     is defined, there's one ShaperBuffers for each thread created in the
    ///     WCP process space.  If not defined, each shaping engine has its own
    ///     ShaperBuffers object.  In this latter case, the owner of the ShapeManager
    ///     that owns the shaping engine is responsible for guaranteeing thread safety (everything
    ///     else in the shape engines is reentrant, but each ShaperBuffers is not.
    ///
    ///     A ShaperBuffers is created whenever a IShaper interface method of a
    ///     shaper is called and its member ShaperBuffers is null (ie, the first time
    ///     a shaper's IShaper interface is invoked).  It exists till the shaper is destroyed.
    /// </remarks>
    internal class ShaperBuffers
    {
        /// <summary>
        /// ShaperBuffers - constructor
        /// </summary>
        public ShaperBuffers(ushort charCount, ushort glyphCount)
        {
            // the charCount is used to provide an initial size for the
            // various buffers used by the shaper(s).  These may
            // grow over the shaper's life
            _glyphInfoList = new GlyphInfoList((glyphCount > charCount ? glyphCount : charCount), 16, false);
            _charMap = new UshortList(charCount, 16);
            _layoutWorkspace = new OpenTypeLayoutWorkspace();

            _charMap.SetRange(0,charCount);
            if (glyphCount > 0)
            {
                _glyphInfoList.SetRange(0,glyphCount);
            }
}
        
         ~ShaperBuffers()
         {
             _glyphInfoList = null;
             _charMap = null;
             _layoutWorkspace = null;
             _textFeatures = null;
         }
         
         // I have laid these out alphabetically.  If a particular variable has
         // an accessor with a different name than the variable, I've added a
         // comment in the position of the variable...

        public UshortList                       CharMap
        {
            get { return _charMap; }
        }
         
        
        public GlyphInfoList                    GlyphInfoList
        {
            get { return _glyphInfoList; }
            set { _glyphInfoList = value; }
        }
        

        /// <summary>
        /// ShaperBuffers.Initialize - initializer for GetGlyphs.
        /// </summary>
        /// <remarks>
        ///   Called by every shaper's GetGlyph method (indirectly -
        ///   this function is actually called by the ShapingWorkspace.Initialize
        ///   method (which is called from IShaper.GetGlyphs)).
        /// </remarks>
        public bool Initialize(ushort charCount, ushort glyphCount)
        {
            if (charCount <= 0)
            {
                return false;
            }

            // clear charmap and resize it        
            if (_charMap.Length > 0)
            {
                _charMap.Remove(0, _charMap.Length);
            }
            _charMap.Insert(0, charCount);
            Debug.Assert(_charMap.Length == charCount);

            // clear glyphinfolist
            if (_glyphInfoList.Length > 0)
            {
                _glyphInfoList.Remove(0, _glyphInfoList.Length);
            }
            if (glyphCount > 0)
            {
                _glyphInfoList.Insert(0, glyphCount);
            }
            Debug.Assert(_glyphInfoList.Length == glyphCount);

            return true;
        }


         /// <summary>
         /// ShaperBuffers.InitializeFeatureList - initializer for GetGlyphs.
         /// </summary>
         /// <param name="size">Requested new array size</param>
         /// <param name="keep">number of features to copy into new array</param>
         /// <remarks>
         ///   Called by pertinent shaper's GetGlyph method; ie, those shapers
         ///   that need to create a text dependent list of features
         ///   (e.g. Arabic, Mongolian).
         ///   The "keep" count takes priority over "size" if there's already an
         ///   array, so if size size is less than keep, the resized array has at
         ///    least keep elements.  It is possible to create a 0 sized array.
         /// </remarks>
         public bool InitializeFeatureList(ushort size, ushort keep)
         {
            if (_textFeatures == null)
            {
                _textFeatures = new ShaperFeaturesList();
                if (_textFeatures == null)
                {
                    return false;
                }
                
                _textFeatures.Initialize(size);
            }
            else
            {
                _textFeatures.Resize(size, keep);
            }

            return true;
         }
         

         public OpenTypeLayoutWorkspace            LayoutWorkspace
         {
             get { return _layoutWorkspace; }
         }
         
         //                                          NextIx; see CurrentCharIx


        public ShaperFeaturesList                  TextFeatures
        {
            get { return _textFeatures; }
        }

        
        // these are one per shaping engine (or per thread) and are kept around
        // between calls (ie, we allocate these once for the lifetime of this
        // ShapingWorkspace instance)
        private UshortList                  _charMap;
        private GlyphInfoList               _glyphInfoList;
        private OpenTypeLayoutWorkspace     _layoutWorkspace;
        private ShaperFeaturesList          _textFeatures;
}

    internal class ShaperFeaturesList
    {
        public int                                FeaturesCount
        {
            get { return _featuresCount; }
        }
         
        public Feature[]                          Features
        {
            get { return _features; }
        }
        
        public int                                NextIx
        {
            get { return _featuresCount; }
        }

        public uint                               CurrentTag
        {
            get { return _featuresCount == 0 ? 0 : _features[_featuresCount - 1].Tag;}
        }

        public int                                Length
        {
            get { return _featuresCount; }
        }

        public void SetFeatureParameter (ushort featureIx, uint paramValue)
        {
            Invariant.Assert ( _featuresCount > featureIx );
            _features[featureIx].Parameter = paramValue;
        }
        
        /// <summary>
        /// ShaperFeateruList.Initialize - initializer for GetGlyphs.
        /// </summary>
        /// <remarks>
        ///   Called by pertinent shaper's GetGlyph method (indirectly -
        ///   this function is actually called by the
        ///   ShaperBuffers.InitializeFeatureList method which is
        ///   called by those shapers that need to create a text dependent
        ///   list of features (e.g. Arabic, Mongolian).
        /// </remarks>
        internal bool Initialize (ushort newSize)
        {
            if (_features == null || 
                newSize > _features.Length ||
                newSize == 0)
            {
                Feature[] newArray = new Feature[newSize];
                if (newArray != null)
                {
                    _features = newArray;
                }
            }

            _featuresCount = 0;
            _minimumAddCount = 3;       // add space for init,med,final whenever we need to actually resize array
            return _features != null;
        }

        /// <summary>
        /// ShaperFeateruList.Resize - used to change the size of the features array
        /// </summary>
        /// <param name="newSize">Requested new array size</param>
        /// <param name="keepCount">number of features to copy into new array</param>
        /// <remarks>
        ///   Used locally for each AddFeature, and by ShaperBuffers.InitializeFeatureList.
        ///   May be called from a shaping engine.
        ///   The "keepCount" count takes priority over "newSize" if there's already an
        ///   array, so if size is less than keep, the resized array has at least keep elements.
        /// </remarks>
        internal bool Resize (ushort newSize, ushort keepCount)
        {
            _featuresCount = keepCount;     // 
   
            if (_features != null && 
                _features.Length != 0 && 
                keepCount > 0 &&
                _features.Length >= keepCount)
            {
                // make sure keep count is no bigger than current
                // array size
                ushort currentLength = (ushort)_features.Length;

                // make sure new size is at least as big as keep count
                if (newSize < keepCount)
                {
                    newSize = keepCount;
                }
                
                // if new size is bigger than the current array, create
                // a new array
                if (newSize > currentLength)
                {
                    // always use minimum leap for adding to array
                    if (newSize < (currentLength + _minimumAddCount))
                    {
                       newSize = (ushort)(currentLength + _minimumAddCount);
                    }
                    
                    Feature[] newArray = new Feature[newSize];
                    if (newArray == null)
                    {
                        // can't create new array, leave (at least we still
                        // have our current array)
                        return false;
                    }
                    
                    // Our client wants us to keep the first "keepCount" entries.
                    // so copy them now.
                    for (int i = 0; i < keepCount; ++i)
                    {
                        newArray[i] = _features[i];
                    }

                    _features = newArray;
                }
            }
            else
            {
                // nothing to keep, or currently no array so initialize
                return Initialize(newSize);
            }

            return true;
}
        
        /// <summary>
        /// ShaperFeateruList.AddFeature - add a feature to the array
        /// </summary>
        /// <param name="feature">new feature to add</param>
        /// <remarks>
        ///     This is aimed at allowing shaping engines to add features
        ///     to the array (generally used for required, all character
        ///     features added at the start of shaping)
        /// </remarks>
        internal void AddFeature (Feature feature)
        {
            if ( _featuresCount == _features.Length )
            {
                // need more space, so resize the array
               if (!Resize((ushort)(_featuresCount + 1),_featuresCount))
               {
                    return; // can't resize array, fail quietly (not going
                            // to apply this feature!)
               }
            }
            
            _features[_featuresCount] = feature;
            ++_featuresCount;
        }

        /// <summary>
        /// ShaperFeateruList.AddFeature - add a feature to the array
        /// </summary>
        /// <remarks>
        ///     An alternative to adding an already created feature.
        /// </remarks>
        internal void AddFeature (ushort startIndex, ushort length, uint featureTag, uint parameter)
        {
            if ( _featuresCount == _features.Length )
            {
                // need more space
               if (!Resize((ushort)(_featuresCount + 1),_featuresCount))
               {
                    return;
               }
            }
            
            if (_features[_featuresCount] != null)
            {
                _features[_featuresCount].Tag = featureTag;
                _features[_featuresCount].StartIndex = startIndex;
                _features[_featuresCount].Length = length;
                _features[_featuresCount].Parameter = parameter;
            }
            else
            {
                _features[_featuresCount] = new Feature(startIndex,length,featureTag,parameter);
            }
            
            ++_featuresCount;
        }

        /// <summary>
        /// ShaperFeateruList.AddFeature - add a feature to the array
        /// </summary>
        /// <remarks>
        ///     This variation of "AddFeature" is used by the shaper state
        ///     machines for adding each new feature.
        /// </remarks>
        internal void AddFeature (ushort charIx, uint featureTag )
        {
            if (featureTag == 1)     // "NotShaped"
            {
                return;
            }
            
            if (_featuresCount > 0)     // if previous feature exists
            {
                // see if this feature can just be subsumed in the latest feature
                ushort latestFeatureIx = (ushort)(_featuresCount - 1);
                if ((featureTag == 0 || featureTag == _features[latestFeatureIx].Tag) &&
                     (_features[latestFeatureIx].StartIndex +
                      _features[latestFeatureIx].Length) == charIx)
                {
                    _features[latestFeatureIx].Length += 1;
                }
                else
                {
                    // can't be added to previous feature, so add one.
                    AddFeature(charIx,
                               1,
                               (featureTag == 0 ? _features[latestFeatureIx].Tag : featureTag),
                               1);
                }
            }
            else if (featureTag != 0)        // cant' be "Same" (there's no feature yet)
            {
                AddFeature(charIx,1,featureTag,1);
            }
}
        
        /// <summary>
        /// ShaperFeateruList.UpdatePreviousShapedChar - adjust previous char's tag
        /// </summary>
        /// <remarks>
        ///     This is used by the shaper state machines for modifying the
        ///     feature tag of the previous character.
        /// </remarks>
        internal void UpdatePreviousShapedChar (uint featureTag)
        {
            if (featureTag <= 1)     // nothing to do if "NotShaped" or "Same"
            {
                return;
            }
            
            if (_featuresCount > 0)
            {
                // see if this feature can just be subsumed in the latest feature
                ushort latestFeatureIx = (ushort)(_featuresCount - 1);

                if (_features[latestFeatureIx].Tag != featureTag)
                {
                    // the previous char's feature just applied to
                    // it, so update its tag and we're done
                    _features[latestFeatureIx].Tag = featureTag;
                }
            }
        }

        private ushort                            _minimumAddCount;
        private ushort                            _featuresCount;
        private Feature[]                         _features;
    }
}
