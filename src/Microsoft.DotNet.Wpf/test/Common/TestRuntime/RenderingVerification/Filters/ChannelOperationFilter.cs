// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        


namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
    #endregion using

    /// <summary>
    /// Summary description for ChannelOperation.
    /// </summary>
    public class ChannelOperationFilter : Filter
    {
        #region Constants
            private const string SPLITCHANNEL = "SplitChannel";
            private const string JOINCHANNEL = "JoinChannel";
            private const string ADDCHANNEL = "AddChannel";
            private const string SUBTRACTCHANNEL = "SubtractChannel";
            private const string MULTIPLYCHANNEL = "MultiplyChannel";
            private const string DIVIDECHANNEL = "DivideChannel";

            private const string ALPHACHANNEL = "AlphaChannel";
            private const string REDCHANNEL = "RedChannel";
            private const string GREENCHANNEL = "GreenChannel";
            private const string BLUECHANNEL = "BlueChannel";
            private const string REINJECTLAPHA = "ReinjectAlpha";
        #endregion Constants

        #region Delegates
            private delegate double channelOperationHandler(int x, int y, ref IImageAdapter[] images, int IPUtil_Channel);
        #endregion Delegates

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The split operation (ARGB -> A + R + G + B) 
                /// </summary>
                public bool SplitChannel 
                {
                    get
                    {
                        return (bool)this[SPLITCHANNEL].Parameter;
                    }
                    set 
                    {
                        this[SPLITCHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// The join (= merge) operation (A + R + G + B -> ARGB) 
                /// </summary>
                /// <value></value>
                public bool JoinChannel
                {
                    get 
                    {
                        return (bool)this[JOINCHANNEL].Parameter;
                    }
                    set 
                    {
                        this[JOINCHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// Add a channel to the image (uses AlphaChannel / RedChannel / GreenChannel and BlueChannel properties)
                /// </summary>
                /// <value></value>
                public bool AddChannel
                {
                    get 
                    {
                        return (bool)this[ADDCHANNEL].Parameter;
                    }
                    set 
                    {
                        this[ADDCHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// Remove the value from the source
                /// </summary>
                /// <value></value>
                public bool SubtractChannel
                {
                    get 
                    {
                        return (bool)this[SUBTRACTCHANNEL].Parameter;
                    }
                    set 
                    {
                        this[SUBTRACTCHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// Multiply all channels from the source image by the image(s) defined in the parameter channel(s) (alpha red green blue).
                /// </summary>
                public bool MultiplyChannel
                {
                    get 
                    {
                        return (bool)this[MULTIPLYCHANNEL].Parameter;
                    }
                    set 
                    {
                        this[MULTIPLYCHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// Divide all channels from the source image by the image(s) defined in the parameter channel(s) (alpha red green blue).
                /// Note : if the dividing channel is zero (DivideByZeroException), the source channel is unmodified, no exception is thrown.
                /// </summary>
                public bool DivideChannel
                {
                    get 
                    {
                        return (bool)this[DIVIDECHANNEL].Parameter;
                    }
                    set 
                    {
                        this[DIVIDECHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// (Used when splitting channels) Insert the alpha channel value in every image so user can view the image.
                /// </summary>
                public bool ReinjectAlphaInAllChannels
                {
                    get 
                    {
                        return (bool)this[REINJECTLAPHA].Parameter;
                    }
                    set 
                    {
                        this[REINJECTLAPHA].Parameter = value;
                    }
                }
                /// <summary>
                /// The IImageAdapter containing the Alpha channel
                /// </summary>
                /// <value></value>
                public IImageAdapter AlphaChannel
                {
                    get 
                    {
                        return (IImageAdapter)this[ALPHACHANNEL].Parameter;
                    }
                    set 
                    {
                        this[ALPHACHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// The IImageAdapter containing the Red channel
                /// </summary>
                /// <value></value>
                public IImageAdapter RedChannel
                {
                    get 
                    {
                        return (IImageAdapter)this[REDCHANNEL].Parameter;
                    }
                    set 
                    {
                        this[REDCHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// The IImageAdapter containing the Green channel
                /// </summary>
                /// <value></value>
                public IImageAdapter GreenChannel
                {
                    get 
                    {
                        return (IImageAdapter)this[GREENCHANNEL].Parameter;
                    }
                    set 
                    {
                        this[GREENCHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// The IImageAdapter containing the Blue channel
                /// </summary>
                /// <value></value>
                public IImageAdapter BlueChannel
                {
                    get 
                    {
                        return (IImageAdapter)this[BLUECHANNEL].Parameter;
                    }
                    set 
                    {
                        this[BLUECHANNEL].Parameter = value;
                    }
                }
                /// <summary>
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "Perform operation on Channels (add / sub / mul / div / split / ...)";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create an instance of the ChannelOperationFilter class
            /// </summary>
            public ChannelOperationFilter()
            {
                FilterParameter splitChannels = new FilterParameter(SPLITCHANNEL, "Split the image into 4 IImageAdapter (one per channel)", true);
                FilterParameter reinjectAlphaInAllChannels = new FilterParameter(REINJECTLAPHA, "Set the alpha value of every channel to the original alpha for easy viewing", true);

                FilterParameter joinChannels = new FilterParameter(JOINCHANNEL, "Merge the IImageAdapter into 1 ", false);
                FilterParameter addChannels = new FilterParameter(ADDCHANNEL, "Add the channel value of the image to the IImageAdapter", false);
                FilterParameter subtractChannels = new FilterParameter(SUBTRACTCHANNEL, "Substract the channel value of the image from the IImageAdapter (color starvation is likely to occur)", false);
                FilterParameter multiplyChannels = new FilterParameter(MULTIPLYCHANNEL, "Multiply the channel value of the image to the IImageAdapter", false);
                FilterParameter divideChannels = new FilterParameter(DIVIDECHANNEL, "Divide the channel value of the image from the IImageAdapter", false);


                FilterParameter alphaChannel = new FilterParameter(ALPHACHANNEL, "The Alpha composant of the image to be added to IImageAdapter", new ImageAdapter(0, 0));
                FilterParameter redChannel = new FilterParameter(REDCHANNEL, "The Red composant of the image to be added to IImageAdapter", new ImageAdapter(0, 0));
                FilterParameter greenChannel = new FilterParameter(GREENCHANNEL, "The Green composant of the image to be added to IImageAdapter", new ImageAdapter(0, 0));
                FilterParameter blueChannel = new FilterParameter(BLUECHANNEL, "The Blue composant of the image to be added to IImageAdapter", new ImageAdapter(0, 0));


                AddParameter(splitChannels);
                AddParameter(joinChannels);
                AddParameter(addChannels);
                AddParameter(subtractChannels);
                AddParameter(multiplyChannels);
                AddParameter(divideChannels);
                AddParameter(alphaChannel);
                AddParameter(redChannel);
                AddParameter(greenChannel);
                AddParameter(blueChannel);
                AddParameter(reinjectAlphaInAllChannels);

            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private double AddOperation(int x, int y, ref IImageAdapter[] channels, int IPUtil_Channel)
                {       
                    double retVal = 0.0;
                    foreach (IImageAdapter channel in channels)
                    {                       
                        if (IPUtil_Channel == IPUtil.ALPHA)
                        {
                            retVal += channel[x, y].Alpha;
                        }
                        if (IPUtil_Channel == IPUtil.RED)
                        {
                            retVal += channel[x, y].Red;
                        }
                        if (IPUtil_Channel == IPUtil.GREEN)
                        {
                            retVal += channel[x, y].Green;
                        }
                        if (IPUtil_Channel == IPUtil.BLUE)
                        {
                            retVal += channel[x, y].Blue;
                        }
                    }
                    return retVal;
                }
                private double SubtractOperation(int x, int y, ref IImageAdapter[] channels, int IPUtil_Channel)
                {
                    double retVal = 0.0;
                    if (channels.Length > 0)
                    {
                        foreach (IImageAdapter channel in channels)
                        {
                            if (IPUtil_Channel == IPUtil.ALPHA)
                            {
                                retVal -= channel[x, y].Alpha;
                            }
                            if (IPUtil_Channel == IPUtil.RED)
                            {
                                retVal -= channel[x, y].Red;
                            }
                            if (IPUtil_Channel == IPUtil.GREEN)
                            {
                                retVal -= channel[x, y].Green;
                            }
                            if (IPUtil_Channel == IPUtil.BLUE)
                            {
                                retVal -= channel[x, y].Blue;
                            }
                        }

                        retVal /= channels[0][x, y].NormalizedValue;
                    }

                    return retVal;
                }
                private double MultiplyOperation(int x, int y, ref IImageAdapter[] channels, int IPUtil_Channel)
                {
                    double retVal = 1.0;
                    IColor color;

                    if (channels.Length > 0)
                    {
                        foreach (IImageAdapter channel in channels)
                        {
                            color = channel[x, y];
                            if (IPUtil_Channel == IPUtil.ALPHA)
                            {
                                retVal *= color.A;
                            }
                            if (IPUtil_Channel == IPUtil.RED)
                            {
                                retVal *= color.R;
                            }
                            if (IPUtil_Channel == IPUtil.GREEN)
                            {
                                retVal *= color.G;
                            }
                            if (IPUtil_Channel == IPUtil.BLUE)
                            {
                                retVal *= color.B;
                            }
                        }
                        retVal /= channels[0][x, y].NormalizedValue;
                    }

                    return retVal;
                }
                private double DivideOperation(int x, int y, ref IImageAdapter[] channels, int IPUtil_Channel)
                {
                    double retVal = 1.0;
                    Color color = Color.Empty;

                    foreach (IImageAdapter channel in channels)
                    {
                        color = channel[x, y].ToColor();
                        if (IPUtil_Channel == IPUtil.ALPHA)
                        {
                            if (color.A != 0)
                            {
                                retVal /= color.A;
                            }
                        }
                        if (IPUtil_Channel == IPUtil.RED)
                        {
                            if (color.R != 0)
                            {
                                retVal /= color.R;
                            }
                        }
                        if (IPUtil_Channel == IPUtil.GREEN)
                        {
                            if (color.G != 0)
                            {
                                retVal /= color.G;
                            }
                        }
                        if (IPUtil_Channel == IPUtil.BLUE)
                        {
                            if (color.B != 0)
                            {
                                retVal /= color.B;
                            }
                        }
                    }
                    return retVal;
                }
            #endregion Private Methods
            #region Protected Methods
                /// <summary>
                /// Perform the operation
                /// </summary>
                /// <param name="source">The image to use for the operation</param>
                /// <returns>The Filtered image</returns>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    IImageAdapter retVal = source;
                    if (SplitChannel)
                    {
                        ImageAdapter alphaChannel = new ImageAdapter(source.Width, source.Height);
                        ImageAdapter redChannel = new ImageAdapter(source.Width, source.Height);
                        ImageAdapter greenChannel = new ImageAdapter(source.Width, source.Height);
                        ImageAdapter blueChannel = new ImageAdapter(source.Width, source.Height);

                        for (int y = 0; y < source.Height; y++)
                        {
                            for (int x = 0; x < source.Width; x++)
                            {
                                double alpha = source[x, y].Alpha;
                                alphaChannel[x, y] = (IColor)source[x, y].Clone();
                                alphaChannel[x, y].RGB = 0;
                                if (ReinjectAlphaInAllChannels)
                                {
                                    redChannel[x, y] = (IColor)source[x, y].Clone(); redChannel[x, y].RGB = source[x, y].R << 16;
                                    greenChannel[x, y] = (IColor)source[x, y].Clone(); greenChannel[x, y].RGB = source[x, y].G << 8;
                                    blueChannel[x, y] = (IColor)source[x, y].Clone(); blueChannel[x, y].RGB = source[x, y].B;
                                }
                                else
                                {
                                    redChannel[x, y] = (IColor)source[x, y].Clone(); redChannel[x, y].ARGB = source[x, y].R << 16;
                                    greenChannel[x, y] = (IColor)source[x, y].Clone(); greenChannel[x, y].ARGB = source[x, y].G << 8;
                                    blueChannel[x, y] = (IColor)source[x, y].Clone(); blueChannel[x, y].ARGB = source[x, y].B;
                                }
                            }
                        }

                        AlphaChannel = alphaChannel;
                        RedChannel = redChannel;
                        GreenChannel = greenChannel;
                        BlueChannel = blueChannel;
                    }
                    else
                    {
                        if (!JoinChannel && !AddChannel && !MultiplyChannel && !DivideChannel)
                        {
                            throw new RenderingVerificationException("Nothing selected !");
                        }

                        ArrayList tempChannels = new ArrayList(4);
                        if (AlphaChannel.Width != 0) { tempChannels.Add(AlphaChannel); }
                        if (RedChannel.Width != 0) { tempChannels.Add(RedChannel); }
                        if (GreenChannel.Width != 0) { tempChannels.Add(GreenChannel); }
                        if (BlueChannel.Width != 0) { tempChannels.Add(BlueChannel); }
                        if (source.Width != 0) { tempChannels.Add(source); }
                        IImageAdapter[] channels = new IImageAdapter[tempChannels.Count];
                        tempChannels.CopyTo(channels);

                        retVal = new ImageAdapter(source.Width, source.Height);
                        IColor colorEmpty = (IColor)source[0, 0].Clone(); colorEmpty.IsEmpty = true;

                        channelOperationHandler channelOperation = null;
                        if (AddChannel || JoinChannel) { channelOperation = new channelOperationHandler(AddOperation); }
                        if (SubtractChannel) { channelOperation = new channelOperationHandler(SubtractOperation); }
                        if (MultiplyChannel) { channelOperation = new channelOperationHandler(MultiplyOperation); }
                        if (DivideChannel) { channelOperation = new channelOperationHandler(DivideOperation); }

                        IColor color = null;
                        for (int y = 0; y < retVal.Height; y++)
                        {
                            for (int x = 0; x < retVal.Width; x++)
                            {
                                color = (IColor)colorEmpty.Clone();
                                color.Alpha = channelOperation(x, y, ref channels, IPUtil.ALPHA);
                                color.Red = channelOperation(x, y, ref channels, IPUtil.RED);
                                color.Green = channelOperation(x, y, ref channels, IPUtil.GREEN);
                                color.Blue = channelOperation(x, y, ref channels, IPUtil.BLUE);
                                retVal[x, y] = color;
                            }
                        }
                    }
                    // TODO : SUB, MUL and DIV

                    return retVal;
                }
            #endregion Protected Methods
        #endregion Methods

    }
}
