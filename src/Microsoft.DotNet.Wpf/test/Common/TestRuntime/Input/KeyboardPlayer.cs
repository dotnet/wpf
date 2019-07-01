// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading; using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security;
using System.Windows.Controls;
using System.Collections;
using System.IO;

using System.Windows.Markup;
using System.Xml;
using Microsoft.Test.Modeling;

namespace Microsoft.Test.Input
{

    ///<summary>
    /// Special TestCaseLoader that inherent from the TestCaseLoader used on Modeling test cases
    /// The special aspect on the TCL is that takes a ActionQueue
    ///</summary>
    
    internal class GeneralTestCaseLoader : TestCaseLoader
    {
        ///<summary>
        /// Takes a ActionsQueue to be execute the test cases on it
        ///</summary>
        public GeneralTestCaseLoader(ActionsQueue testCaseQueue)
        {
            if (testCaseQueue == null)
                throw new ArgumentNullException("testCaseQueue", "Cannot be null");

            _testCaseQueue = testCaseQueue;
            
        }

        ///<summary>
        /// Way to pass that to the base class 
        ///</summary>
        protected override ActionsQueue CreateActionQueue()
        {
            return _testCaseQueue;
        }

        
        ActionsQueue _testCaseQueue;
    } // end GeneralTestCaseLoader 

    ///<summary>
    /// Keyboard Player for the Recorder
    /// 
    ///</summary>
    
    public class KeyboardPlayer
    {
        ///<summary>
        /// Takes the xml for KeyboardTestCase and uses that to run the test case
        /// The Model that you passed it is the one that it will be used
        ///</summary>
        public KeyboardPlayer(XmlDocument xmlDoc, Model model)
        {
            if (xmlDoc == null)
                throw new ArgumentNullException("xmlDoc", "Cannot be null");

            if (model == null)
                throw new ArgumentNullException("model", "Cannot be null");

            

            _xmlDoc = xmlDoc;
            _model = model;
        }

        ///<summary>
        /// Loads the Xml and Execute the test cases
        /// 
        ///</summary>
        public void Play()
        {
            ActionsQueue queue = new ActionsQueue();
	    
            XmlElement element = (XmlElement)_xmlDoc.SelectSingleNode("KeyboardTestCase");


            // Retriving the test Xmal Content for set up the environment, right now all the variations share the 
            // same enviroment.
            
            XmlElement xamlElement = (XmlElement)element.SelectSingleNode("Xaml");
            XmlTextWriter writer = new XmlTextWriter("KeyboardTestEnvironment.xml", System.Text.Encoding.Unicode);
            xamlElement.WriteContentTo(writer);
            writer.Close();

            
            // Getting all the Variations
            XmlNodeList xmlList = element.SelectNodes("RecordActions");
            for (int j=0; j<xmlList.Count; j++)
            {

                XmlElement actionTest = (XmlElement)xmlList[j];
                ModelTestCase modelTestCase = convertXmlElementToModelTestCase(_model, actionTest);
                
                queue.Enqueue(modelTestCase);
            }


            // This is actually executing the test cases
            GeneralTestCaseLoader tcLoader = new GeneralTestCaseLoader(queue);
            tcLoader.AddModel(_model);
            tcLoader.Run();
                


        }


        ///<summary>
        /// This is been used to convert XmlElement to a modeltestcase. This method also populate the 
        /// ModelTransitions
        ///</summary>
        ModelTestCase convertXmlElementToModelTestCase(Model model, XmlElement xmlElement)
        {

            ModelTestCase modelTestCase = new ModelTestCase();

            //This will call BeginCase, this is a pattern for State-based Modeling
            ModelTransition modelTransition = new ModelTransition(model);               
            modelTransition.ActionName = "";
            modelTestCase.AddTransition(modelTransition);

            modelTransition = new ModelTransition(model);               
            modelTransition.ActionName = "LoadEnvironment";
            modelTransition.InParams.Add("Filename","KeyboardTestEnvironment.xml");
            modelTestCase.AddTransition(modelTransition);

            for(int i=0; i< xmlElement.ChildNodes.Count; i++)
            {
                modelTransition = new ModelTransition(model);
                modelTestCase.AddTransition(modelTransition);
                convertToModelTransition(modelTransition, (XmlElement)xmlElement.ChildNodes[i]);
            }
            
            modelTestCase.AddTransition(new ModelEndTransition(model, new State()));

            return modelTestCase;

        }

    
        ///<summary>
        /// Given an XmlElement node that contains the transition, this populate the modelTranstion
        /// that is passed as parameter
        ///</summary>
        void convertToModelTransition(ModelTransition modelTransition, XmlElement action)
        {

            string name = action.GetAttribute("Name");


            if (name == "KeyDown")
            {
                modelTransition.ActionName = "KeyDown";
      
                getKeyState(modelTransition, action);

            }
            else if (name == "KeyUp")
            {
                getKeyState(modelTransition, action);
                modelTransition.ActionName = "KeyUp";                


            }
            else if (name == "GotKeyboardFocus")
            {
                modelTransition.ActionName = "GotKeyboardFocus";
				modelTransition.EndState.Add("NewFocus", action.GetAttribute("NewFocus"));
				modelTransition.EndState.Add("OldFocus", action.GetAttribute("OldFocus"));

			}
			else if (name == "LostKeyboardFocus")
			{
                modelTransition.ActionName = "LostKeyboardFocus";
				modelTransition.EndState.Add("NewFocus", action.GetAttribute("NewFocus"));
				modelTransition.EndState.Add("OldFocus", action.GetAttribute("OldFocus"));

			}
            else if (name == "MouseDown")
            {
                modelTransition.ActionName = "MouseDown";
				modelTransition.InParams.Add("Source", action.GetAttribute("Source"));


            }
		}



        ///<summary>
        /// This is called on KeyDown or KeyUp to get the state of the KeyDown and populate 
        /// the modeltransition InParams that will be used on the Model.
        ///</summary>        
        void getKeyState(ModelTransition modelTransition,XmlElement action)
        {
            modelTransition.InParams.Add("Key",action.GetAttribute("Key"));
            modelTransition.InParams.Add("ShiftDown",action.GetAttribute("ShiftDown"));
            modelTransition.InParams.Add("ControlDown",action.GetAttribute("ControlDown"));
            modelTransition.InParams.Add("AltDown",action.GetAttribute("AltDown"));                
            modelTransition.InParams.Add("TextInputKey",action.GetAttribute("TextInputKey"));                
            modelTransition.InParams.Add("ImeProcessedKey",action.GetAttribute("ImeProcessedKey"));   
        }

        Queue _transitions = new Queue();
        XmlDocument _xmlDoc;
        Model _model = null;
    } // end KeyboardPlayer

}//namespace

