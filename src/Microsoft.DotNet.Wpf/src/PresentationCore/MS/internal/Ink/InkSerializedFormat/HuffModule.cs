// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Ink;
using MS.Internal.Ink.InkSerializedFormat;
using System.Collections.Generic;


using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// HuffModule
    /// </summary>
    internal class HuffModule
    {
        /// <summary>
        /// Ctor
        /// </summary>
        internal HuffModule()
        {
        }

        /// <summary>
        /// GetDefCodec
        /// </summary>
        internal HuffCodec GetDefCodec(uint index)
        {
            HuffCodec huffCodec = null;
            if (AlgoModule.DefaultBAACount > index)
            {
                huffCodec = _defaultHuffCodecs[index];
                if (huffCodec == null)
                {
                    huffCodec = new HuffCodec(index);
                    _defaultHuffCodecs[index] = huffCodec;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return huffCodec;
        }

        /// <summary>
        /// FindCodec
        /// </summary>
        /// <param name="algoData"></param>
        internal HuffCodec FindCodec(byte algoData)
        {
            byte codec = (byte)(algoData & 0x1f);
            //unused
            //if ((0x20 & algoData) != 0)
            //{
            //    int iLookup = (algoData & 0x1f);
            //    if ((iLookup > 0) && (iLookup <= _lookupList.Count))
            //    {
            //        codec = _lookupList[iLookup - 1].Byte;
            //    }
            //}

            if (codec < AlgoModule.DefaultBAACount)
            {
                return GetDefCodec((uint)codec);
            }
            
            if ((int)codec >= _huffCodecs.Count + AlgoModule.DefaultBAACount)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("invalid codec computed"));
            }
            return _huffCodecs[(int)(codec - AlgoModule.DefaultBAACount)];
        }

        /// <summary>
        /// FindDtXf
        /// </summary>
        /// <param name="algoData"></param>
        internal DataXform FindDtXf(byte algoData)
        {
            //unused
            //if ((0x20 & algoData) != 0)
            //{
            //    int lookupIndex = (int)(algoData & 0x1f);
            //    if ((lookupIndex > 0) && (lookupIndex < _lookupList.Count))
            //    {
            //        return _lookupList[lookupIndex].DeltaDelta;
            //    }
            //}
            return this.DefaultDeltaDelta;
        }

        /// <summary>
        /// Private lazy init'd
        /// </summary>
        private DeltaDelta DefaultDeltaDelta
        {
            get
            {
                if (_defaultDtxf == null)
                {
                    _defaultDtxf = new DeltaDelta();
                }
                return _defaultDtxf;
            }
        }

        /// <summary>
        /// Privates
        /// </summary>
        private DeltaDelta          _defaultDtxf;
        //unused
        //private List<CodeLookup>    _lookupList = new List<CodeLookup>();
        private List<HuffCodec>     _huffCodecs = new List<HuffCodec>();
        private HuffCodec[]         _defaultHuffCodecs = new HuffCodec[AlgoModule.DefaultBAACount];

        //unused
        ///// <summary>
        ///// Simple helper class
        ///// </summary>
        //private class CodeLookup
        //{
        //    internal CodeLookup(DeltaDelta dd, byte b)
        //    {
        //        if (dd == null) { throw new ArgumentNullException(); }
        //        DeltaDelta = dd;
        //        Byte = b;
        //    }
        //    internal DeltaDelta DeltaDelta;
        //    internal Byte Byte;
        //}
    }
}
