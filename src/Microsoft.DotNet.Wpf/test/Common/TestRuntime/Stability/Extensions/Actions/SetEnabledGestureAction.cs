// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Sets the application gestures that the GestureRecognizer or the InkCanvas will recognize.
    /// </summary>
    public class SetEnabledGestureAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int CountIndex { get; set; }

        public int GestureIndex { get; set; }

        public override void Perform()
        {
            ApplicationGesture[] allAppGestures = (ApplicationGesture[])Enum.GetValues(ApplicationGesture.Up.GetType());

            //Get a number of gestures we will enable
            int gestureCount = CountIndex % (allAppGestures.Length - 2) + 1;

            ArrayList enableGesturesArrayList = new ArrayList();

            //Fill the arraylist with the number of gestures we want to enable
            while (gestureCount > 0)
            {
                int index = GestureIndex % allAppGestures.Length;
                GestureIndex++;

                //If adding an AllGestures,reject it and continue to get the next gesture to add
                if (allAppGestures[index] == ApplicationGesture.AllGestures)
                    continue;

                bool gestureExists = false;

                //Check if the appgesture is already added. if there are duplicates, API throws exception
                for (int i = 0; i < enableGesturesArrayList.Count; i++)
                {
                    ApplicationGesture currentGesture = (ApplicationGesture)enableGesturesArrayList[i];
                    //Don't add something that already exists in the enableGestures list
                    if (allAppGestures[index] == currentGesture)
                    {
                        gestureExists = true;
                        break;
                    }
                }
                //Not added so far, add and continue
                if (gestureExists == false)
                {
                    enableGesturesArrayList.Add(allAppGestures[index]);
                    gestureCount--; //One less thing to add
                }
            }

            ApplicationGesture[] applicationGestureArray = (ApplicationGesture[])enableGesturesArrayList.ToArray(ApplicationGesture.ArrowDown.GetType());

            //Sets the application gestures that the InkCanvas recognizes.
            InkCanvas.SetEnabledGestures(applicationGestureArray);
        }

        public override bool CanPerform()
        {
            return InkCanvas.IsGestureRecognizerAvailable;
        }
    }
}
