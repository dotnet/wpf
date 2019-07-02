// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using Microsoft.Test.RenderingVerification.Filters;
   #endregion using

    /// <summary>
    /// Provide data for the Match found event
    /// </summary>
    public class GlyphComparatorEventArgs: EventArgs
    {
        #region Properties
            /// <summary>
            /// Information about the match found
            /// </summary>
            public MatchingInfo MatchingInfo;
        #endregion Properties

        #region Constructors
            private GlyphComparatorEventArgs() {}
            /// <summary>
            /// Create a new instance of the GlyphComparatorEventArgs class
            /// </summary>
            /// <param name="matchingInfo"></param>
            public GlyphComparatorEventArgs(MatchingInfo matchingInfo)
            {
                MatchingInfo = matchingInfo;
            }
        #endregion Constructors
    }

    /// <summary>
    /// Summary description for GlyphComparator.
    /// </summary>
    public class GlyphComparator
    {
        #region Delegates & events
            /// <summary>
            /// Delegate to handle Match found events
            /// </summary>
            public delegate void MatchFoundEventHandler(object sender, GlyphComparatorEventArgs e);
            /// <summary>
            /// The event raised on a match found
            /// </summary>
            public event MatchFoundEventHandler MatchFoundEvent;
        #endregion Delegates & events

        #region Properties
            #region Private Properties
                GlyphBase _glyphToFind = null;
                GlyphBase _glyphSearched = null;
            #endregion Private Properties
            #region Internal Properties
            #endregion Internal Properties
            #region Public Properties
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Instantiate a new GlyphComparator object
            /// </summary>
            public GlyphComparator() 
            {
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void GlyphComparator_MatchFoundInternal(object sender, GlyphComparatorEventArgs e)
                {
                    if (_glyphToFind != null) { _glyphToFind.CompareInfo.Matches.Add(e.MatchingInfo); }
                    if (_glyphSearched != null) { _glyphSearched.CompareInfo.Matches.Add(e.MatchingInfo); }
                }
                private void AddInc(float val, float[] hist)
                {
#if DEBUG
                    if (val < 0f) { throw new RenderingVerificationException("AddInc / val < 0; should never occur"); }
                    if (val > 1f) { throw new RenderingVerificationException("AddInc / val > 1; should never occur"); }
#endif  // DEBUG

                    val *= MatchingInfo.HistogramLength - 1;
                    hist[(int)val]++;
                }
/*
                /// <summary>
                /// Process the matching found (sorts, inserts or rejects) 
                /// </summary>
                private void ProcessMatch(MatchingInfo matchingInfo)
                {
                    int index = 0;
                    foreach (MatchingInfo existingMatchingInfo in Matches)
                    {
                        if (existingMatchingInfo.Error >= matchingInfo.Error) { break; }
                        index++;
                    }
                    if (index < MaxMatch)
                    {
                        Matches.Insert(index, matchingInfo);
                        if (Matches.Count > MaxMatch) { Matches.RemoveRange(MaxMatch, 1); }

                        // Raise event
                        if (NewMatch != null) { NewMatch(this, matchingInfo); }
                    }
                }
*/
                /// <summary>
                /// Check if any of the children use the EdgeOnly property
                /// </summary>
                /// <returns></returns>
                private bool GlyphUseEdgeOnly(GlyphBase[] glyphArray)
                {
                    foreach (GlyphBase glyph in glyphArray)
                    {
                        if (glyph is GlyphContainer)
                        {
                            bool result = GlyphUseEdgeOnly((GlyphBase[])((GlyphContainer)glyph).Glyphs.ToArray(typeof(GlyphBase)));
                            if (result) { return result; }
                        }
                        else
                        {
                            if (glyph.CompareInfo.EdgesOnly == true) { return true; }
                        }
                    }
                    return false;
                }
                private IImageAdapter RenderGlyphs(GlyphBase[] glyphs, IImageAdapter renderSurface)
                {
                    foreach (GlyphBase glyphBase in glyphs)
                    {
                        Point pt = new Point((int)(glyphBase.Position.X + .5), (int)(glyphBase.Position.Y + .5));
                        renderSurface = ImageUtility.CopyImageAdapter(renderSurface, glyphBase.Render(), pt, glyphBase.Size, true);
                    }
                    return renderSurface;
                }
                /// <summary>
                /// 
                /// </summary>
                /// <param name="imageToParse"></param>
                /// <param name="imageToFind"></param>
                /// <param name="maxErr"></param>
                /// <returns></returns>
                private bool FindImageAdapter(IImageAdapter imageToParse, IImageAdapter imageToFind, double maxErr)
                {
                    if (imageToParse == null) { throw new ArgumentNullException("imageToParse", "variable must be set to a valid instance of an object implementing IImageAdapter (null passed in)"); }
                    if (imageToFind == null) { throw new ArgumentNullException("imageToFind", "variable must be set to a valid instance of an object implementing IImageAdapter (null passed in)"); }

                    float[] hist = new float[MatchingInfo.HistogramLength];
                    int pixelCount = imageToFind.Height * imageToFind.Width;

                    int maxX = (int)imageToParse.Width;
                    int maxY = (int)imageToParse.Height;
                    int minX = 0;
                    int minY = 0;
                    long feedbackCount = 0;
                    bool found = false;

                    // Parse the whole image
                    for (int y = minY; y < maxY; y++)
                    {
                        for (int x = minX; x < maxX; x++)
                        {
                            if (y + imageToFind.Height >= maxY || x + imageToFind.Width >= maxX) { continue; }

                            // Init histogram
                            for (int u = 0; u < MatchingInfo.HistogramLength; u++) { hist[u] = 0f; }

                            double err = 0;
                            int count = 0;
                            // compare target (sub-area) and glyph (full area)
                            for (int glyphY = 0; glyphY < imageToFind.Height; glyphY++)
                            {
                                for (int glyphX = 0; glyphX < imageToFind.Width; glyphX++)
                                {
                                    IColor targetColor = imageToParse[x + glyphX, y + glyphY];
                                    IColor glyphColor = imageToFind[glyphX, glyphY];

                                    // BUGBUG : What if user want to compare for transparent region ?
                                    // Probably need an extra flag.
                                    if (glyphColor.Alpha != 0 && targetColor.Alpha != 0)
                                    {
                                        float deltaColor = (float)(Math.Abs(targetColor.Red - glyphColor.Red) + Math.Abs(targetColor.Green - glyphColor.Green) + Math.Abs(targetColor.Blue - glyphColor.Blue)) / 3f;
                                        err += deltaColor / pixelCount;
                                        if (err > maxErr) { goto ExitInternalLoop; }

                                        AddInc((float)deltaColor, hist);
                                        count++;
                                    }
                                }
                            }

                            if (err <= maxErr)
                            {
                                if (count > 0)
                                {
                                    GlyphComparatorEventArgs glyphComp = new GlyphComparatorEventArgs(new MatchingInfo());
                                    glyphComp.MatchingInfo.Error = (float)err;
                                    glyphComp.MatchingInfo.BoundingBox = new Rectangle(x, y, imageToFind.Width, imageToFind.Height);
                                    // Normalize histogram
                                    for (int i = 0; i < MatchingInfo.HistogramLength; i++)
                                    {
                                        glyphComp.MatchingInfo.Histogram[i] = hist[i] / count;
                                    }
//                                    ProcessMatch(matchingInfo);
                                    if (MatchFoundEvent != null) { MatchFoundEvent(this, glyphComp); }
                                    found = true;
                                }
                            }
                            else
                            {
                                // Log.WriteLine();
                                goto ExitExternalLoop;
                            }
                        ExitInternalLoop:
                            feedbackCount++;
                            if (feedbackCount % 100 == 0)
                            {
                                // Give Feedback
//                                Log.Write(".");
                            }
                        }   // for x
                    }   // for y
                    ExitExternalLoop:
                    if (found == false)
                    {
//                        Log.WriteLine("   < TEMPLATE NOT FOUND during the Scan>");
                    }
                    return found;
                }
            #endregion Private Methods

            #region Public Methods
                /// <summary>
                /// Find the all the GlyphBase specified in the GlyphModel::Glyphs property within the GlyphModel::Target
                /// </summary>
                /// <param name="glyphModel">The GlyphModel to use</param>
                /// <returns>The number of match found (max of one per glyph found)</returns>
                public int FindGlyphs(GlyphModel glyphModel)
                {
                    return FindGlyphs((GlyphBase[])glyphModel.Glyphs.ToArray(typeof(GlyphBase)), glyphModel);
                }
                /// <summary>
                /// Find the all the GlyphBase specified in the Array within the GlyphModel::Target
                /// </summary>
                /// <param name="glyphsToFind">The GlyphBase to find</param>
                /// <param name="glyphModel">The GlyphModel to use</param>
                /// <returns>The number of match found (max of one per glyph found)</returns>
                public int FindGlyphs(GlyphBase[] glyphsToFind, GlyphModel glyphModel)
                {
                    int retVal = 0;
                    _glyphSearched = glyphModel;
                    MatchFoundEvent += new MatchFoundEventHandler(GlyphComparator_MatchFoundInternal);

                    IImageAdapter targetFiltered = ImageUtility.ClipImageAdapter(glyphModel.Target, new Rectangle((int)glyphModel.Panel.Position.X, (int)glyphModel.Panel.Position.Y, (int)glyphModel.Panel.Size.Width, (int)glyphModel.Panel.Size.Height));
//                    IImageAdapter targetFiltered = ImageUtility.ClipImageAdapter(glyphModel.Target, new Rectangle((int)glyphModel.Layout.Position.X, (int)glyphModel.Layout.Position.Y, (int)glyphModel.Layout.Size.Width, (int)glyphModel.Layout.Size.Height));

                    // Filter Image (Only needed if one or more "GlyphBase::EdgesOnly" property is true)
                    SaturateEdgeFilter edgeFilter = new SaturateEdgeFilter();
                    edgeFilter.ColorBelowThresold = (ColorByte)Color.White;
                    edgeFilter.ColorAboveThresold = (ColorByte)Color.Red;
                    edgeFilter.Threshold = 0;
                    if (GlyphUseEdgeOnly(glyphsToFind))
                    {
                        targetFiltered = edgeFilter.Process(targetFiltered);
                    }

                    // Render every glyphs (needed because Glyph overlap would mess up the resulting bmp otherwise)
                    IImageAdapter renderSurface = null;
                    if (glyphModel.GeneratedImage != null)
                    {
                        renderSurface = glyphModel.GeneratedImage;
                    }
                    else
                    {
                        renderSurface = new ImageAdapter(glyphModel.Size.Width, glyphModel.Size.Height, ColorByte.Empty);
                        renderSurface = RenderGlyphs(glyphsToFind, renderSurface);
                    }

                    // Capture images
                    Hashtable glyphBitmapHash = new Hashtable(glyphModel.Glyphs.Count);
                    for (int t = 0; t < glyphModel.Glyphs.Count; t++)
                    {
                        GlyphBase glyph = (GlyphBase)glyphModel.Glyphs[t];

                        Rectangle area = new Rectangle(glyph.ComputedLayout.Position.X, glyph.ComputedLayout.Position.Y, (int)Math.Min(glyph.ComputedLayout.Size.Width, glyphModel.Panel.Size.Width - glyph.ComputedLayout.Position.X + .5), (int)Math.Min(glyph.ComputedLayout.Size.Height, glyphModel.Panel.Size.Height - glyph.ComputedLayout.Position.Y + .5));
                        IImageAdapter clippedImage = ImageUtility.ClipImageAdapter(renderSurface, area);
                        if(glyphBitmapHash.Contains(glyph) == false)
                        {
                            glyphBitmapHash.Add(glyph, clippedImage);
                        }
                        else
                        {
                            // Duplicate, remove it from the collection
                            glyphModel.Glyphs.RemoveAt(t);
                            t--;
                        }
                    }

                    // Try to match ever Glyph in the collection
                    IDictionaryEnumerator iter = glyphBitmapHash.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        GlyphBase glyph = (GlyphBase)iter.Key;
                        _glyphToFind = glyph;
                        IImageAdapter imageAdapter = (ImageAdapter)iter.Value;

                        if (glyph.CompareInfo.EdgesOnly)
                        {
                            imageAdapter = edgeFilter.Process(imageAdapter);
                        }
                        bool found = FindImageAdapter(targetFiltered, imageAdapter, glyph.CompareInfo.ErrorMax);
                        if (found) { retVal++; }
                   }

                    return retVal;
                }
                /// <summary>
                /// Try to find the GlyphBase specified within second GlyphBase
                /// </summary>
                /// <param name="glyphToFind">The GlyphBase to look for</param>
                /// <param name="glyphToParse">The GlyphBase to search</param>
                /// <param name="errorMax">The maximum number of pixel mismatching (normalized percentage)</param>
                /// <returns>true if match, false otherwise</returns>
                public bool FindSingleGlyph(GlyphBase glyphToFind, GlyphBase glyphToParse, double errorMax)
                {
                    _glyphToFind = glyphToFind;
                    _glyphSearched = glyphToParse;
                    MatchFoundEvent += new MatchFoundEventHandler(GlyphComparator_MatchFoundInternal);

                    IImageAdapter imageToParse = glyphToParse.Render();
                    IImageAdapter imageToFind = glyphToFind.Render();

                    return FindImageAdapter(imageToParse, imageToFind, errorMax);
                }
            #endregion Public Methods
        #endregion Methods
    }
}
