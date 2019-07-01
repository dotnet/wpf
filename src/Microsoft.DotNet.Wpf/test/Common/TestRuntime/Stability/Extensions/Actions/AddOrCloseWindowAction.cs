// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class AddOrCloseWindowAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetWindowListFromState)]
        public List<Window> WindowList { get; set; }

        public Window NewWindow { get; set; }

        public bool IsRemove { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            //There will be at most 5 Windows. And It can't remove the first Window. 
            if (WindowList.Count > 5 || (IsRemove && WindowList.Count > 1))
            {
                //The new Window already got created and shown.
                NewWindow.Close();

                //Select removed Window from 1 to Count-1.
                RemoveIndex = RemoveIndex % (WindowList.Count - 1) + 1;
                Window removeWindow = WindowList[RemoveIndex];

                //Close the window before removing it.
                removeWindow.Close();

                WindowList.Remove(removeWindow);
            }
            else
            {
                WindowList.Add(NewWindow);
            }
        }

        #endregion
    }
}
