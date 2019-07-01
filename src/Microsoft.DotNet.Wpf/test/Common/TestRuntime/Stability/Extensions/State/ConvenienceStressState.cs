// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Actions;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.Factories;

namespace Microsoft.Test.Stability.Extensions.State
{
    /// <summary>
    /// A serializable input payload type specific to the Convenience Stress Type
    /// </summary>
    [Serializable]
    class ConvenienceStressStatePayload : IStatePayload
    {
        /// <summary>
        /// Converts XmlInput and initializes the payload object 
        /// with explicit property values which can be serialized
        /// </summary>
        /// <param name="arguments"></param>
        public void Initialize(ContentPropertyBag bag)
        {
            // Required attributes
            FactoriesPath = GetAttributeValue(bag, "FactoriesPath", true);
            ActionsPath = GetAttributeValue(bag, "ActionsPath", true);

            // Optional attributes
            WindowXamlPaths = GetAttributeValue(bag, "WindowXamlPaths", false);
            ContentXamlPath = GetAttributeValue(bag, "ContentXamlPath", false);
            ConstraintsTablePath = GetAttributeValue(bag, "ConstraintsTablePath", false);

            if (bag["MemoryLimit"] != null)
            {
                MemoryLimit = int.Parse((string)bag["MemoryLimit"]);
            }
            else
            {
                MemoryLimit = 100000000;
            }
        }

        private string GetAttributeValue(ContentPropertyBag stateData, string attributeName, bool throwIfNotDefined)
        {
            string attributeValue = stateData[attributeName];
            if (String.IsNullOrEmpty(attributeValue) && throwIfNotDefined)
            {
                throw new InvalidDataException(String.Format("{0} attribute requested in StateArguments.", attributeName));
            }
            return attributeValue;
        }
        public string FactoriesPath { get; set; }
        public string ActionsPath { get; set; }
        public string WindowXamlPaths { get; set; }
        public string ContentXamlPath { get; set; }
        public string ConstraintsTablePath { get; set; }
        public int MemoryLimit { get; set; }
    }

    /// <summary>
    /// This contains all statefulness for a stress test.
    /// </summary>
    class ConvenienceStressState : IState
    {
        #region Private Variables
        //TODO: A window can't be root for all stress tests. 
        //This will likely need to be factored into a test selected strategy.
        //Generalizing slightly be allowing the user to load the content from XAML
        List<Window> windowList;
        ConstraintsTable constraintsTable = null;
        bool stateEnded = false;

        FactoryPool factoryPool;
        string[] windowXamls; //location of the XAML files used to load window
        string contentXaml;

        List<Type> actionTypes;
        private int memoryLimit;
        #endregion

        #region Public Implementation

        public List<Window> WindowList
        {
            get
            {
                return windowList;
            }
        }

        /// <summary>
        /// Randomly finds a factory which can produce an object of Type T
        /// </summary>
        /// <param name="t"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public DiscoverableFactory GetFactory(Type t, DeterministicRandom random, bool favorSimpleFactories)
        {
            //Hard-Terminate the test if the state context is inactive. 
            //A human user would expect the app to die on window close, and automation closing window is a problem.
            if (stateEnded)
            {
                throw new InvalidOperationException("Window for this state object was closed. Human intervention is fine, but this should not occur as a result of input automation.");
            }
            return factoryPool.GetFactory(t, random, favorSimpleFactories);
        }

        /// <summary>
        /// A heavy non-cached Logical tree value extraction from state
        /// </summary>
        /// <param name="desiredType"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public Object GetFromLogicalTree(Type desiredType, DeterministicRandom random)
        {
            List<DependencyObject> matches = new List<DependencyObject>();
            foreach (Window window in windowList)
            {
                List<DependencyObject> elements = HomelessTestHelpers.LogicalTreeWalk(window);
                elements.Add(window);

                foreach (DependencyObject obj in elements)
                {
                    if (desiredType.IsInstanceOfType(obj))
                    {
                        matches.Add(obj);
                    }
                }
            }

            if (matches.Count == 0)
            {
                return null;
            }

            return random.NextItem<DependencyObject>(matches);
        }

        /// <summary>
        /// A heavy non-cached Visual tree value extraction from state
        /// </summary>
        /// <param name="desiredType"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public DependencyObject GetFromVisualTree(Type desiredType, DeterministicRandom random)
        {
            List<DependencyObject> matches = new List<DependencyObject>();

            foreach (Window window in windowList)
            {
                List<DependencyObject> elements = HomelessTestHelpers.VisualTreeWalk(window);
                elements.Add(window);

                foreach (DependencyObject obj in elements)
                {
                    if (desiredType.IsInstanceOfType(obj))
                    {
                        matches.Add(obj);
                    }
                }
            }

            if (matches.Count == 0)
                return null;

            return random.NextItem<DependencyObject>(matches);
        }

        /// <summary>
        /// Get an random object of desired type from the whole object tree.
        /// </summary>
        /// <param name="desiredType"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public object GetFromObjectTree(Type desiredType, DeterministicRandom random)
        {
            List<object> matches = new List<object>();

            foreach (Window window in windowList)
            {
                matches.AddRange(HomelessTestHelpers.GetAllObjects(window, desiredType));
            }

            if (matches.Count == 0)
                return null;

            return random.NextItem<object>(matches);
        }

        public object MakeFromConstraint(Type targetType, string propertyName, DeterministicRandom random)
        {
            //TODO: How do we associate types to Data Source consumers? This needs work.
            ConstrainedDataSource source = constraintsTable.Get(TargetTypeAttribute.FindTarget(targetType), propertyName);
            return source.GetData(random);
        }

        #endregion

        #region IState Members

        /// <summary>
        /// Initializes the state object against the state Payload.
        /// </summary>
        /// <param name="stateData"></param>
        public void Initialize(IStatePayload stateData)
        {
            ConvenienceStressStatePayload payload = (ConvenienceStressStatePayload)stateData;

            string windowXamlPaths = payload.WindowXamlPaths;

            if (!String.IsNullOrEmpty(windowXamlPaths))
            {
                windowXamls = windowXamlPaths.Split(new char[] { ',' });
            }

            contentXaml = payload.ContentXamlPath;

            constraintsTable = new ConstraintsTable(payload.ConstraintsTablePath);

            actionTypes = TypeListReader.ParseTypeList(payload.ActionsPath, typeof(DiscoverableAction));
            factoryPool = new FactoryPool(payload.FactoriesPath, DiscoverableInputHelper.GetFactoryInputTypes(actionTypes), constraintsTable);
            constraintsTable.VerifyConstraints(actionTypes);
            memoryLimit = payload.MemoryLimit;
        }

        /// <summary>
        /// Resets the application to the default state.
        /// </summary>
        public void Reset(int stateIndicator)
        {
            if (windowList != null)
            {
                foreach (Window window in windowList)
                {
                    window.Closing -= new CancelEventHandler(CancelWindowClosing);
                    window.Closed -= new EventHandler(w_Closed);
                    window.Close();
                }
            }

            windowList = new List<Window>();

            Trace.WriteLine("[ConvenienceStressState] Creating new Window...");
            if (windowXamls == null || windowXamls.Length == 0)
            {
                Trace.WriteLine("No valid WindowXamlPaths specified, so no window will be created.");
                return;
            }
            else
            {
                windowList.Add(LoadWindowFromXaml(stateIndicator));
            }
        }


        /// <summary>
        /// Window Close event notifies the State object to end the test.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void w_Closed(object sender, EventArgs e)
        {
            stateEnded = true;
        }

        /// <summary>
        /// Make sure the Mouse Input will not close the Window.
        /// </summary>
        void CancelWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        /// <summary>
        /// Implements the heuristic for determining when the test has exceeded complexity limits
        /// </summary>
        /// <returns></returns>
        public bool IsBeyondConstraints()
        {
            long memUse = GC.GetTotalMemory(true);
            Trace.WriteLine("[ConvenienceStressState] GC's Assumed memory in Use: " + memUse);

            int numberOfObjectsInVisualTree = 0;
            foreach (Window window in windowList)
            {
                numberOfObjectsInVisualTree += HomelessTestHelpers.VisualTreeWalk(window).Count;
            }

            Trace.WriteLine("[ConvenienceStressState] Number of the objects in visual tree: " + numberOfObjectsInVisualTree);

            return (numberOfObjectsInVisualTree > 500) || (memUse > memoryLimit);
        }

        #endregion

        #region Implementation

        /// <summary>
        /// Provides a randomly sorted queue of possible actions
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        internal Queue<Type> GetActions(DeterministicRandom random)
        {
            Queue<Type> randomQueue = new Queue<Type>();
            while (randomQueue.Count != actionTypes.Count)
            {
                // make sure each entry in the random queue is a unique action type
                Type randomAction = random.NextItem<Type>(actionTypes);
                if (!randomQueue.Contains(randomAction))
                {
                    randomQueue.Enqueue(randomAction);
                }
            }
            return randomQueue;
        }

        /// <summary>
        /// Load Window from the xaml files.
        /// </summary>
        /// <param name="stateIndicator">an integer to determinate which xaml to load.</param>
        private Window LoadWindowFromXaml(int stateIndicator)
        {
            int windowIndex = stateIndicator < 0 ? 0 : (stateIndicator % windowXamls.Length);

            string windowXamlPath = windowXamls[windowIndex];
            Window window = LoadXaml(windowXamlPath) as Window;

            if (window.Content != null)
            {
                throw new InvalidOperationException(string.Format("Window should have no content in {0}", windowXamlPath));
            }

            if (!string.IsNullOrEmpty(contentXaml))
            {
                Trace.WriteLine("[ConvenienceStressState] Loading content...");
                window.Content = LoadContentXaml();
            }

            window.Closing += new CancelEventHandler(CancelWindowClosing);
            window.Closed += new EventHandler(w_Closed);

            window.Show();

            return window;
        }

        /// <summary>
        /// Load content from content xaml. 
        /// </summary>
        /// <returns>object loaded.</returns>
        private object LoadContentXaml()
        {
            object content;
            content = LoadXaml(contentXaml);

            if (content is Window)
            {
                throw new InvalidOperationException(string.Format("Content xaml: {0}, should not contain Window.", contentXaml));
            }

            return content;
        }

        /// <summary>
        /// Load from xaml file. 
        /// </summary>
        /// <returns>loaded object</returns>
        private object LoadXaml(string fileName)
        {
            object loadedObject = null;
            Trace.WriteLine(string.Format("Loading from file: {0}...", fileName));
            using (FileStream fs = File.OpenRead(fileName))
            {
                loadedObject = XamlReader.Load(fs);
            }
            return loadedObject;
        }
        #endregion
    }
}
