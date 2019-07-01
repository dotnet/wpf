// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Globalization;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    public class ValidationRuleForBinding : ValidationRule
    {
        #region Public Members

        public ValidationRuleForBinding(int intMax, int intMin, double doubleMax, double doubleMin, int stringLenMax, int stringLenMin)
        {
            maxInt = intMax;
            minInt = intMin;
            maxDouble = doubleMax;
            minDouble = doubleMin;
            maxStringLen = stringLenMax;
            minStringLen = stringLenMin;
        }

        public int MaxInt
        {
            get { return maxInt; }
            set { maxInt = value; }
        }

        public int MinInt
        {
            get { return minInt; }
            set { minInt = value; }
        }

        public double MaxDouble
        {
            get { return maxDouble; }
            set { maxDouble = value; }
        }

        public double MinDouble
        {
            get { return minDouble; }
            set { minDouble = value; }
        }

        public int MaxStringLen
        {
            get { return maxStringLen; }
            set { maxStringLen = value; }
        }

        public int MinStringLen
        {
            get { return minStringLen; }
            set { minStringLen = value; }
        }

        #endregion

        #region Override Members

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value.GetType() == typeof(int) && ((int)value > MaxInt || (int)value < MinInt))
            {
                return new ValidationResult(false, null);
            }

            if (value.GetType() == typeof(double) && ((double)value > MaxDouble || (double)value < MinDouble))
            {
                return new ValidationResult(false, null);
            }

            if (value.GetType() == typeof(string) && (((string)value).Length > MaxStringLen || ((string)value).Length < MinStringLen))
            {
                return new ValidationResult(false, null);
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }

        #endregion

        #region Private Data

        private int maxInt;

        private int minInt;

        private double maxDouble;

        private double minDouble;

        private int maxStringLen;

        private int minStringLen;

        #endregion
    }
}
