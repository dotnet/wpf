// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.Windows.Ink;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create GestureRecognizer.
    /// </summary>
    internal class GestureRecognizerFactory : DiscoverableFactory<GestureRecognizer>
    {
        public override GestureRecognizer Create(DeterministicRandom random)
        {
            ApplicationGesture[] allAppGestures = (ApplicationGesture[])Enum.GetValues(random.NextEnum<ApplicationGesture>().GetType());

            //Get a number of gestures we will enable
            int gestureCount = random.Next(allAppGestures.Length - 2) + 1;

            ArrayList enableGesturesArrayList = new ArrayList();

            //Fill the arraylist with the number of gestures we want to enable
            while (gestureCount > 0)
            {
                int index = random.Next(allAppGestures.Length);

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

            ApplicationGesture[] applicationGestureArray = (ApplicationGesture[])enableGesturesArrayList.ToArray(random.NextEnum<ApplicationGesture>().GetType());

            GestureRecognizer gestureRecognizer;
            if (random.NextBool())
            {
                gestureRecognizer = new GestureRecognizer(applicationGestureArray);
            }
            else
            {
                gestureRecognizer = new GestureRecognizer();
                gestureRecognizer.SetEnabledGestures(applicationGestureArray);
            }

            return gestureRecognizer;
        }
    }
}
