// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Data;
using System.Xml;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    public class StressBindingInfo
    {
        #region Private Members

        private Library[] libraries;
        private XmlDocument[] xmlLibraries;
        private DataTable[] adoLibraries;
        private int changeIndex = 3;
        private SortDescriptionCollection[] sortDescriptionArray;
        private PropertyGroupDescription[] groupDescriptionArray;
        private Predicate<object>[] filters;
        private int addCount = 0;

        #endregion

        #region Constructor

        public StressBindingInfo(DeterministicRandom random)
        {
            libraries = new Library[] { new Library(0.0, random), new Library(0.1, random), new Library(0.2, random) };

            xmlLibraries = new XmlDocument[] { libraries[0].ToXmlDocument(),
                                              libraries[1].ToXmlDocument(),
                                              libraries[2].ToXmlDocument() };

            adoLibraries = new DataTable[] { libraries[0].ToDataTable(),
                                            libraries[1].ToDataTable(),
                                            libraries[2].ToDataTable() };

            sortDescriptionArray = new SortDescriptionCollection[4];

            SortDescriptionCollection sortDescriptionCollection = new SortDescriptionCollection();
            sortDescriptionCollection.Add(new SortDescription("Title", random.NextEnum<ListSortDirection>()));
            sortDescriptionArray[0] = sortDescriptionCollection;

            sortDescriptionCollection = new SortDescriptionCollection();
            sortDescriptionCollection.Add(new SortDescription("Genre", random.NextEnum<ListSortDirection>()));
            sortDescriptionCollection.Add(new SortDescription("Price", random.NextEnum<ListSortDirection>()));
            sortDescriptionArray[1] = sortDescriptionCollection;

            sortDescriptionCollection = new SortDescriptionCollection();
            sortDescriptionCollection.Add(new SortDescription("Price", random.NextEnum<ListSortDirection>()));
            sortDescriptionArray[2] = sortDescriptionCollection;

            sortDescriptionCollection = new SortDescriptionCollection();
            sortDescriptionCollection.Add(new SortDescription("Author", random.NextEnum<ListSortDirection>()));
            sortDescriptionArray[3] = sortDescriptionCollection;

            groupDescriptionArray = new PropertyGroupDescription[3];
            PropertyGroupDescription propertyGroupDescription = new PropertyGroupDescription();
            propertyGroupDescription.PropertyName = "Title";
            groupDescriptionArray[0] = propertyGroupDescription;

            propertyGroupDescription = new PropertyGroupDescription();
            propertyGroupDescription.PropertyName = "Genre";
            groupDescriptionArray[1] = propertyGroupDescription;

            propertyGroupDescription = new PropertyGroupDescription();
            propertyGroupDescription.PropertyName = "Author";
            groupDescriptionArray[2] = propertyGroupDescription;

            CustomFilter myfilter = new CustomFilter();
            filters = new Predicate<object>[] { new Predicate<object>(myfilter.Over50), new Predicate<object>(myfilter.NotOver50) };
        }

        #endregion

        #region Public Properties

        public SortDescriptionCollection[] SortDescriptions
        {
            get { return sortDescriptionArray; }
        }

        public PropertyGroupDescription[] GroupDescriptionArray
        {
            get { return groupDescriptionArray; }
        }

        public Library[] Libraries
        {
            get { return libraries; }
        }

        public XmlDocument[] XMLLibraries
        {
            get { return xmlLibraries; }
        }

        public DataTable[] ADOLibraries
        {
            get { return adoLibraries; }
        }

        public Predicate<object>[] Filters
        {
            get { return filters; }
        }

        #endregion

        #region Public Methods

        public void ChangeBookData(int publisherNumber)
        {
            for (int i = 0; i < libraries.Length; i++)
            {
                if (changeIndex >= libraries[i].Count)
                {
                    continue;
                }

                Book book = libraries[i][changeIndex] as Book;
                if (book != null)
                {
                    book.Publisher = "Publisher " + publisherNumber.ToString();
                }

                XmlElement xmlBook = xmlLibraries[i].FirstChild.FirstChild as XmlElement;
                for (int j = 0; j < changeIndex; j++)
                {
                    xmlBook = xmlBook.NextSibling as XmlElement;
                }

                xmlBook["Publisher"].InnerText = book.Publisher;

                DataTable dataTable = adoLibraries[i];
                DataRow dataRow = dataTable.Rows[changeIndex];
                dataRow["Publisher"] = book.Publisher;
            }

            changeIndex++;
        }

        public void RemoveBook()
        {
            // Always leave at least 1 book in the collections
            if (libraries[0].Count > 1)
            {
                if (changeIndex >= libraries[0].Count)
                {
                    changeIndex = 0;
                }

                for (int i = 0; i < libraries.Length; i++)
                {
                    if (libraries[i].Count > 0 && changeIndex < libraries[i].Count)
                    {
                        libraries[i].RemoveAt(changeIndex);

                        XmlElement xmlBook = xmlLibraries[i].FirstChild.FirstChild as XmlElement;
                        for (int j = 0; j < changeIndex; j++)
                        {
                            xmlBook = xmlBook.NextSibling as XmlElement;
                        }

                        xmlLibraries[i].FirstChild.RemoveChild(xmlBook);

                        DataTable dataTable = adoLibraries[i];
                        DataRow dataRow = dataTable.Rows[changeIndex];
                        dataTable.Rows.Remove(dataRow);
                    }
                }

                if (changeIndex >= libraries[0].Count)
                {
                    changeIndex = 0;
                }
            }
        }

        public void AddBook(int price, BookGenre genre)
        {
            Book book;
            for (int i = 0; i < libraries.Length; i++)
            {
                book = new Book(
                    DateTime.Now.ToString(),
                    addCount.ToString(),
                    "Author " + addCount.ToString(),
                    "Publisher",
                    price,
                    genre);  // getting random genre

                libraries[i].Add(book);

                xmlLibraries[i].FirstChild.AppendChild(book.ToXmlNode(xmlLibraries[i]));

                adoLibraries[i].Rows.Add(book.Title, book.ISBN, book.Publisher, book.Price, book.Author, book.Genre);
            }

            addCount++;
        }

        #endregion

        #region public class CustomFilter

        public class CustomFilter
        {
            public bool Over50(object item)
            {
                if (item.GetType() == typeof(Book))
                {
                    if (((Book)item).Price > 50.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (item.GetType() == typeof(XmlElement))
                {
                    if (double.Parse(((XmlElement)item)["Price"].InnerText) > 50.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return false;
            }

            public bool NotOver50(object item)
            {
                if (item.GetType() == typeof(Book))
                {
                    if (((Book)item).Price <= 50.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (item.GetType() == typeof(XmlElement))
                {
                    if (double.Parse(((XmlElement)item)["Price"].InnerText) <= 50.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return false;
            }
        }

        #endregion
    }
}
