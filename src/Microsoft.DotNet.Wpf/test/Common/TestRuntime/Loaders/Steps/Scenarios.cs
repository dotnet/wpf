// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections.Generic;
using System.Xml.XPath;

namespace Microsoft.Test.Utilities.VariationEngine
{
	/// <summary>
	/// Summary description for Scenarios.
	/// </summary>
    internal class Scenarios
	{
		#region Private Members.
		internal List<Scenario> scenariolist;

		internal XmlNode defaultsnode = null;
		#endregion

		#region Internal Methods
		
		/// <summary>
		/// Constructor.
        /// </summary>
        internal Scenarios()
		{
			scenariolist = new List<Scenario>(); 
		}

        /// <summary>
        /// Store Scenario nodes under Scenarios Element.
        /// </summary>
        /// <param name="scenariosnode"></param>
        internal Scenarios(XmlNode scenariosnode)
        {
			scenariolist = new List<Scenario>();

			if (scenariosnode.Name != Constants.ScenariosElement)
			{
				throw new Exception("Unexpected root element in document, " + Constants.ScenariosElement + " expected");
			}

			string query = "./" + Constants.ScenarioElement;
			XmlNodeList templist = scenariosnode.SelectNodes(query);
			if (templist.Count == 0)
			{
				// Todo : Might be this is not good as user can add Scenarion instead of having read from a file.
				throw new Exception("No Scenarios found in current document");
			}

			// Save all Scenario elements.
			for (int i = 0; i < templist.Count; i++)
			{
				Scenario newscenario = new Scenario(templist[i]);
				scenariolist.Add(newscenario);
			}

			// Save Defaults element.
			query = "./" + "Defaults";
			templist = scenariosnode.SelectNodes(query);
			if (templist.Count != 0 && templist.Count > 1)
			{
				throw new ApplicationException("Cannot have more than one Defaults Element");
			}

			if (templist.Count > 0)
			{
				defaultsnode = templist[0];
			}
		}

		#endregion

	}

}
