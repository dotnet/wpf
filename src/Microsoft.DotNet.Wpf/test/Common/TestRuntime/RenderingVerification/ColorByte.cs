// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region usings
        using System;
        using System.Drawing;
    #endregion usings

    /// <summary>
    /// ColorDouble: Fast and small memory usage but no scRGB support (information migh be lost during filtering / type conversion)
    /// </summary>
    [SerializableAttribute()]
#if CLR_VERSION_BELOW_2
    public struct ColorByte : IColor
#else
    public partial struct ColorByte: IColor
#endif
    {
        #region Properties
            /// <summary>
            /// Defines an empty color for this type
            /// </summary>
            public static readonly ColorByte Empty = new ColorByte();
            internal static double _maxChannelValue = 1.0;
            internal static double _minChannelValue = 0.0;
            internal static double _normalizedValue = 255.0;
            private bool _isDefined; 
            private bool _isScRgb;
            private byte _a;
            private byte _r;
            private byte _g;
            private byte _b;

            /// <summary>
            /// Lookup table for Pseudo color.
            /// </summary>
            // NOTE : csc.exe in CLR 1.1 is limited to 2046 characters per line
            private static IColor[] _colorLUT = {
                new ColorDouble(1, 0, 0, 0), new ColorDouble(1, 0, 0.0065359477124183, 0.403921568627451), new ColorDouble(1, 0, 0.0130718954248366, 0.407843137254902), new ColorDouble(1, 0, 0.0196078431372549, 0.411764705882353), new ColorDouble(1, 0, 0.0261437908496732, 0.415686274509804), new ColorDouble(1, 0, 0.0326797385620915, 0.419607843137255), new ColorDouble(1, 0, 0.0392156862745098, 0.423529411764706), new ColorDouble(1, 0, 0.0457516339869281, 0.427450980392157), new ColorDouble(1, 0, 0.0522875816993464, 0.431372549019608), new ColorDouble(1, 0, 0.0588235294117647, 0.435294117647059), new ColorDouble(1, 0, 0.065359477124183, 0.43921568627451), new ColorDouble(1, 0, 0.0718954248366013, 0.443137254901961), new ColorDouble(1, 0, 0.0784313725490196, 0.447058823529412), new ColorDouble(1, 0, 0.0849673202614379, 0.450980392156863), new ColorDouble(1, 0, 0.0915032679738562, 0.454901960784314), new ColorDouble(1, 0, 0.0980392156862745, 0.458823529411765), new ColorDouble(1, 0, 0.104575163398693, 0.462745098039216), new ColorDouble(1, 0, 0.111111111111111, 0.466666666666667), new ColorDouble(1, 0, 0.117647058823529, 0.470588235294118), new ColorDouble(1, 0, 0.124183006535948, 0.474509803921569), new ColorDouble(1, 0, 0.130718954248366, 0.47843137254902), new ColorDouble(1, 0, 0.137254901960784, 0.482352941176471), new ColorDouble(1, 0, 0.143790849673203, 0.486274509803922), new ColorDouble(1, 0, 0.150326797385621, 0.490196078431373), new ColorDouble(1, 0, 0.156862745098039, 0.494117647058824), new ColorDouble(1, 0, 0.163398692810458, 0.498039215686275), new ColorDouble(1, 0, 0.169934640522876, 0.501960784313725), new ColorDouble(1, 0, 0.176470588235294, 0.505882352941176), new ColorDouble(1, 0, 0.183006535947712, 0.509803921568627), new ColorDouble(1, 0, 0.189542483660131, 0.513725490196078), new ColorDouble(1, 0, 0.196078431372549, 0.517647058823529), 
                new ColorDouble(1, 0, 0.202614379084967, 0.52156862745098), new ColorDouble(1, 0, 0.209150326797386, 0.525490196078431), new ColorDouble(1, 0, 0.215686274509804, 0.529411764705882), new ColorDouble(1, 0, 0.222222222222222, 0.533333333333333), new ColorDouble(1, 0, 0.228758169934641, 0.537254901960784), new ColorDouble(1, 0, 0.235294117647059, 0.541176470588235), new ColorDouble(1, 0, 0.241830065359477, 0.545098039215686), new ColorDouble(1, 0, 0.248366013071895, 0.549019607843137), new ColorDouble(1, 0, 0.254901960784314, 0.552941176470588), new ColorDouble(1, 0, 0.261437908496732, 0.556862745098039), new ColorDouble(1, 0, 0.26797385620915, 0.56078431372549), new ColorDouble(1, 0, 0.274509803921569, 0.564705882352941), new ColorDouble(1, 0, 0.281045751633987, 0.568627450980392), new ColorDouble(1, 0, 0.287581699346405, 0.572549019607843), new ColorDouble(1, 0, 0.294117647058824, 0.576470588235294), new ColorDouble(1, 0, 0.300653594771242, 0.580392156862745), new ColorDouble(1, 0, 0.30718954248366, 0.584313725490196), new ColorDouble(1, 0, 0.313725490196078, 0.588235294117647), new ColorDouble(1, 0, 0.320261437908497, 0.592156862745098), new ColorDouble(1, 0, 0.326797385620915, 0.596078431372549), new ColorDouble(1, 0, 0.333333333333333, 0.6), new ColorDouble(1, 0, 0.339869281045752, 0.603921568627451), new ColorDouble(1, 0, 0.34640522875817, 0.607843137254902), new ColorDouble(1, 0, 0.352941176470588, 0.611764705882353), new ColorDouble(1, 0, 0.359477124183007, 0.615686274509804), new ColorDouble(1, 0, 0.366013071895425, 0.619607843137255), new ColorDouble(1, 0, 0.372549019607843, 0.623529411764706), new ColorDouble(1, 0, 0.379084967320261, 0.627450980392157), new ColorDouble(1, 0, 0.38562091503268, 0.631372549019608), new ColorDouble(1, 0, 0.392156862745098, 0.635294117647059), new ColorDouble(1, 0, 0.398692810457516, 0.63921568627451), 
                new ColorDouble(1, 0, 0.405228758169935, 0.643137254901961), new ColorDouble(1, 0, 0.411764705882353, 0.647058823529412), new ColorDouble(1, 0, 0.418300653594771, 0.650980392156863), new ColorDouble(1, 0, 0.42483660130719, 0.654901960784314), new ColorDouble(1, 0, 0.431372549019608, 0.658823529411765), new ColorDouble(1, 0, 0.437908496732026, 0.662745098039216), new ColorDouble(1, 0, 0.444444444444444, 0.666666666666667), new ColorDouble(1, 0, 0.450980392156863, 0.670588235294118), new ColorDouble(1, 0, 0.457516339869281, 0.674509803921569), new ColorDouble(1, 0, 0.464052287581699, 0.67843137254902), new ColorDouble(1, 0, 0.470588235294118, 0.682352941176471), new ColorDouble(1, 0, 0.477124183006536, 0.686274509803922), new ColorDouble(1, 0, 0.483660130718954, 0.690196078431373), new ColorDouble(1, 0, 0.490196078431373, 0.694117647058824), new ColorDouble(1, 0, 0.496732026143791, 0.698039215686274), new ColorDouble(1, 0, 0.503267973856209, 0.701960784313725), new ColorDouble(1, 0, 0.509803921568627, 0.705882352941176), new ColorDouble(1, 0, 0.516339869281046, 0.709803921568627), new ColorDouble(1, 0, 0.522875816993464, 0.713725490196078), new ColorDouble(1, 0, 0.529411764705882, 0.717647058823529), new ColorDouble(1, 0, 0.535947712418301, 0.72156862745098), new ColorDouble(1, 0, 0.542483660130719, 0.725490196078431), new ColorDouble(1, 0, 0.549019607843137, 0.729411764705882), new ColorDouble(1, 0, 0.555555555555556, 0.733333333333333), new ColorDouble(1, 0, 0.562091503267974, 0.737254901960784), new ColorDouble(1, 0, 0.568627450980392, 0.741176470588235), new ColorDouble(1, 0, 0.57516339869281, 0.745098039215686), new ColorDouble(1, 0, 0.581699346405229, 0.749019607843137), new ColorDouble(1, 0, 0.588235294117647, 0.752941176470588), new ColorDouble(1, 0, 0.594771241830065, 0.756862745098039), new ColorDouble(1, 0, 0.601307189542484, 0.76078431372549), 
                new ColorDouble(1, 0, 0.607843137254902, 0.764705882352941), new ColorDouble(1, 0, 0.61437908496732, 0.768627450980392), new ColorDouble(1, 0, 0.620915032679739, 0.772549019607843), new ColorDouble(1, 0, 0.627450980392157, 0.776470588235294), new ColorDouble(1, 0, 0.633986928104575, 0.780392156862745), new ColorDouble(1, 0, 0.640522875816993, 0.784313725490196), new ColorDouble(1, 0, 0.647058823529412, 0.788235294117647), new ColorDouble(1, 0, 0.65359477124183, 0.792156862745098), new ColorDouble(1, 0, 0.660130718954248, 0.796078431372549), new ColorDouble(1, 0, 0.666666666666667, 0.8), new ColorDouble(1, 0, 0.673202614379085, 0.803921568627451), new ColorDouble(1, 0, 0.679738562091503, 0.807843137254902), new ColorDouble(1, 0, 0.686274509803922, 0.811764705882353), new ColorDouble(1, 0, 0.69281045751634, 0.815686274509804), new ColorDouble(1, 0, 0.699346405228758, 0.819607843137255), new ColorDouble(1, 0, 0.705882352941177, 0.823529411764706), new ColorDouble(1, 0, 0.712418300653595, 0.827450980392157), new ColorDouble(1, 0, 0.718954248366013, 0.831372549019608), new ColorDouble(1, 0, 0.725490196078431, 0.835294117647059), new ColorDouble(1, 0, 0.73202614379085, 0.83921568627451), new ColorDouble(1, 0, 0.738562091503268, 0.843137254901961), new ColorDouble(1, 0, 0.745098039215686, 0.847058823529412), new ColorDouble(1, 0, 0.751633986928105, 0.850980392156863), new ColorDouble(1, 0, 0.758169934640523, 0.854901960784314), new ColorDouble(1, 0, 0.764705882352941, 0.858823529411765), new ColorDouble(1, 0, 0.77124183006536, 0.862745098039216), new ColorDouble(1, 0, 0.777777777777778, 0.866666666666667), new ColorDouble(1, 0, 0.784313725490196, 0.870588235294118), new ColorDouble(1, 0, 0.790849673202614, 0.874509803921569), new ColorDouble(1, 0, 0.797385620915033, 0.87843137254902), new ColorDouble(1, 0, 0.803921568627451, 0.882352941176471), 
                new ColorDouble(1, 0, 0.810457516339869, 0.886274509803922), new ColorDouble(1, 0, 0.816993464052288, 0.890196078431373), new ColorDouble(1, 0, 0.823529411764706, 0.894117647058823), new ColorDouble(1, 0, 0.830065359477124, 0.898039215686275), new ColorDouble(1, 0, 0.836601307189543, 0.901960784313726), new ColorDouble(1, 0, 0.843137254901961, 0.905882352941176), new ColorDouble(1, 0, 0.849673202614379, 0.909803921568627), new ColorDouble(1, 0, 0.856209150326797, 0.913725490196078), new ColorDouble(1, 0, 0.862745098039216, 0.917647058823529), new ColorDouble(1, 0, 0.869281045751634, 0.92156862745098), new ColorDouble(1, 0, 0.875816993464052, 0.925490196078431), new ColorDouble(1, 0, 0.882352941176471, 0.929411764705882), new ColorDouble(1, 0, 0.888888888888889, 0.933333333333333), new ColorDouble(1, 0, 0.895424836601307, 0.937254901960784), new ColorDouble(1, 0, 0.901960784313726, 0.941176470588235), new ColorDouble(1, 0, 0.908496732026144, 0.945098039215686), new ColorDouble(1, 0, 0.915032679738562, 0.949019607843137), new ColorDouble(1, 0, 0.92156862745098, 0.952941176470588), new ColorDouble(1, 0, 0.928104575163399, 0.956862745098039), new ColorDouble(1, 0, 0.934640522875817, 0.96078431372549), new ColorDouble(1, 0, 0.941176470588235, 0.964705882352941), new ColorDouble(1, 0, 0.947712418300654, 0.968627450980392), new ColorDouble(1, 0, 0.954248366013072, 0.972549019607843), new ColorDouble(1, 0, 0.96078431372549, 0.976470588235294), new ColorDouble(1, 0, 0.967320261437909, 0.980392156862745), new ColorDouble(1, 0, 0.973856209150327, 0.984313725490196), new ColorDouble(1, 0, 0.980392156862745, 0.988235294117647), new ColorDouble(1, 0, 0.986928104575163, 0.992156862745098), new ColorDouble(1, 0, 0.993464052287582, 0.996078431372549), new ColorDouble(1, 0, 1, 1), new ColorDouble(1, 0, 1, 0.980392156862745), new ColorDouble(1, 0, 1, 0.96078431372549), 
                new ColorDouble(1, 0, 1, 0.941176470588235), new ColorDouble(1, 0, 1, 0.92156862745098), new ColorDouble(1, 0, 1, 0.901960784313726), new ColorDouble(1, 0, 1, 0.882352941176471), new ColorDouble(1, 0, 1, 0.862745098039216), new ColorDouble(1, 0, 1, 0.843137254901961), new ColorDouble(1, 0, 1, 0.823529411764706), new ColorDouble(1, 0, 1, 0.803921568627451), new ColorDouble(1, 0, 1, 0.784313725490196), new ColorDouble(1, 0, 1, 0.764705882352941), new ColorDouble(1, 0, 1, 0.745098039215686), new ColorDouble(1, 0, 1, 0.725490196078431), new ColorDouble(1, 0, 1, 0.705882352941176), new ColorDouble(1, 0, 1, 0.686274509803922), new ColorDouble(1, 0, 1, 0.666666666666667), new ColorDouble(1, 0, 1, 0.647058823529412), new ColorDouble(1, 0, 1, 0.627450980392157), new ColorDouble(1, 0, 1, 0.607843137254902), new ColorDouble(1, 0, 1, 0.588235294117647), new ColorDouble(1, 0, 1, 0.568627450980392), new ColorDouble(1, 0, 1, 0.549019607843137), new ColorDouble(1, 0, 1, 0.529411764705882), new ColorDouble(1, 0, 1, 0.509803921568627), new ColorDouble(1, 0, 1, 0.490196078431373), new ColorDouble(1, 0, 1, 0.470588235294118), new ColorDouble(1, 0, 1, 0.450980392156863), new ColorDouble(1, 0, 1, 0.431372549019608), new ColorDouble(1, 0, 1, 0.411764705882353), new ColorDouble(1, 0, 1, 0.392156862745098), new ColorDouble(1, 0, 1, 0.372549019607843), new ColorDouble(1, 0, 1, 0.352941176470588), new ColorDouble(1, 0, 1, 0.333333333333333), new ColorDouble(1, 0, 1, 0.313725490196078), new ColorDouble(1, 0, 1, 0.294117647058823), new ColorDouble(1, 0, 1, 0.274509803921569), new ColorDouble(1, 0, 1, 0.254901960784314), new ColorDouble(1, 0, 1, 0.235294117647059), new ColorDouble(1, 0, 1, 0.215686274509804), new ColorDouble(1, 0, 1, 0.196078431372549), new ColorDouble(1, 0, 1, 0.176470588235294), new ColorDouble(1, 0, 1, 0.156862745098039), 
                new ColorDouble(1, 0, 1, 0.137254901960784), new ColorDouble(1, 0, 1, 0.117647058823529), new ColorDouble(1, 0, 1, 0.0980392156862745), new ColorDouble(1, 0, 1, 0.0784313725490197), new ColorDouble(1, 0, 1, 0.0588235294117647), new ColorDouble(1, 0, 1, 0.0392156862745098), new ColorDouble(1, 0, 1, 0.0196078431372549), new ColorDouble(1, 0, 1, 0), new ColorDouble(1, 0.00980392156862745, 1, 0), new ColorDouble(1, 0.0196078431372549, 1, 0), new ColorDouble(1, 0.0294117647058824, 1, 0), new ColorDouble(1, 0.0392156862745098, 1, 0), new ColorDouble(1, 0.0490196078431373, 1, 0), new ColorDouble(1, 0.0588235294117647, 1, 0), new ColorDouble(1, 0.0686274509803922, 1, 0), new ColorDouble(1, 0.0784313725490196, 1, 0), new ColorDouble(1, 0.0882352941176471, 1, 0), new ColorDouble(1, 0.0980392156862745, 1, 0), new ColorDouble(1, 0.107843137254902, 1, 0), new ColorDouble(1, 0.117647058823529, 1, 0), new ColorDouble(1, 0.127450980392157, 1, 0), new ColorDouble(1, 0.137254901960784, 1, 0), new ColorDouble(1, 0.147058823529412, 1, 0), new ColorDouble(1, 0.156862745098039, 1, 0), new ColorDouble(1, 0.166666666666667, 1, 0), new ColorDouble(1, 0.176470588235294, 1, 0), new ColorDouble(1, 0.186274509803922, 1, 0), new ColorDouble(1, 0.196078431372549, 1, 0), new ColorDouble(1, 0.205882352941176, 1, 0), new ColorDouble(1, 0.215686274509804, 1, 0), new ColorDouble(1, 0.225490196078431, 1, 0), new ColorDouble(1, 0.235294117647059, 1, 0), new ColorDouble(1, 0.245098039215686, 1, 0), new ColorDouble(1, 0.254901960784314, 1, 0), new ColorDouble(1, 0.264705882352941, 1, 0), new ColorDouble(1, 0.274509803921569, 1, 0), new ColorDouble(1, 0.284313725490196, 1, 0), new ColorDouble(1, 0.294117647058824, 1, 0), new ColorDouble(1, 0.303921568627451, 1, 0), new ColorDouble(1, 0.313725490196078, 1, 0), new ColorDouble(1, 0.323529411764706, 1, 0), 
                new ColorDouble(1, 0.333333333333333, 1, 0), new ColorDouble(1, 0.343137254901961, 1, 0), new ColorDouble(1, 0.352941176470588, 1, 0), new ColorDouble(1, 0.362745098039216, 1, 0), new ColorDouble(1, 0.372549019607843, 1, 0), new ColorDouble(1, 0.382352941176471, 1, 0), new ColorDouble(1, 0.392156862745098, 1, 0), new ColorDouble(1, 0.401960784313726, 1, 0), new ColorDouble(1, 0.411764705882353, 1, 0), new ColorDouble(1, 0.42156862745098, 1, 0), new ColorDouble(1, 0.431372549019608, 1, 0), new ColorDouble(1, 0.441176470588235, 1, 0), new ColorDouble(1, 0.450980392156863, 1, 0), new ColorDouble(1, 0.46078431372549, 1, 0), new ColorDouble(1, 0.470588235294118, 1, 0), new ColorDouble(1, 0.480392156862745, 1, 0), new ColorDouble(1, 0.490196078431373, 1, 0), new ColorDouble(1, 0.5, 1, 0), new ColorDouble(1, 0.509803921568627, 1, 0), new ColorDouble(1, 0.519607843137255, 1, 0), new ColorDouble(1, 0.529411764705882, 1, 0), new ColorDouble(1, 0.53921568627451, 1, 0), new ColorDouble(1, 0.549019607843137, 1, 0), new ColorDouble(1, 0.558823529411765, 1, 0), new ColorDouble(1, 0.568627450980392, 1, 0), new ColorDouble(1, 0.57843137254902, 1, 0), new ColorDouble(1, 0.588235294117647, 1, 0), new ColorDouble(1, 0.598039215686274, 1, 0), new ColorDouble(1, 0.607843137254902, 1, 0), new ColorDouble(1, 0.617647058823529, 1, 0), new ColorDouble(1, 0.627450980392157, 1, 0), new ColorDouble(1, 0.637254901960784, 1, 0), new ColorDouble(1, 0.647058823529412, 1, 0), new ColorDouble(1, 0.656862745098039, 1, 0), new ColorDouble(1, 0.666666666666667, 1, 0), new ColorDouble(1, 0.676470588235294, 1, 0), new ColorDouble(1, 0.686274509803922, 1, 0), new ColorDouble(1, 0.696078431372549, 1, 0), new ColorDouble(1, 0.705882352941177, 1, 0), new ColorDouble(1, 0.715686274509804, 1, 0), new ColorDouble(1, 0.725490196078431, 1, 0), 
                new ColorDouble(1, 0.735294117647059, 1, 0), new ColorDouble(1, 0.745098039215686, 1, 0), new ColorDouble(1, 0.754901960784314, 1, 0), new ColorDouble(1, 0.764705882352941, 1, 0), new ColorDouble(1, 0.774509803921569, 1, 0), new ColorDouble(1, 0.784313725490196, 1, 0), new ColorDouble(1, 0.794117647058823, 1, 0), new ColorDouble(1, 0.803921568627451, 1, 0), new ColorDouble(1, 0.813725490196078, 1, 0), new ColorDouble(1, 0.823529411764706, 1, 0), new ColorDouble(1, 0.833333333333333, 1, 0), new ColorDouble(1, 0.843137254901961, 1, 0), new ColorDouble(1, 0.852941176470588, 1, 0), new ColorDouble(1, 0.862745098039216, 1, 0), new ColorDouble(1, 0.872549019607843, 1, 0), new ColorDouble(1, 0.882352941176471, 1, 0), new ColorDouble(1, 0.892156862745098, 1, 0), new ColorDouble(1, 0.901960784313726, 1, 0), new ColorDouble(1, 0.911764705882353, 1, 0), new ColorDouble(1, 0.92156862745098, 1, 0), new ColorDouble(1, 0.931372549019608, 1, 0), new ColorDouble(1, 0.941176470588235, 1, 0), new ColorDouble(1, 0.950980392156863, 1, 0), new ColorDouble(1, 0.96078431372549, 1, 0), new ColorDouble(1, 0.970588235294118, 1, 0), new ColorDouble(1, 0.980392156862745, 1, 0), new ColorDouble(1, 0.990196078431373, 1, 0), new ColorDouble(1, 1, 1, 0), new ColorDouble(1, 1, 0.995098039215686, 0), new ColorDouble(1, 1, 0.990196078431372, 0), new ColorDouble(1, 1, 0.985294117647059, 0), new ColorDouble(1, 1, 0.980392156862745, 0), new ColorDouble(1, 1, 0.975490196078431, 0), new ColorDouble(1, 1, 0.970588235294118, 0), new ColorDouble(1, 1, 0.965686274509804, 0), new ColorDouble(1, 1, 0.96078431372549, 0), new ColorDouble(1, 1, 0.955882352941176, 0), new ColorDouble(1, 1, 0.950980392156863, 0), new ColorDouble(1, 1, 0.946078431372549, 0), new ColorDouble(1, 1, 0.941176470588235, 0), new ColorDouble(1, 1, 0.936274509803922, 0), 
                new ColorDouble(1, 1, 0.931372549019608, 0), new ColorDouble(1, 1, 0.926470588235294, 0), new ColorDouble(1, 1, 0.92156862745098, 0), new ColorDouble(1, 1, 0.916666666666667, 0), new ColorDouble(1, 1, 0.911764705882353, 0), new ColorDouble(1, 1, 0.906862745098039, 0), new ColorDouble(1, 1, 0.901960784313726, 0), new ColorDouble(1, 1, 0.897058823529412, 0), new ColorDouble(1, 1, 0.892156862745098, 0), new ColorDouble(1, 1, 0.887254901960784, 0), new ColorDouble(1, 1, 0.882352941176471, 0), new ColorDouble(1, 1, 0.877450980392157, 0), new ColorDouble(1, 1, 0.872549019607843, 0), new ColorDouble(1, 1, 0.867647058823529, 0), new ColorDouble(1, 1, 0.862745098039216, 0), new ColorDouble(1, 1, 0.857843137254902, 0), new ColorDouble(1, 1, 0.852941176470588, 0), new ColorDouble(1, 1, 0.848039215686274, 0), new ColorDouble(1, 1, 0.843137254901961, 0), new ColorDouble(1, 1, 0.838235294117647, 0), new ColorDouble(1, 1, 0.833333333333333, 0), new ColorDouble(1, 1, 0.82843137254902, 0), new ColorDouble(1, 1, 0.823529411764706, 0), new ColorDouble(1, 1, 0.818627450980392, 0), new ColorDouble(1, 1, 0.813725490196078, 0), new ColorDouble(1, 1, 0.808823529411765, 0), new ColorDouble(1, 1, 0.803921568627451, 0), new ColorDouble(1, 1, 0.799019607843137, 0), new ColorDouble(1, 1, 0.794117647058824, 0), new ColorDouble(1, 1, 0.78921568627451, 0), new ColorDouble(1, 1, 0.784313725490196, 0), new ColorDouble(1, 1, 0.779411764705882, 0), new ColorDouble(1, 1, 0.774509803921569, 0), new ColorDouble(1, 1, 0.769607843137255, 0), new ColorDouble(1, 1, 0.764705882352941, 0), new ColorDouble(1, 1, 0.759803921568627, 0), new ColorDouble(1, 1, 0.754901960784314, 0), new ColorDouble(1, 1, 0.75, 0), new ColorDouble(1, 1, 0.745098039215686, 0), new ColorDouble(1, 1, 0.740196078431373, 0), new ColorDouble(1, 1, 0.735294117647059, 0), 
                new ColorDouble(1, 1, 0.730392156862745, 0), new ColorDouble(1, 1, 0.725490196078431, 0), new ColorDouble(1, 1, 0.720588235294118, 0), new ColorDouble(1, 1, 0.715686274509804, 0), new ColorDouble(1, 1, 0.71078431372549, 0), new ColorDouble(1, 1, 0.705882352941176, 0), new ColorDouble(1, 1, 0.700980392156863, 0), new ColorDouble(1, 1, 0.696078431372549, 0), new ColorDouble(1, 1, 0.691176470588235, 0), new ColorDouble(1, 1, 0.686274509803922, 0), new ColorDouble(1, 1, 0.681372549019608, 0), new ColorDouble(1, 1, 0.676470588235294, 0), new ColorDouble(1, 1, 0.67156862745098, 0), new ColorDouble(1, 1, 0.666666666666667, 0), new ColorDouble(1, 1, 0.661764705882353, 0), new ColorDouble(1, 1, 0.656862745098039, 0), new ColorDouble(1, 1, 0.651960784313726, 0), new ColorDouble(1, 1, 0.647058823529412, 0), new ColorDouble(1, 1, 0.642156862745098, 0), new ColorDouble(1, 1, 0.637254901960784, 0), new ColorDouble(1, 1, 0.632352941176471, 0), new ColorDouble(1, 1, 0.627450980392157, 0), new ColorDouble(1, 1, 0.622549019607843, 0), new ColorDouble(1, 1, 0.617647058823529, 0), new ColorDouble(1, 1, 0.612745098039216, 0), new ColorDouble(1, 1, 0.607843137254902, 0), new ColorDouble(1, 1, 0.602941176470588, 0), new ColorDouble(1, 1, 0.598039215686274, 0), new ColorDouble(1, 1, 0.593137254901961, 0), new ColorDouble(1, 1, 0.588235294117647, 0), new ColorDouble(1, 1, 0.583333333333333, 0), new ColorDouble(1, 1, 0.57843137254902, 0), new ColorDouble(1, 1, 0.573529411764706, 0), new ColorDouble(1, 1, 0.568627450980392, 0), new ColorDouble(1, 1, 0.563725490196078, 0), new ColorDouble(1, 1, 0.558823529411765, 0), new ColorDouble(1, 1, 0.553921568627451, 0), new ColorDouble(1, 1, 0.549019607843137, 0), new ColorDouble(1, 1, 0.544117647058824, 0), new ColorDouble(1, 1, 0.53921568627451, 0), new ColorDouble(1, 1, 0.534313725490196, 0), 
                new ColorDouble(1, 1, 0.529411764705882, 0), new ColorDouble(1, 1, 0.524509803921569, 0), new ColorDouble(1, 1, 0.519607843137255, 0), new ColorDouble(1, 1, 0.514705882352941, 0), new ColorDouble(1, 1, 0.509803921568627, 0), new ColorDouble(1, 1, 0.504901960784314, 0), new ColorDouble(1, 1, 0.5, 0), new ColorDouble(1, 1, 0.495098039215686, 0), new ColorDouble(1, 1, 0.490196078431373, 0), new ColorDouble(1, 1, 0.485294117647059, 0), new ColorDouble(1, 1, 0.480392156862745, 0), new ColorDouble(1, 1, 0.475490196078431, 0), new ColorDouble(1, 1, 0.470588235294118, 0), new ColorDouble(1, 1, 0.465686274509804, 0), new ColorDouble(1, 1, 0.46078431372549, 0), new ColorDouble(1, 1, 0.455882352941176, 0), new ColorDouble(1, 1, 0.450980392156863, 0), new ColorDouble(1, 1, 0.446078431372549, 0), new ColorDouble(1, 1, 0.441176470588235, 0), new ColorDouble(1, 1, 0.436274509803922, 0), new ColorDouble(1, 1, 0.431372549019608, 0), new ColorDouble(1, 1, 0.426470588235294, 0), new ColorDouble(1, 1, 0.42156862745098, 0), new ColorDouble(1, 1, 0.416666666666667, 0), new ColorDouble(1, 1, 0.411764705882353, 0), new ColorDouble(1, 1, 0.406862745098039, 0), new ColorDouble(1, 1, 0.401960784313726, 0), new ColorDouble(1, 1, 0.397058823529412, 0), new ColorDouble(1, 1, 0.392156862745098, 0), new ColorDouble(1, 1, 0.387254901960784, 0), new ColorDouble(1, 1, 0.382352941176471, 0), new ColorDouble(1, 1, 0.377450980392157, 0), new ColorDouble(1, 1, 0.372549019607843, 0), new ColorDouble(1, 1, 0.367647058823529, 0), new ColorDouble(1, 1, 0.362745098039216, 0), new ColorDouble(1, 1, 0.357843137254902, 0), new ColorDouble(1, 1, 0.352941176470588, 0), new ColorDouble(1, 1, 0.348039215686274, 0), new ColorDouble(1, 1, 0.343137254901961, 0), new ColorDouble(1, 1, 0.338235294117647, 0), new ColorDouble(1, 1, 0.333333333333333, 0), 
                new ColorDouble(1, 1, 0.32843137254902, 0), new ColorDouble(1, 1, 0.323529411764706, 0), new ColorDouble(1, 1, 0.318627450980392, 0), new ColorDouble(1, 1, 0.313725490196078, 0), new ColorDouble(1, 1, 0.308823529411765, 0), new ColorDouble(1, 1, 0.303921568627451, 0), new ColorDouble(1, 1, 0.299019607843137, 0), new ColorDouble(1, 1, 0.294117647058824, 0), new ColorDouble(1, 1, 0.28921568627451, 0), new ColorDouble(1, 1, 0.284313725490196, 0), new ColorDouble(1, 1, 0.279411764705882, 0), new ColorDouble(1, 1, 0.274509803921569, 0), new ColorDouble(1, 1, 0.269607843137255, 0), new ColorDouble(1, 1, 0.264705882352941, 0), new ColorDouble(1, 1, 0.259803921568627, 0), new ColorDouble(1, 1, 0.254901960784314, 0), new ColorDouble(1, 1, 0.25, 0), new ColorDouble(1, 1, 0.245098039215686, 0), new ColorDouble(1, 1, 0.240196078431373, 0), new ColorDouble(1, 1, 0.235294117647059, 0), new ColorDouble(1, 1, 0.230392156862745, 0), new ColorDouble(1, 1, 0.225490196078431, 0), new ColorDouble(1, 1, 0.220588235294118, 0), new ColorDouble(1, 1, 0.215686274509804, 0), new ColorDouble(1, 1, 0.21078431372549, 0), new ColorDouble(1, 1, 0.205882352941176, 0), new ColorDouble(1, 1, 0.200980392156863, 0), new ColorDouble(1, 1, 0.196078431372549, 0), new ColorDouble(1, 1, 0.191176470588235, 0), new ColorDouble(1, 1, 0.186274509803922, 0), new ColorDouble(1, 1, 0.181372549019608, 0), new ColorDouble(1, 1, 0.176470588235294, 0), new ColorDouble(1, 1, 0.17156862745098, 0), new ColorDouble(1, 1, 0.166666666666667, 0), new ColorDouble(1, 1, 0.161764705882353, 0), new ColorDouble(1, 1, 0.156862745098039, 0), new ColorDouble(1, 1, 0.151960784313726, 0), new ColorDouble(1, 1, 0.147058823529412, 0), new ColorDouble(1, 1, 0.142156862745098, 0), new ColorDouble(1, 1, 0.137254901960784, 0), new ColorDouble(1, 1, 0.132352941176471, 0), 
                new ColorDouble(1, 1, 0.127450980392157, 0), new ColorDouble(1, 1, 0.122549019607843, 0), new ColorDouble(1, 1, 0.117647058823529, 0), new ColorDouble(1, 1, 0.112745098039216, 0), new ColorDouble(1, 1, 0.107843137254902, 0), new ColorDouble(1, 1, 0.102941176470588, 0), new ColorDouble(1, 1, 0.0980392156862745, 0), new ColorDouble(1, 1, 0.0931372549019608, 0), new ColorDouble(1, 1, 0.0882352941176471, 0), new ColorDouble(1, 1, 0.0833333333333333, 0), new ColorDouble(1, 1, 0.0784313725490196, 0), new ColorDouble(1, 1, 0.0735294117647059, 0), new ColorDouble(1, 1, 0.0686274509803921, 0), new ColorDouble(1, 1, 0.0637254901960784, 0), new ColorDouble(1, 1, 0.0588235294117647, 0), new ColorDouble(1, 1, 0.053921568627451, 0), new ColorDouble(1, 1, 0.0490196078431372, 0), new ColorDouble(1, 1, 0.0441176470588235, 0), new ColorDouble(1, 1, 0.0392156862745098, 0), new ColorDouble(1, 1, 0.0343137254901961, 0), new ColorDouble(1, 1, 0.0294117647058824, 0), new ColorDouble(1, 1, 0.0245098039215687, 0), new ColorDouble(1, 1, 0.0196078431372549, 0), new ColorDouble(1, 1, 0.0147058823529412, 0), new ColorDouble(1, 1, 0.00980392156862747, 0), new ColorDouble(1, 1, 0.00490196078431371, 0)
            };
        #endregion Properties

        #region Constructors
            /// <summary>
            ///  Create an instance of ColorByte using A R G B value
            /// </summary>
            /// <param name="a">The Alpha channel value</param>
            /// <param name="r">The Red channel Value</param>
            /// <param name="g">The Green channel Value</param>
            /// <param name="b">The Blue channel Value</param>
            public ColorByte(byte a, byte r, byte g, byte b)
            {
                _a = a;
                _r = r;
                _g = g;
                _b = b;
                _isDefined = true;
                _isScRgb = false;
            }
            /// <summary>
            ///  Create an instance of ColorByte using the normalized Alpha,Red,Green and Blue
            /// Note : if the channel value is out of bound, the value will be saturated, no exception is thrown
            /// </summary>
            /// <param name="alpha">The Alpha channel</param>
            /// <param name="red">The Red channel</param>
            /// <param name="green">The Green channel</param>
            /// <param name="blue">The Blue channel</param>
            public ColorByte(double alpha, double red, double green, double blue)
            {
                if (double.IsNaN(alpha) || double.IsNaN(red) || double.IsNaN(green) || double.IsNaN(blue))
                {
                    throw new ArgumentOutOfRangeException("One of the channel passed in is not a valid double (NaN). Check your parameters.");
                }
                if (double.IsInfinity(alpha) || double.IsInfinity(red) || double.IsInfinity(green) || double.IsInfinity(blue))
                {
                    throw new ArgumentOutOfRangeException("Channel value cannot be set to infinity (double.PositiveInfinity nor double.NegativeInfinity). Check your parameters.");
                }

                _a = SaturateChannel(alpha);
                _r = SaturateChannel(red);
                _g = SaturateChannel(green);
                _b = SaturateChannel(blue);
                _isDefined = true;
                _isScRgb = false;
            }
            /// <summary>
            ///  Create an instance of ColorByte based on a System.Drawing.Color
            /// </summary>
            /// <param name="gdiColor">The color to be using as input value</param>
            public ColorByte(System.Drawing.Color gdiColor)
            {
                if (gdiColor != System.Drawing.Color.Empty)
                {
                    _a = gdiColor.A;
                    _r = gdiColor.R;
                    _g = gdiColor.G;
                    _b = gdiColor.B;
                    _isDefined = true;
                }
                else 
                {
                    _a = 0;
                    _r = 0;
                    _g = 0;
                    _b = 0;
                    _isDefined = false;
                }

                _isScRgb = false;
            }
            /// <summary>
            ///  Create an instance of ColorByte using any color implementing IColor
            /// </summary>
            /// <param name="color">An instance of a type implementing IColor</param>
            public ColorByte(IColor color)
            {
                if (color == null)
                {
                    throw new ArgumentNullException("color", "The value passed in must be a valid instance of an object implementing IColor (null was passed in)");
                }
                this._a = color.A;
                this._r = color.R;
                this._g = color.G;
                this._b = color.B;
                this._isDefined = ! color.IsEmpty;
                this._isScRgb = color.IsScRgb;
            }
        #endregion Constructors

        #region IColor interface implementation
            /// <summary>
            /// Get/set the all the channels at once
            /// </summary>
            /// <value></value>
            public int ARGB 
            {
                get { return ((int)_a << 24) + ((int)_r << 16) + ((int)_g << 8) + (int)_b; }
                set 
                {
                    A = (byte)(value >> 24);
                    R = (byte)((value & 0x00FF0000) >> 16);
                    G = (byte)((value & 0x0000FF00) >> 8);
                    B = (byte)(value & 0x000000FF);
                }
            }
            /// <summary>
            /// Get/set the Red, Green and Blue channels at once
            /// </summary>
            /// <value></value>
            public int RGB 
            {
                get { return (int)_r >> 16 + (int)_g >> 8 + (int)_b; }
                set 
                {
                    // Do not shield against value > 0x00ffffff () so we can do something like a.RGB = b.ARGB, just ignore Alpha channel
                    R = (byte)((value & 0x00FF0000) >> 16);
                    G = (byte)((value & 0x0000FF00) >> 8);
                    B = (byte)(value & 0x000000FF);
                }
            }
            /// <summary>
            /// Get/set the Extended value for the Alpha channel
            /// </summary>
            /// <value></value>
            public double ExtendedAlpha 
            {
                get { return Alpha; }
                set { A = SaturateChannel(value); }
            }
            /// <summary>
            /// Get/set the normalized value for the Alpha channel
            /// </summary>
            /// <value></value>
            public double Alpha 
            {
                get { return _a / _normalizedValue; }
                set { A = CheckChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Alpha channel
            /// </summary>
            /// <value></value>
            public byte A  
            { 
                get { return _a; }
                set { this._isDefined = true; _a = value; }
            }
            /// <summary>
            /// Get/set the Extended value for the Red channel
            /// </summary>
            /// <value></value>
            public double ExtendedRed  
            {
                get { return Red; }
                set { R = SaturateChannel(value); }
            }
            /// <summary>
            /// Get/set the normalized value for the Red channel
            /// </summary>
            /// <value></value>
            public double Red  
            {
                get { return _r / _normalizedValue; }
                set { R = CheckChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Red channel
            /// </summary>
            /// <value></value>
            public byte R  
            { 
                get { return _r; }
                set { this._isDefined = true; _r = value; }
            }
            /// <summary>
            /// Get/set the Extended value for the Green channel
            /// </summary>
            /// <value></value>
            public double ExtendedGreen  
            {
                get { return Green; }
                set { G = SaturateChannel(value); }
            }
            /// <summary>
            /// Get/set the normalized value for the Green channel
            /// </summary>
            /// <value></value>
            public double Green 
            {
                get { return _g / _normalizedValue; }
                set { G = CheckChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Green channel
            /// </summary>
            /// <value></value>
            public byte G 
            { 
                get { return _g; }
                set { this._isDefined = true; _g = value; }
            }
            /// <summary>
            /// Get/set the Extended value for the Blue channel
            /// </summary>
            /// <value></value>
            public double ExtendedBlue 
            {
                get { return Blue; }
                set { B = SaturateChannel(value); }
            }
            /// <summary>
            /// Get/set the normalized value for the Blue channel
            /// </summary>
            /// <value></value>
            public double Blue 
            {
                get { return _b / _normalizedValue; }
                set { B = CheckChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Blue channel
            /// </summary>
            /// <value></value>
            public byte B 
            { 
                get { return _b; }
                set { this._isDefined = true; _b = value; }
            }
            /// <summary>
            /// Convert this Color to the standard "System.Drawing.Color" type
            /// </summary>
            /// <returns></returns>
            public System.Drawing.Color ToColor()
            {
                return (_isDefined) ? System.Drawing.Color.FromArgb(_a, _r, _g, _b) : System.Drawing.Color.Empty;
            }
            /// <summary>
            /// Get/set the color as Empty 
            /// </summary>
            /// <value></value>
            public bool IsEmpty
            {
                get{return ! _isDefined;}
                set
                {
                    if (value == true) { _a = 0; _r = 0; _g = 0; _b = 0; }
                    _isDefined = !value;
                }
            }
            /// <summary>
            /// Retrieve if this type can effectively deal with scRGB color (no information loss when filtering)
            /// </summary>
            /// <value></value>
            public bool SupportExtendedColor
            {
                get { return false; }
            }
            /// <summary>
            /// Get/set the color to scRgb (Gamma 1.0) or not (Gamma 2.2)
            /// </summary>
            /// <value></value>
            public bool IsScRgb
            {
                get { return _isScRgb; }
                set { _isScRgb = value; }
            }
            /// <summary>
            /// Get/set the Max value for all channels when normalizing
            /// </summary>
            /// <value></value>
            public double MaxChannelValue 
            {
                get { return ColorByte._maxChannelValue; }
                set { ColorByte._maxChannelValue = value; }
            }
            /// <summary>
            /// Get/set the Min value for all channels when normalizing
            /// </summary>
            /// <value></value>
            public double MinChannelValue
            {
                get { return ColorByte._minChannelValue; }
                set { ColorByte._minChannelValue = value; }
            }
            /// <summary>
            /// Get/set the Normalization value
            /// </summary>
            /// <value></value>
            public double NormalizedValue
            {
                get { return ColorByte._normalizedValue; }
                set { ColorByte._normalizedValue = value; }
            }
        #endregion IColor interface implementation

        #region IClonable interface implementation
            /// <summary>
            /// Clone the current object
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new ColorByte(this);
            }
        #endregion IClonable interface implementation

        #region Methods
            #region Private Methods
                private static byte CheckChannelBound(double normalizedValue)
                {
                    int channelValue = (int)(normalizedValue * ColorByte._normalizedValue + 0.5);
                    if (channelValue < 0 || channelValue > 255)
                    {
                        throw new ArgumentOutOfRangeException("normalizedValue",normalizedValue,"Value must be between 0 and 1");
                    }
                    return (byte)channelValue;
                }
                private static byte SaturateChannel(double normalizedValue)
                {
                    int channelValue = (int)(normalizedValue * ColorByte._normalizedValue + 0.5);
                    if (channelValue > 255) { channelValue = 255; }
                    if (channelValue < 0) { channelValue = 0; }
                    return (byte)channelValue;
                }
                private static byte SaturateChannel(int channelValue)
                {
                    if (channelValue > 255) { channelValue = 255; }
                    if (channelValue < 0) { channelValue = 0; }
                    return (byte)channelValue;
                }
            #endregion Private Methods
            #region Operators overload
                /// <summary>
                /// Cast a GDI color (System.Drawing.Color) into this type
                /// </summary>
                /// <param name="gdiColor">The GDI color to cast</param>
                /// <returns>The color translated in this type</returns>
                public static explicit operator ColorByte(System.Drawing.Color gdiColor)
                { 
                    ColorByte colorFast = new ColorByte();
                    if(gdiColor != Color.Empty)
                    {
                        colorFast.A = gdiColor.A;
                        colorFast.R = gdiColor.R;
                        colorFast.G = gdiColor.G;
                        colorFast.B = gdiColor.B;
                    }
                    return colorFast;
                }
                /// <summary>
                /// Cast this type into a GDI color (System.Drawing.Color)
                /// </summary>
                /// <param name="color">This ColorByte type to cast</param>
                /// <returns>The color translated in the standard GDI type</returns>
                public static explicit operator System.Drawing.Color(ColorByte color)
                {
                    System.Drawing.Color gdiColor = System.Drawing.Color.Empty;
                    if (color.IsEmpty == false)
                    {
                        gdiColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
                    }
                    return gdiColor;
                }
                /// <summary>
                /// Cast a ColorDouble type into a ColorByte type. The value is rounded to the nearest color
                /// </summary>
                /// <param name="colorPrecision">The ColorDouble to be converted</param>
                /// <returns>Returns a ColorByte type representing the ColorDouble value (loss of information might occur)</returns>
                public static implicit operator ColorByte(ColorDouble colorPrecision)
                {
                    ColorByte retVal = new ColorByte();
                    if(colorPrecision.IsEmpty) { return retVal; }
                    retVal.A = colorPrecision.A;
                    retVal.R = colorPrecision.R;
                    retVal.G = colorPrecision.G;
                    retVal.B = colorPrecision.B;
                    retVal._isDefined = ! colorPrecision.IsEmpty;
                    return retVal;
                }
                /// <summary>
                /// Merge two color using the Alpha channel value as multiplier
                /// </summary>
                /// <param name="colorFast1">The first color to merge</param>
                /// <param name="colorFast2">The second color to merge</param>
                /// <returns>The merge color (averaging based on the alpha channel)</returns>
                // @ review : assume not premultiply ?
                // @ review : if both color are opaque should be just add them (instead of averaging them) ? 
                //    ...Might help some customer but would be inconstient (not done for now)
                public static ColorByte operator +(ColorByte colorFast1, ColorByte colorFast2)
                {
                    ColorByte retVal = new ColorByte();
                    if (colorFast1.IsEmpty == false && colorFast2.IsEmpty == false)
                    {
                        retVal.A = Math.Max(colorFast1.A, colorFast2.A);
                        retVal.R = SaturateChannel( (int)((colorFast1.R * colorFast1.A + colorFast2.R * colorFast2.A) / 510.0));
                        retVal.G = SaturateChannel( (int)((colorFast1.G * colorFast1.A + colorFast2.G * colorFast2.A) / 510.0));
                        retVal.B = SaturateChannel( (int)((colorFast1.B * colorFast1.A + colorFast2.B * colorFast2.A) / 510.0));
                        retVal._isDefined = true;
                    }
                    else
                    {
                        if (colorFast1.IsEmpty) { retVal = (ColorByte)colorFast2.Clone(); }
                        else { retVal = (ColorByte)colorFast1.Clone(); }
                    }
                    return retVal;
                }
                /// <summary>
                /// Un-Merge two color using the Alpha channel value as multiplier
                /// </summary>
                /// <param name="colorFast1">The resulting color</param>
                /// <param name="colorFast2">The color to remove from the resulting image</param>
                /// <returns>The merging color (color that would lead to color one perform "thisColor + color2")</returns>
                // @ review : assume not premultiply ?
                // @ review : if both color are opaque should be just subtract them (instead of un-averaging them) ? 
                //    ...Might help some customer but would be inconstient (not done for now)
                public static ColorByte operator -(ColorByte colorFast1, ColorByte colorFast2)
                {
                    ColorByte retVal = new ColorByte();
                    if (colorFast1.IsEmpty == false || colorFast2.IsEmpty == false)
                    {
                        retVal.A = Math.Max(colorFast1.A, colorFast2.A);
                        retVal.R = SaturateChannel( (int)((colorFast1.R * colorFast1.A * 2 - colorFast2.R * colorFast2.A) / 255.0) );
                        retVal.G = SaturateChannel( (int)((colorFast1.G * colorFast1.A * 2 - colorFast2.G * colorFast2.A) / 255.0) );
                        retVal.B = SaturateChannel( (int)((colorFast1.B * colorFast1.A * 2 - colorFast2.B * colorFast2.A) / 255.0) );
                        retVal._isDefined = true;
                   }
                    return retVal;
                }
                /// <summary>
                /// Negate a color without affecting the alpha channel.
                /// </summary>
                /// <param name="colorFast">The color to negate</param>
                /// <returns>The negated color</returns>
                // @ review : IGNORE alpha ? (we are right now)
                public static ColorByte operator !(ColorByte colorFast)
                {
                    ColorByte retVal = new ColorByte();
                    if (colorFast.IsEmpty)
                    {
                        return retVal;
                    }

                    retVal.A = colorFast.A;
                    retVal.R = SaturateChannel(ColorByte._maxChannelValue - colorFast.R / ColorByte._normalizedValue);
                    retVal.G = SaturateChannel(ColorByte._maxChannelValue - colorFast.G / ColorByte._normalizedValue);
                    retVal.B = SaturateChannel(ColorByte._maxChannelValue - colorFast.B / ColorByte._normalizedValue);
                    retVal._isDefined = true;

                    return retVal;
                }
                /// <summary>
                /// Compare two ColorByte type for equality
                /// </summary>
                /// <param name="colorFast1">The first color to compare</param>
                /// <param name="colorFast2">The second color to compare</param>
                /// <returns>return true if the color are the same, false otherwise</returns>
                public static bool operator ==(ColorByte colorFast1, ColorByte colorFast2)
                {
                    if(colorFast1._a == colorFast2._a && colorFast1._r == colorFast2._r &&
                        colorFast1._g == colorFast2._g && colorFast1._b == colorFast2._b &&
                        colorFast1._isDefined == colorFast2._isDefined)
                    {
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// Compare two ColorByte type for inequality
                /// </summary>
                /// <param name="colorFast1">The first color to compare</param>
                /// <param name="colorFast2">The second color to compare</param>
                /// <returns>return true if the color are the different, false if they are the same</returns>
                public static bool operator !=(ColorByte colorFast1, ColorByte colorFast2)
                {
                    return ! (colorFast1 == colorFast2);
                }
            #endregion Operators overload
            #region Overriden Methods
                /// <summary>
                /// Compare two ColorByte for equality
                /// </summary>
                /// <param name="obj">The color to compare against</param>
                /// <returns>returns true if the color are the same, false otherwise</returns>
                public override bool Equals(object obj)
                {
                    if (obj is ColorByte)
                    {
                        return this == (ColorByte)obj;
                    }
                    throw new InvalidCastException("The typoe passed ('" + obj.GetType().ToString() + "') in cannot be casted to a ColorByte object");
                }
                /// <summary>
                /// Get the hashcode for this color
                /// </summary>
                /// <returns></returns>
                public override int GetHashCode()
                {
                    if (_isDefined == false)
                    {
                        return System.Drawing.Color.Empty.GetHashCode();
                    }
                    return System.Drawing.Color.FromArgb(_a, _r, _g,_b).GetHashCode();
                }
                /// <summary>
                /// Display the value of this color in a friendly manner
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    if (_isDefined == false) { return "ColorByte [Empty]"; }
                    return "ColorByte [ A:" +_a.ToString() +" / R:" + _r.ToString() + " / G:" +_g.ToString() +" / B:" + _b.ToString() + " ]";
                }
            #endregion Overriden Methods
            /// <summary>
            /// Return a LUT color for mapping a scalar onto a rainbow  
            /// </summary>
            /// <param name="luminance">Luminance value</param>
            /// <returns>The pseudo-color mapping to this luminance value</returns>
            public static IColor GetColorFromLUT(double luminance)
            {
                if (luminance < 0.0) { luminance = 0.0; }

                if (luminance > 1.0) { luminance = 1.0; }

                return _colorLUT[(int)(luminance * (_colorLUT.Length - 1))];
            }

        #endregion Methods
    }
}
