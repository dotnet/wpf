// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Markup;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    /// <summary>
    /// Stress Constraints Dictionary
    /// </summary>
    public class StressConstraints : Dictionary<Type, ClassConstraints>
    {
        /// <summary>
        /// Load StressConstraints from a xaml file
        /// </summary>
        /// <param name="file">xaml file</param>
        /// <returns>StressConstaints loaded</returns>
        public static StressConstraints LoadFromFile(string file)
        {
            Trace.WriteLine("Loading constraints from : " + file);

            StressConstraints stressConstraints;

            using (Stream stream = File.OpenRead(file))
            {
                stressConstraints = (StressConstraints)XamlReader.Load(stream);
            }
            return stressConstraints;
        }
    }

    /// <summary>
    /// Class Constraints Dictionary for XAML Round-Tripping
    /// </summary>
    public class ClassConstraints : Dictionary<string, ConstrainedDataSource>
    {
    }

    /// <summary>
    /// A table of constraints mapped to WPF Type.Property combinations
    /// This is simply a storage service.
    /// </summary>
    public class ConstraintsTable
    {
        StressConstraints stressConstraints;

        public ConstraintsTable(string constraintsTablePath)
        {
            stressConstraints = ConstructConstraintsTable(constraintsTablePath);
        }

        /// <summary>
        /// Construct the ConstrainsTable from a list of files. If the same type exists in multiple files, throw. 
        /// </summary>
        /// <param name="constraintsTablePath"></param>
        /// <returns></returns>
        private StressConstraints ConstructConstraintsTable(string constraintsTablePath)
        {
            StressConstraints constraintsTable = new StressConstraints();
            if (!String.IsNullOrEmpty(constraintsTablePath))
            {
                string[] fileNames = constraintsTablePath.Split(new char[] { ',' });
                foreach (string fileName in fileNames)
                {
                    string file = fileName.Trim();
                    if (String.IsNullOrEmpty(file))
                    {
                        continue;
                    }

                    StressConstraints constrains = StressConstraints.LoadFromFile(file);

                    foreach (KeyValuePair<Type, ClassConstraints> pair in constrains)
                    {
                        constraintsTable.Add(pair.Key, pair.Value);
                    }

                }
            }
            else
            {
                Trace.WriteLine("[ConstraintsTable] ConstraintsTable loaded is empty.");
            }

            return constraintsTable;
        }
        public ConstrainedDataSource Get(Type targetType, string propertyName)
        {
            ConstrainedDataSource constraint = null;
            if (stressConstraints.ContainsKey(targetType))
            {
                Dictionary<string, ConstrainedDataSource> typeConstraints = stressConstraints[targetType];
                if (typeConstraints.ContainsKey(propertyName))
                {
                    constraint = typeConstraints[propertyName];
                }
                else
                {
                    throw new ArgumentException("PropertyName: " + propertyName + " is not registered for constraints");
                }
            }
            else
            {
                throw new ArgumentException("TargetType: " + targetType + " is not registered for constraints");
            }

            return constraint;
        }

        /// <summary>
        /// Check for missing constraints table entries.
        /// </summary>
        /// <param name="types"></param>
        internal void VerifyConstraints(List<Type> types)
        {
            Trace.WriteLine("[ConstraintsTable] Checking Constraints");
            foreach (Type t in types)
            {
                CheckConstraints(t);
            }
        }

        public List<Type> CheckConstraints(Type t)
        {
            List<Type> types = new List<Type>();
            foreach (PropertyDescriptor prop in DiscoverableInputHelper.GetInputProperties(t))
            {
                Trace.WriteLine(String.Format("[ConstraintsTable] Checking Property Constraint {0} on {1}.", prop.Name, t));
                Type inputType = prop.PropertyType;
                if (prop.Attributes.Contains(InputAttribute.CreateFromConstraints))
                {
                    ConstrainedDataSource datasource = Get(TargetTypeAttribute.FindTarget(t), prop.Name);
                    if (null == datasource)
                    {
                        throw new InvalidOperationException("Missing constraint entry for" + t + " property: " + prop.Name);
                    }
                    else
                    {
                        datasource.Validate();
                    }

                }
            }
            return types;
        }
    }
}
