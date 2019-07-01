// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class KeyboardCombinationPressAction : SimpleDiscoverableAction
    {
        #region Public Members

        public Key RandomKey { get; set; }

        public bool IsCtrl { get; set; }

        public bool IsAlt { get; set; }

        public bool IsShift { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsKeyCombinationOkay(RandomKey, IsCtrl, IsAlt, IsShift))
            {
                List<Key> allKeys = new List<Key>();
                if (IsCtrl)
                {
                    allKeys.Add(Key.LeftCtrl);
                }
                if (IsAlt)
                {
                    allKeys.Add(Key.LeftAlt);
                }
                if (IsShift)
                {
                    allKeys.Add(Key.LeftShift);
                }
                allKeys.Add(RandomKey);

                HomelessTestHelpers.KeyPress(allKeys);
            }
        }

        #endregion

        #region Private Data

        /// <summary>
        /// Returns false if key is Control, Alt, Shift, Win or Menu key, or
        /// if combination is CTRL+ALT+DEL, or
        /// if combination is ALT+TAB, or
        /// if combination is SHIFT+CTRL+TAB.
        /// Note: maybe we have to avoid other combinations.
        /// </summary>
        private bool IsKeyCombinationOkay(Key randomByte, bool isCtrl, bool isAlt, bool isShift)
        {
            return (randomByte != Key.LeftCtrl && randomByte != Key.RightCtrl &&
                    randomByte != Key.LeftAlt && randomByte != Key.RightAlt &&
                    randomByte != Key.LeftShift && randomByte != Key.RightShift &&
                    randomByte != Key.LWin && randomByte != Key.RWin &&
                    randomByte != Key.LaunchApplication1 && randomByte != Key.LaunchApplication2 &&
                    randomByte != Key.SelectMedia && randomByte != Key.LaunchMail && randomByte != Key.Apps) &&
                    !(randomByte == Key.P && isCtrl && !isAlt && !isShift) && //CTRL+P
                    !(randomByte == Key.Delete && isCtrl && isAlt && !isShift) && //CTRL+ALT+DEL
                    !(randomByte == Key.Escape && isCtrl && !isAlt && isShift) && //CTRL+SHIFT+ESC = Launch Task Manager
                    !(randomByte == Key.F4 && !isCtrl && isAlt && !isShift) && //ALT+F4 = Close Window
                    !(randomByte == Key.F3 && !isCtrl && !isAlt && !isShift) && //F3 = Launch Search
                    !(randomByte == Key.M && isCtrl && isAlt && !isShift) && //CTRL+ALT+M = MSN Desktop Search
                    !(randomByte == Key.Escape && isCtrl && !isAlt && !isShift) && //CTRL+ESC = Start Menu
                    !(randomByte == Key.Escape && !isCtrl && isAlt) && //ALT(+SHIFT)+ESC = Send window to bottom/top z-order
                    !(randomByte == Key.Tab && !isCtrl && isAlt) && //ALT+TAB = Shift Windows
                    !(randomByte == Key.F10 && isShift) && //SHIFT+F10 = Show Context Menu
                    !(randomByte == Key.Space && !isCtrl && isAlt && !isShift) && //ALT+SPACE = Open System Menu
                    !(randomByte == Key.Tab && isCtrl && !isAlt && isShift); //CTRL+SHIFT+TAB = Shift Windows
        }

        #endregion
    }
}
