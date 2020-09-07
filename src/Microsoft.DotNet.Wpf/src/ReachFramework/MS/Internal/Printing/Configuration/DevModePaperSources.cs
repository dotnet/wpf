// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    /// <remarks>
    /// From http://msdn.microsoft.com/en-us/library/cc244659(PROT.13).aspx
    /// </remarks>
    internal static class DevModePaperSources
    {
        /// <summary>
        /// Select the upper paper bin
        /// </summary>
        public const short DMBIN_UPPER = 0x0001;

        /// <summary>
        /// Select the lower bin.
        /// </summary>
        public const short DMBIN_LOWER = 0x0002;

        /// <summary>
        /// Select the middle paper bin.
        /// </summary>
        public const short DMBIN_MIDDLE = 0x0003;

        /// <summary>
        /// Manually select the paper bin.
        /// </summary>
        public const short DMBIN_MANUAL = 0x0004;

        /// <summary>
        /// Select the auto envelope bin.
        /// </summary>
        public const short DMBIN_ENVELOPE = 0x0005;

        /// <summary>
        /// Select the manual envelope bin.
        /// </summary>
        public const short DMBIN_ENVMANUAL = 0x0006;

        /// <summary>
        /// Auto-select the bin.
        /// </summary>
        public const short DMBIN_AUTO = 0x0007;

        /// <summary>
        /// Select the bin with the tractor paper.
        /// </summary>
        public const short DMBIN_TRACTOR = 0x0008;

        /// <summary>
        /// Select the bin with the smaller paper format.
        /// </summary>
        public const short DMBIN_SMALLFMT = 0x0009;

        /// <summary>
        /// Select the bin with the larger paper format.
        /// </summary>
        public const short DMBIN_LARGEFMT = 0x000A;

        /// <summary>
        /// Select the bin with large capacity.
        /// </summary>
        public const short DMBIN_LARGECAPACITY = 0x000B;

        /// <summary>
        /// Select the cassette bin.
        /// </summary>
        public const short DMBIN_CASSETTE = 0x000E;

        /// <summary>
        /// Select the bin with the required form.
        /// </summary>
        public const short DMBIN_FORMSOURCE = 0x000F;
    }
}