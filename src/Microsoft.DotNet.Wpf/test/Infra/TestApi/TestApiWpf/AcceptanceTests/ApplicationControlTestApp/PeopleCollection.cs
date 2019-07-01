// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Microsoft.Test.AcceptanceTests.WpfTestApplication
{
    public class PeopleCollection : ObservableCollection<Person>
    {
        private Random random = new Random();

        public PeopleCollection()
        {
            CreateGenericPersonData(1);
        }

        public PeopleCollection(int multiplier)
        {
            CreateGenericPersonData(multiplier);
        }

        private void CreateGenericPersonData(int multiplier)
        {
            if (multiplier > 0)
            {
                for (int i = 0; i < multiplier; i++)
                {
                    Add(new Person("George", "Washington"));
                    Add(new Person("John", "Adams"));
                    Add(new Person("Thomas", "Jefferson"));
                    Add(new Person("James", "Madison"));
                    Add(new Person("James", "Monroe"));
                    Add(new Person("John", "Quincy", "Adams"));
                    Add(new Person("Andrew", "Jackson"));
                    Add(new Person("Martin", "Van Buren"));
                    Add(new Person("William", "H.", "Harrison"));
                    Add(new Person("John", "Tyler"));
                    Add(new Person("James", "K.", "Polk"));
                    Add(new Person("Zachary", "Taylor"));
                    Add(new Person("Millard", "Fillmore"));
                    Add(new Person("Franklin", "Pierce"));
                    Add(new Person("James", "Buchanan"));
                    Add(new Person("Abraham", "Lincoln"));
                    Add(new Person("Andrew", "Johnson"));
                    Add(new Person("Ulysses", "S.", "Grant"));
                    Add(new Person("Rutherford", "B.", "Hayes"));
                    Add(new Person("James", "Garfield"));
                    Add(new Person("Chester", "A.", "Arthur"));
                    Add(new Person("Grover", "Cleveland"));
                    Add(new Person("Benjamin", "Harrison"));
                    Add(new Person("William", "McKinley"));
                    Add(new Person("Theodore", "Roosevelt"));
                    Add(new Person("William", "H.", "Taft"));
                    Add(new Person("Woodrow", "Wilson"));
                    Add(new Person("Warren", "G.", "Harding"));
                    Add(new Person("Calvin", "Coolidge"));
                    Add(new Person("Herbert", "Hoover"));
                    Add(new Person("Franklin", "D.", "Roosevelt"));
                    Add(new Person("Harry", "S.", "Truman"));
                    Add(new Person("Dwight", "D.", "Eisenhower"));
                    Add(new Person("John", "F.", "Kennedy"));
                    Add(new Person("Lyndon", "B.", "Johnson"));
                    Add(new Person("Richard", "Nixon"));
                    Add(new Person("Gerald", "Ford"));
                    Add(new Person("Jimmy", "Carter"));
                    Add(new Person("Ronald", "Reagan"));
                    Add(new Person("George", "Bush"));
                    Add(new Person("Bill", "Clinton"));
                    Add(new Person("George", "W.", "Bush"));
                }
            }
        }
    }
}
