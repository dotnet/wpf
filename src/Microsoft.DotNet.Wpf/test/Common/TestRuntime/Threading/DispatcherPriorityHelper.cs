// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Threading;


namespace Microsoft.Test.Threading
{
    /// <summary>
    /// Helper class to return DispatcherPriorities.
    /// </summary>
    public static class DispatcherPriorityHelper
    {
        /// <summary>
        /// Return a random priority that it is valid for a DispatcherTimer.
        /// </summary>
        public static DispatcherPriority GetRandomValidPriorityForDispatcherTimer()
        {
            DispatcherPriority[] priorities = GetValidDispatcherPrioritiesForDispatcherTimer();
            return priorities[GetNextRandom(priorities.Length)];
        }
        
        /// <summary>
        /// Return a random priority that it is valid for a DispatcherTimer.
        /// </summary>
        public static DispatcherPriority GetRandomValidDispatcherPriorityForDispatcherInvoking()
        {
            DispatcherPriority[] priorities = GetValidDispatcherPrioritiesForDispatcherInvoking();
            return priorities[GetNextRandom(priorities.Length)];
        }

        /// <summary>
        /// We return an array with all the DispatcherPriority values.
        /// </summary>
        public static DispatcherPriority[] GetAllDispatcherPriorities()
        {

            List<DispatcherPriority> invalidPriorities = new List<DispatcherPriority>();                                   
            return GetValidDispatcherPriorities(invalidPriorities);            
        }

        /// <summary>
        /// Set a Seed that will be used for all the random API on this class.
        /// You can only set it once.
        /// </summary>
        public static int Seed 
        {
            set
            {
                lock(_globalObjectSync)
                {
                    if (_random == null)
                    {
                        _random = new Random(value);
                    }
                }
            }
        }

        private static int GetNextRandom(int maxValue)
        {            
            // Max Value is not included.
            
            if (_random == null)
            {
                Seed = 1;
            }

            return _random.Next(maxValue);
        }
        
        /// <summary>
        /// We return an array with all the DispatcherPriority values, except for Inactive and Invalid.
        /// </summary>
        private static DispatcherPriority[] GetValidDispatcherPrioritiesForDispatcherTimer()
        {
            if (_validPrioritiesForDispatcherTimer != null)
            {
                return _validPrioritiesForDispatcherTimer;
            }
            
            lock (_globalObjectSync)
            {
                if (_validPrioritiesForDispatcherTimer == null)
                {
                    List<DispatcherPriority> invalidPriorities = new List<DispatcherPriority>();
                    invalidPriorities.Add(DispatcherPriority.Invalid);
                    invalidPriorities.Add(DispatcherPriority.Inactive);                    
   
                    
                    _validPrioritiesForDispatcherTimer = GetValidDispatcherPriorities(invalidPriorities);
                }
            }
            
            return _validPrioritiesForDispatcherTimer;
            
        }

        /// <summary>
        /// We return an array with all the DispatcherPriority values, Invalid.
        /// </summary>
        private static DispatcherPriority[] GetValidDispatcherPrioritiesForDispatcherInvoking()
        {
            if (_validPrioritiesForDispatcherInvoking != null)
            {
                return _validPrioritiesForDispatcherInvoking;
            }
            
            lock (_globalObjectSync)
            {
                if (_validPrioritiesForDispatcherInvoking == null)
                {
                    List<DispatcherPriority> invalidPriorities = new List<DispatcherPriority>();
                    invalidPriorities.Add(DispatcherPriority.Invalid);
                      
                    _validPrioritiesForDispatcherInvoking = GetValidDispatcherPriorities(invalidPriorities);
                }
            }
            
            return _validPrioritiesForDispatcherInvoking;
         }


        /// <summary>
        /// We return an array with all the DispatcherPriority values, except for Inactive and Invalid.
        /// </summary>
        private static DispatcherPriority[] GetValidDispatcherPriorities(List<DispatcherPriority> invalidPriorities)
        {
            List<DispatcherPriority> priorityList = null;
            
            ArrayList priorityArrayTemp = new ArrayList(Enum.GetValues(typeof(DispatcherPriority)));

            priorityList = new List<DispatcherPriority>();

            for (int i = 0; i < priorityArrayTemp.Count; i++)
            {
                if (!invalidPriorities.Contains((DispatcherPriority)priorityArrayTemp[i]))
                {
                    priorityList.Add((DispatcherPriority)priorityArrayTemp[i]);
                }
            }                    
            
            return priorityList.ToArray();
            
        }
       
        private static DispatcherPriority[] _validPrioritiesForDispatcherTimer = null;
        private static DispatcherPriority[] _validPrioritiesForDispatcherInvoking = null;
        private static object _globalObjectSync = new Object();
        private static Random _random = null;
        
    }    
}


