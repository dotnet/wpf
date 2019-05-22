// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Abstract:

    Definition and implementation of this public feature/parameter related types.


--*/

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents job copy count capability.
    /// </summary>
    internal sealed class JobCopyCountCapability : NonNegativeIntParameterDefinition
    {
        #region Constructors

        internal JobCopyCountCapability() : base()
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static ParameterDefinition NewParamDefCallback(InternalPrintCapabilities printCap)
        {
            JobCopyCountCapability cap = new JobCopyCountCapability();

            return cap;
        }

        #endregion Internal Methods
    }

    /// <summary>
    /// Represents job copy count setting.
    /// </summary>
    internal class JobCopyCountSetting : PrintTicketParameter
    {
        #region Constructors

        /// <summary>
        /// Constructs a new job copy count setting object.
        /// </summary>
        internal JobCopyCountSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket,
                   PrintSchemaTags.Keywords.ParameterDefs.JobCopyCount,
                   PrintTicketParamTypes.Parameter,
                   PrintTicketParamValueTypes.IntValue)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of job copy count.
        /// </summary>
        /// <remarks>
        /// If this setting is not specified yet, getter will return <see cref="PrintSchema.UnspecifiedIntValue"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not a positive integer.
        /// </exception>
        public int Value
        {
            get
            {
                return this.IntValue;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                                  PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                }

                this.IntValue = value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the job copy count setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this job copy count setting.</returns>
        public override string ToString()
        {
            return Value.ToString(CultureInfo.CurrentCulture);
        }

        #endregion Public Methods

        #region Internal Methods

        internal override sealed void SettingClearCallback()
        {
        }

        #endregion Internal Methods
    }
}
