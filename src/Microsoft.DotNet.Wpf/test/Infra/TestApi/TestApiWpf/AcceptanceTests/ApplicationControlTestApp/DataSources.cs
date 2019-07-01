// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Globalization;

namespace Microsoft.Test.AcceptanceTests.WpfTestApplication
{
    public enum EyeColor
    {
        Unknown,
        Blue,
        Green,
        Brown,
        Other
    };

    public class Address
    {
        public string City { get; set; }
        public string State { get; set; }
    }

    public class Person : INotifyPropertyChanged, ICloneable, IDataErrorInfo
    {
        #region Private Fields

        private static int globalId = 0;   // not thread-safe
        private int id;
        private string firstName;
        private string lastName;
        private string middleName;
        private bool likesCake;
        private string cake = String.Empty;
        private Uri homepage = null;
        private DateTime dob;
        private EyeColor eyeColor = EyeColor.Unknown;
        private Address address;
        private bool _isSelected;

        #endregion Private Fields

        #region Constructors

        public Person()
        {
        }

        public Person(string firstName, string lastName)
            : this(firstName, String.Empty, lastName)
        {
        }

        public Person(string firstName, string lastName, DateTime dob)
            : this(firstName, String.Empty, lastName, dob)
        {
        }

        public Person(string firstName, string lastName, DateTime dob, Address address)
            : this(firstName, String.Empty, lastName, dob, address)
        {
        }

        public Person(string firstName, string middleName, string lastName)
            : this(firstName, middleName, lastName, new DateTime(2008, 10, 26))
        {
        }

        public Person(string firstName, string middleName, string lastName, DateTime dob)
            : this(firstName, middleName, lastName, dob, new Address { City = "Redmond", State = "WA" })
        {
        }

        public Person(string firstName, string middleName, string lastName, DateTime dob, Address address)
        {
            FirstName = firstName;
            MiddleName = middleName;
            LastName = lastName;
            LikesCake = true; // Everyone likes cake
            Cake = "Chocolate"; // Hehe....
            Dob = dob;
            Id = globalId++;
            Address = address;

            string prefix = firstName.ToLower(CultureInfo.CurrentCulture) + "." + lastName.ToLower(CultureInfo.CurrentCulture);
            Homepage = new Uri("http://" + prefix.Replace(' ', '_') + ".whitehouse.gov/");
        }

        #endregion Constructors

        #region Public Properties

        public int Id
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("Id");
            }
        }

        private Brush _brush = Brushes.LightGreen;
        public Brush Brush
        {
            get { return _brush = Brushes.LightGreen; }
            set
            {
                _brush = value;
                OnPropertyChanged("Brush");
            }
        }
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }
        public string FirstName
        {
            get { return firstName; }
            set
            {
                firstName = value;
                OnPropertyChanged("FirstName");
            }
        }

        public string MiddleName
        {
            get { return middleName; }
            set
            {
                middleName = value;
                OnPropertyChanged("MiddleName");
            }
        }

        public string LastName
        {
            get { return lastName; }
            set
            {
                lastName = value;
                OnPropertyChanged("LastName");
            }
        }

        public bool LikesCake
        {
            get { return likesCake; }
            set
            {
                likesCake = value;
                OnPropertyChanged("LikesCake");
            }
        }

        public string Cake
        {
            get { return cake; }
            set
            {
                cake = value;
                OnPropertyChanged("Cake");
            }
        }

        public Uri Homepage
        {
            get
            {
                return homepage;
            }
            set
            {
                homepage = value;
                OnPropertyChanged("Homepage");
            }
        }

        public DateTime Dob
        {
            get
            {
                return dob;
            }
            set
            {
                dob = value;
                OnPropertyChanged("DOB");
            }
        }

        public EyeColor MyEyeColor
        {
            get
            {
                return eyeColor;
            }
            set
            {
                eyeColor = value;
                OnPropertyChanged("MyEyeColor");
            }
        }

        public Address Address
        {
            get
            {
                return address;
            }
            set
            {
                address = value;
                OnPropertyChanged("Address");
            }
        }

        #endregion Public Properties

        #region Public Members        

        public override string ToString()
        {
            return FirstName + " " + LastName;
        }

        #endregion Public Members

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region ICloneable

        public object Clone()
        {
            Person person = (Person)this.MemberwiseClone();

            // need to manually copy Uri
            person.Homepage = new Uri(this.Homepage.OriginalString);

            return person;
        }

        #endregion ICloneable

        #region IDataErrorInfo

        string IDataErrorInfo.Error
        {
            get { return null; }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (columnName == "LastName")
                {
                    if (string.IsNullOrEmpty(this.LastName))
                    {
                        return "LastName cannot be null or empty";
                    }
                }

                return null;
            }
        }
        #endregion IDataErrorInfo
    }
}
