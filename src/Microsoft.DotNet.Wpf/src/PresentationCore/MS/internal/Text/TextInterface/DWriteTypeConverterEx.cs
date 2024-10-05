// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Windows.Media;
using MS.Internal.Interop.DWrite;

namespace MS.Internal.Text.TextInterface
{
    /// <summary>
    /// This class is used to convert data types back and forth between DWrite and DWriteWrapper.
    /// </summary>
    internal static class DWriteTypeConverterEx
    {
        internal static DWRITE_FACTORY_TYPE Convert(FactoryType factoryType)
        {
            switch (factoryType)
            {
                case FactoryType.Shared:
                    return DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED;
                case FactoryType.Isolated:
                    return DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_ISOLATED;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static DWRITE_MEASURING_MODE Convert(TextFormattingMode measuringMode)
        {
            switch (measuringMode)
            {
                case TextFormattingMode.Ideal:
                    return DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_NATURAL;
                case TextFormattingMode.Display:
                    return DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_GDI_CLASSIC;
                // We do not support Natural Metrics mode in WPF
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static TextFormattingMode Convert(DWRITE_MEASURING_MODE dwriteMeasuringMode)
        {
            switch (dwriteMeasuringMode)
            {
                case DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_NATURAL:
                    return TextFormattingMode.Ideal;
                case DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_GDI_CLASSIC:
                    return TextFormattingMode.Display;
                // We do not support Natural Metrics mode in WPF
                // However, the build system complained about not having an explicit case 
                // for DWRITE_TEXT_MEASURING_METHOD_USE_DISPLAY_NATURAL_METRICS
                case DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_GDI_NATURAL:
                    throw new InvalidOperationException();
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
